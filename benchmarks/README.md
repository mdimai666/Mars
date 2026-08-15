# Бенчмарки Mars

В каталоге два вида измерений:

1. **Нагрузочные тесты (k6 + стенд)** — HTTP-нагрузка на реальное приложение:
   рендер страниц (статика и данные из БД, анонимно и авторизованно) и CRUD постов через API.
2. **Микробенчмарки (BenchmarkDotNet)** — отдельные проекты `Benchmarks.*` и `Benchmark.*`
   (движки шаблонов, запросы к БД, Handlebars, JSON-сериализация, QueryLang и т.д.).

---

## Нагрузочные тесты (k6)

### Требования

- **Docker Desktop** (запущен) — стенд поднимает PostgreSQL 14 через Testcontainers;
- **k6 v2** — `winget install GrafanaLabs.k6`;
- **.NET SDK 10**.

### Быстрый старт

Из корня репозитория:

```powershell
.\benchmarks\run-perf.ps1
```

Скрипт: собирает стенд → поднимает Kestrel + контейнер PostgreSQL → сидит данные
(админ, фронт с дефолтной темой, 1000 постов) → прогоняет 8 сценариев k6 последовательно →
кладёт JSON-сводки и лог стенда в `benchmarks/results/<версия приложения>/`
(версия берётся из `MarsAppVersion` сборки Mars.WebApp — результаты разных версий
не смешиваются).

Полезные варианты:

```powershell
.\benchmarks\run-perf.ps1 -Mode smoke                               # быстрая проверка на явные просадки (~1,5 минуты)
.\benchmarks\run-perf.ps1 -Scenarios render_static_anon,post_read   # только часть сценариев (без пробела после запятой)
.\benchmarks\run-perf.ps1 -Parallel                                 # все сценарии одновременно
.\benchmarks\run-perf.ps1 -Posts 5000                               # другой объём данных
```

Режимы `-Mode`:
- `full` (по умолчанию) — полные 30-секундные окна, ~5 минут: основной срез для сравнения версий;
- `smoke` — по 1000 итераций на сценарий, суммарно ~1,5 минуты: повседневная проверка
  «нет ли явных просадок» после изменений. Оба режима пишутся в историю (`history.csv`)
  в отдельных колонках `mode` и сравниваются каждый со своим предыдущим прогоном.

### Вручную

```powershell
# 1. Стенд (Ctrl+C — остановка; --no-wait — поднять, проверить и выйти)
dotnet run --project benchmarks/Mars.Performance.Stand -c Release -- --posts 1000

# 2. В другом терминале (URL стенд печатает сам)
$env:MARS_URL="http://localhost:PORT"
$env:MARS_SCENARIO="render_db_anon"   # без MARS_SCENARIO — все сценарии параллельно
k6 run benchmarks/k6/run-all.js
```

Против уже запущенного приложения (свои данные/фронт):

```powershell
dotnet run --project benchmarks/Mars.Performance.Stand -c Release -- --attach http://localhost:5003
```

### Сценарии

| Сценарий | Что меряет | Запрос | Авторизация |
|---|---|---|---|
| `render_static_anon` | рендер шаблона без БД | `GET /` | анонимно |
| `render_static_auth` | рендер шаблона без БД | `GET /` | Bearer |
| `render_db_anon` | рендер шаблона с запросом к БД (QueryLang, список постов) | `GET /posts` | анонимно |
| `render_db_auth` | то же | `GET /posts` | Bearer |
| `post_read` | чтение поста по slug | `GET /api/Post/by-type/post/item/{slug}` | анонимно |
| `post_list` | постраничный список постов | `GET /api/Post/list/page` | анонимно |
| `post_create` | создание поста | `POST /api/Post` | Bearer |
| `post_update` | обновление поста (GET + PUT) | `PUT /api/Post` | Bearer |
| `stress_render_static` | потолок рендера статики | `GET /` | анонимно |
| `stress_render_db` | потолок рендера с БД | `GET /posts` | анонимно |
| `stress_post_create` | потолок записи | `POST /api/Post` | Bearer |

Стенд использует дефолтный стартовый шаблон (`src/Mars.WebApp/Res/front_templates/default`):
`/` — чистая статика, `/posts` — страница с `{{#context}}`-запросом к БД,
`/posts/{slug}` — детальная страница поста.

**Базлайнные сценарии** — `constant-vus`: фиксированное число VU на фиксированное
окно времени (чтение — 20 VU, запись — 10 VU, окно по умолчанию 30s). Так замер
снимается из стационарного режима и сопоставим от прогона к прогону. Логин
выполняется один раз в `setup()` и в замерах не участвует.

**Стресс-сценарии** (`stress_*`, в базлайновый прогон не входят) — `ramping-vus`:
разгон 0 → 100 → 200 VU за ~2,5 минуты с полкой на максимуме. Показывают точку
насыщения сервера: RPS перестаёт расти, p95 уходит вверх. Запуск:
`$env:MARS_SCENARIO="stress_render_db"` или
`.\benchmarks\run-perf.ps1 -Scenarios stress_render_db,stress_post_create`.

### Настройка через переменные окружения

| Переменная | По умолчанию | Назначение |
|---|---|---|
| `MARS_URL` | `http://localhost:5003` | URL приложения |
| `MARS_SCENARIO` | — (все сценарии) | один конкретный сценарий |
| `MARS_POSTS` | `1000` | сколько постов посеяно (слаг-пул `post-0001..post-N`) |
| `MARS_LOGIN` / `MARS_PASSWORD` | `testuser` / `Password123@` | учётка (в стенде — админ) |
| `MARS_READ_VUS` / `MARS_WRITE_VUS` | `20` / `10` | VU в базлайне (чтение / запись) |
| `MARS_DURATION` | `30s` | окно нагрузки базлайна |
| `MARS_STRESS_VUS` | `200` | потолок VU стресс-профиля |
| `MARS_MODE` | `full` | `smoke`/`full` (ставит `run-perf.ps1`) |
| `MARS_SMOKE_ITERATIONS` | `1000` | итераций на сценарий в smoke |

Пороги качества: доля ошибок `< 1%`, доля прошедших проверок `> 99%`.

### История результатов и поиск регрессий

`benchmarks/history.csv` коммитится в git. После каждого последовательного прогона
`run-perf.ps1` дописывает строку на сценарий:

```
timestamp,commit,version,mode,scenario,rps,avg_ms,p95_ms,failed_pct
```

и печатает таблицу сравнения с предыдущим прогоном того же режима. Пометкой `!!!`
выделяется подозрение на регрессию: RPS упал на 20%+ или p95 вырос на 25%+
(одиночные замеры шумят, поэтому допуски широкие; для сомнительных мест гоняйте
повторно). Стресс-прогоны в историю не пишутся.

### Результаты

Базовый срез 2026-08-15 (k6 v2.2.0, профиль constant-vus 30s): Windows 11, i7-13700,
Docker Desktop (WSL2), PostgreSQL 14 в контейнере, 1000 постов в БД, приложение в Release,
окружение Test (как в интеграционных тестах).

| Сценарий | RPS (ит/с) | avg, мс | p95, мс | ошибки |
|---|---:|---:|---:|---:|
| render_static_anon | 51 079 | 0,33 | 1,4 | 0% |
| render_static_auth | 3 877 | 5,0 | 7,6 | 0% |
| render_db_anon | 1 207 | 16,4 | 22,3 | 0% |
| render_db_auth | 949 | 20,9 | 26,7 | 0% |
| post_read | 2 894 | 6,8 | 9,2 | 0% |
| post_list | 2 159 | 9,0 | 13,2 | 0% |
| post_update | 627 | 7,8 | 12,6 | 0% |
| post_create | 1 224 | 8,0 | 10,2 | 0% |

Потолки сервера (стресс-профиль ramping-vus, разгон до максимума за 2,5 мин):

| Сценарий | Пик VU | Потолок RPS | avg на пике, мс | p95 на пике, мс | ошибки |
|---|---:|---:|---:|---:|---:|
| stress_render_static | 200 | 62 513 | 2,0 | 4,1 | 0% |
| stress_render_db | 200 | 1 295 | 102,6 | 175,5 | 0% |
| stress_post_create | 100 | 1 811 | 36,5 | 64,7 | 0% |

Выводы:
- авторизация (Bearer JWT) добавляет ~5 мс на запрос — проверка RSA-подписи токена;
- рендер с БД (`/posts`) — узкое место: потолок ~1,3k RPS достигается уже при 20 VU,
  дальше рост конкурентности только увеличивает latency;
- статика отдаётся 50–60k RPS — запас по серверу огромный;
- запись: ~1,2k create/s при 10 VU, под разгоном до 100 VU — ~1,8k create/s;
- `post_update` — два HTTP-запроса на итерацию (GET перед PUT), RPS указан по итерациям;
- порядок сценариев важен: `post_create` раздувает БД, и чтения по slug деградируют —
  **индекса на `Posts.Slug` нет** (seq scan); в `run-perf.ps1` create идёт последним.

---

## Микробенчмарки (BenchmarkDotNet)

Каждый проект — консольное приложение:

```powershell
dotnet run --project benchmarks/Benchmarks.TemplateEngines -c Release
```

Результаты складываются в `BenchmarkDotNet.Artifacts/` (в git не коммитятся);
у некоторых проектов есть свои `RESULT.md`.

---

## История: замеры ApacheBench (ab)

До k6 использовался ApacheBench:

```
.\ab.exe -n 100 -c 20 http://localhost:5003/
.\ab.exe -n 1000 -c 50 http://localhost:5003/
```

Для конфигурации Windows 11, i7-13700:

1. Пустая/полностью закешированная страница: `Requests per second: 8322.93 [#/sec] (mean)`
   (без разницы, шаблонный URL `{param}` или нет).
2. Страница с запросом к БД: `Requests per second: 2895.86 [#/sec] (mean)`.
3. Nodes Http In: `Requests per second: 27.19 [#/sec] (mean)`.
