# План: уменьшение Docker-образа mdimai666/mars — framework-dependent + chiseled

Статус: план (исполняется позже, по команде). Создан: 2026-09-09.

## Цель

Уменьшить размер Docker-образа `mdimai666/mars` (Docker Hub), не меняя поведение инстансов.
**Это НЕ про RAM**: замеры (см. ниже) показали, что смена базового образа почти не влияет на
потребление памяти процесса — выигрыш в размере образа: быстрее pull/деплой, меньше место на
хосте и в реестре.

## Контекст: что измерено (2026-09-09)

- Инстанс Mars в Docker (прод): cgroup ~223 MiB, VmRSS ~336–347 MiB, один dotnet-процесс,
  342 загруженные сборки. Память почти не зависит от базового образа.
- Baseline минимального ASP.NET Core: ~69 MiB (Ubuntu noble) vs ~64–66 MiB (alpine/musl) —
  разница 3–5 MiB.
- Образ `mdimai666/mars` сейчас: publish-содержимое ~252 MB (240,4 MiB, 2348 файлов), база
  `aspnet:10.0` (Ubuntu 24.04, ~230 MB). **Образ framework-dependent, НЕ self-contained** —
  установлено фактически при исполнении этапа 1 (2026-09-09): локальные publish c `-r linux-x64`
  со `--self-contained false` и без него **идентичны** (240,4 MiB, рантайма нет), а принудительный
  `-p:SelfContained=true` падает с `NETSDK1067` («Self-contained applications are required to use
  the application host»): в Dockerfile стоит `-p:UseAppHost=false`, что исключает self-contained.
  Рантайм берётся из базового слоя. Прежняя оценка «самодостаточный образ, рантайм в /app» была
  ошибочной.
- «Жир» образа — не рантайм в publish, а **базовый слой Ubuntu (~230 MB)** + контент приложения
  (~252 MB): реальный выигрыш размера — смена базы (этап 2), не флаг (этап 1).

## Ключевые факты для выбора базы (.NET 10)

Проверено фактически (mcr, 2026-09-09):

| Вариант базы | Тег | Комментарий |
|---|---|---|
| Ubuntu 24.04 (текущая) | `aspnet:10.0` | shell, ICU, root; размер базового слоя ~230 MB |
| Chiseled (distroless) | `aspnet:10.0-noble-chiseled` | есть muxer `/usr/bin/dotnet` + оба shared-рантайма; **нет shell/apt**; **User=1654**; **`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true`** (ICU нет) |
| **Chiseled-extra (рекомендуется для этапа 2)** | `aspnet:10.0-noble-chiseled-extra` | то же, что chiseled, но **+ ICU и tzdata**; `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` отсутствует — глобализация штатная. Проверено 2026-09-09: runtimes 10.0.11, muxer есть, **User=1654** |
| Alpine (musl) | `aspnet:10.0-alpine` | **исключён**: нативные кодеки PhotoSauce (`Mars.Media.Host`, грузятся при старте) не имеют musl-сборок, только glibc |
| Debian-slim | — | **тегов для .NET 10 нет** (trixie/bookworm-slim отсутствуют) |

Прямые следствия для chiseled (оба варианта):
1. **ICU**: у plain-`chiseled` глобализация выключена (invariant) — неприемлемо для локализуемой CMS.
   У **`-chiseled-extra` ICU и tzdata уже включены** — вопрос закрыт (tzdata важен и для часовых
   поясов, в т.ч. РФ). Обычный `chiseled` без ICU не рассматривать.
2. **User=1654**: текущий контейнер Mars работает root (в образе нет `USER`). CloudPanel монтирует
   каталоги от root (`/root/.aspnet/DataProtection-Keys`, `/app/data`, `/app/wwwroot/upload`) —
   под 1654 запись в `/root/.aspnet` невозможна. Нужно решение (см. Этап 2, вопрос B).
3. **Нет shell**: образ нельзя `docker exec ... sh`; проверено, что тесты/оркестратор используют
   только HTTP/`docker run`, shell не требуется (подтвердить при верификации).

## Этап 1 (сделан 2026-09-09): явная фиксация framework-dependent

Дифф внесён в Dockerfile: добавлен `--self-contained false`. **Режим при этом не изменился** —
образ и раньше был framework-dependent: `-p:UseAppHost=false` в Dockerfile исключает
self-contained (SDK требует apphost для SC, `NETSDK1067`), и рантайм всегда брался из базового
слоя. Проверено локальным publish: со флагом и без — идентичный результат (240,4 MiB).
Значение правки: делает режим явным и защищает от будущих изменений (например, если `UseAppHost`
когда-нибудь включат — `-r linux-x64` молча вернёт self-contained). **Размера этап 1 не меняет**;
единственный реальный выигрыш — этап 2 (база 230 MB → 162 MB у `-chiseled-extra`).

### Дифф Dockerfile (этап 1)

В publish-стадии (`dotnet publish ...`) добавить флаг `--self-contained false`
(вместе с существующим `-r linux-x64`, чтобы сохранить выбор нативных ассетов RID):

```dockerfile
RUN --mount=type=cache,target=/root/.nuget/packages \
        dotnet publish "./Mars.WebApp.csproj" \
            -c $BUILD_CONFIGURATION \
            -o /app/publish \
            --self-contained false \
            -p:UseAppHost=false \
            -p:DockerBuild=true \
            -p:SourceRevisionId="${GIT_SHA}" \
            -r linux-x64
```

Больше ничего не меняем (база `aspnet:10.0`, ENTRYPOINT, EXPOSE 80). `Mars.WebApp.csproj`
не трогаем: `SelfContained`/`RuntimeIdentifiers` в Release не заданы — режим определяет только CLI.

### Критерии готовности этапа 1

- Дифф внесён; локальный publish со флагом проходит (проверено: `--self-contained false -r
  linux-x64`, 240,4 MiB, сборка успешна). Размер publish не меняется — это фиксация режима.
- Полная проверка (docker-сборка, smoke, RAM) — вместе с этапом 2, чтобы не гонять сборку
  дважды.

## Этап 2 (по желанию): chiseled-extra (`aspnet:10.0-noble-chiseled-extra`)

Меняем только базовую строку; вопрос ICU закрыт выбором `-extra` (ICU + tzdata внутри),
остаётся вопрос B (пользователь и права). Дифф:

```dockerfile
# было:
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# стало:
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS base
```

### Вопрос A — глобализация (ICU) — решён выбором тега

Plain `chiseled` идёт с `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true` и без ICU — для Mars
(ru/en-локализация, даты/числа, Npgsql) неприемлем. **`-chiseled-extra` уже включает ICU и
tzdata**, invariant-флаг отсутствует (проверено 2026-09-09) — используем только его. Если по
размеру/иным причинам `-extra` не устроит — откат к этапу 1 (framework-dependent на noble),
кастомная ICU-сборка через `chisel` не нужна.

### Вопрос B — решено: non-root (B2), сделано 2026-09-09

Выбран полный non-root (uid 1654, задаётся базовым chiseled-образом). Факты, определившие
дизайн:
- В chiseled **нет shell** — в final-стадии невозможны `RUN`; `/app` обязан создать первый же
  `COPY --chown=1654:1654` (иначе каталог остаётся root и 1654 не может писать). В базовом
  chiseled-образе `/app` отсутствует (проверено) — паттерн работает.
- Порт 80 не-root доступен: docker выставляет `net.ipv4.ip_unprivileged_port_start=0`
  (проверено) — `Urls http://+:80` менять не нужно, Caddy (:80) не трогаем.
- Хостовая группа каталогов данных — `mars-cloud` (gid 994); CloudPanel-сервис работает под
  `mars-cloud` (не root), chown недоступен → каталоги данных получают **режим 0770**, а
  контейнеру добавляется **supplementary group 994** (`GroupAdd`) — 1654 пишет через группу.

Изменения внесены:
- **Mars/Dockerfile**: база → `aspnet:10.0-noble-chiseled-extra`; final-стадия:
  `COPY --from=publish --chown=1654:1654 /app/publish /app` (создаёт /app владельцем 1654),
  `ENV HOME=/app` (ключи DataProtection детерминированно в `/app/.aspnet/DataProtection-Keys`),
  без RUN; ENTRYPOINT прежний.
- **Mars.Cloud** (репо облака):
  - `InstanceProvisioningService.BuildSpec` — бинд ключей: `/app/.aspnet/DataProtection-Keys`
    (вместо `/root/.aspnet/...`);
  - `DockerInstanceProvider` — `GroupAdd` из новой опции `Instances:DataGroupId`;
  - `EnsureVolumeDirectories` — на Linux каталогам данных режим 0770 (владелец mars-cloud).

Осталось для прода (отдельными подтверждёнными шагами):
1. Собрать и опубликовать образ Mars (`pwsh build-docker.ps1`, затем `publish-docker.ps1`).
2. В `/opt/mars-cloud/env/cloud-panel.env` добавить `Instances__DataGroupId=994`; выкатить панель.
3. Мигрировать права существующего инстанса (test2): на сервере
   `sudo chown -R 1654:994 /opt/mars-cloud/instances/*` (+ при необходимости `chmod -R g+w`).
4. Пересоздать контейнер инстанса (спецификация изменилась: env-бинды/группа) — reconcile
   сам пересоздаст при несовпадении spec; при необходимости удалить контейнер вручную.
5. Прогнать `MARS_DOCKER_TESTS=1 dotnet test tests/Mars.DockerImage.Tests`.
6. Проверить инстанс: HTTP 200, загрузка медиа, ru-RU-культура, ключи DP создаются.

Бонус: переход на non-root устраняет известный дефект прода «удаление инстанса падает на
root-файлах» (новые файлы в каталогах — не root, а 1654/группа mars-cloud).

### Критерии готовности этапа 2

- ✅ Локальный прототип паттерна на chiseled-extra: приложение не-root стартует на :80, HTTP 200,
  ключи DataProtection создаются в /app/.aspnet (2026-09-09).
- ✅ `Mars.DockerImage.Tests` зелёные локально (2026-09-09, MARS_DOCKER_TESTS=1, 7 тестов):
  старт с пустой БД, процесс dotnet под uid 1654, USER=1654 и отсутствие invariant-глобализации
  в конфиге, логи без Unhandled, API-валидация/статика WASM/404, ключи DP в /app/.aspnet,
  сквозной медиа-upload (PNG через MagicScaler → файл отдаётся). Осталось: прогнать после
  публикации образа (см. прод-шаги) и smoke инстанса.
- RAM в прежних пределах; глобализация ru-RU работает (на `-chiseled-extra` ICU штатно).

## Чек-лист верификации (выполнять после каждого этапа)

```bash
# 1. Сборка и размер
pwsh build-docker.ps1                          # теги version+sha+latest
docker images mdimai666/mars                   # зафиксировать размер (сравнить с "до")

# 2. Локальный smoke (с Postgres, как в Mars.DockerImage.Tests)
#    образ реально запускается с env ConnectionStrings__DefaultConnection, порт 80
docker run -d --name mars-smoke -p 8080:80 \
  -e ConnectionStrings__DefaultConnection="Host=127.0.0.1;Port=5432;Database=...;Username=...;Password=..." \
  mdimai666/mars:latest
curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:8080/   # 200
# 3. RAM не выросла
docker stats --no-stream mars-smoke
# 4. Этап 2 — глобализация (запустить через muxer, т.к. shell нет):
docker run --rm --entrypoint /usr/bin/dotnet mdimai666/mars:latest exec ... # либо тест через HTTP-эндпоинт,
#    возвращающий ru-RU дату/число; убедиться, что культура применяется (на -chiseled-extra должна)
# 5. Этап 2, вопрос B — запись: загрузить файл/медиа в инстанс (upload работает, DataProtection-ключи создаются)
# 6. Автотесты образа:
dotnet test tests/Mars.DockerImage.Tests --verbosity minimal
```

## Риски и открытые вопросы

- **RAM не уменьшится** — это ожидаемо; план про размер образа. Для RAM отдельный план
  (модульность загрузки, фиче-флаги `FeatureManagement__{AiChat,AITool,SingleSignOn}`).
- Chiseled-фаза тянет один поведенческий вопрос (non-root, вопрос B) — поэтому вынесена
  отдельно от безопасного этапа 1.
- Если `-chiseled-extra` не устроит (размер, поведение) — возврат к этапу 1 без потерь;
  кастомная ICU-сборка через `chisel` не нужна.
- Плавучий тег базы: `aspnet:10.0` / `-noble-chiseled-extra` обновляются (сейчас 10.0.11) —
  образ пересобирается под актуальный патч, это норма.
- CloudPanel-часть (права/пути биндов) — только для варианта B2 и только в репо Mars.Cloud.

## Порядок исполнения

1. ✅ Этап 1 выполнен (2026-09-09): `--self-contained false` в Dockerfile; локальный publish
   проверен (240,4 MiB). Размер не меняется — это фиксация режима.
2. ✅ Этап 2 (2026-09-09, B2 non-root) внесён в код: Mars/Dockerfile (chiseled-extra,
   `COPY --chown=1654:1654`, `HOME=/app`) и Mars.Cloud (бинды ключей, `DataGroupId` + `GroupAdd`,
   режим 0770). Mars.Cloud собран; локальный прототип паттерна на chiseled-extra — в проверке.
3. ⏳ Прод (по отдельной команде): собрать/опубликовать образ → `Instances__DataGroupId=994`
   в env панели + выкат → `chown -R 1654:994` каталогов инстансов → пересоздать контейнер
   инстанса → `MARS_DOCKER_TESTS=1 dotnet test tests/Mars.DockerImage.Tests` → smoke инстанса.
