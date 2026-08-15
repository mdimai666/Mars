# Задача: индекс на Slug у постов

**Статус:** не сделана (создана 2026-08-15 по итогам нагрузочных тестов k6).

## Проблема

У таблицы `Posts` нет индекса по `Slug` — поиск поста по slug выполняется seq scan'ом
по всей таблице. Это видно на нагрузочных тестах (`benchmarks/run-perf.ps1`):

- на 1000 постов: `GET /api/Post/by-type/post/item/{slug}` ~ 7–8 мс;
- когда сценарий `post_create` раздул БД до ~38 000 постов, тот же запрос стал
  ~30 мс, а сценарий `post_update` деградировал с **627 до 202 ит/с** (×3);
- цифры: `benchmarks/README.md` → «Результаты», выводы про порядок сценариев.

Запросы, которые страдают:

1. `PostRepository.GetDetailBySlug` — `src/Mars.Host.Repositories/PostRepository.cs` (~строка 46):
   `FirstOrDefaultAsync(s => s.PostType.TypeName == type && EF.Functions.ILike(s.Slug, slug))`
   — используется в `GET /api/Post/by-type/{type}/item/{slug}` и в рендере страниц
   (`/api/PageRender/by-post/{type}/{slug}`).
2. `PostRepository.ExistAsync(typeName, slug)` — тот же файл (~строка 289):
   проверка коллизий при создании/обновлении поста.
3. Шаблоны фронтов через QueryLang, например `post_detail_page.hbs`:
   `post = ef.post.First(post.Slug.ToLower() == Slug.ToLower())`.

## Что сделать

1. Добавить индекс на `Posts`. Оптимально — составной по фактическому паттерну запроса
   (тип + slug): `(PostTypeId, lower(Slug))`.

   Важный нюанс: запрос использует `EF.Functions.ILike` (регистронезависимое сравнение),
   поэтому **обычный b-tree индекс по `Slug` использоваться не будет** — нужен именно
   выражение-индекс по `lower("Slug")` (варианты: `citext`-колонка или pg_trgm GIN,
   если понадобятся поиски по подстроке).

2. Индекс создаётся raw-SQL'ом в EF-миграции (`HasIndex` не умеет выражения):
   - миграции: `src/Mars.Host.Data.PostgreSQL/Migrations/` (образец свежей —
     `20260207164400_AddPostCategories.cs`);
   - пример: `migrationBuilder.Sql("CREATE INDEX \"IX_Posts_PostTypeId_SlugLower\" ON \"Posts\" (\"PostTypeId\", lower(\"Slug\"));");`
     (down — `DROP INDEX`).

3. Уникальность — по желанию. Коллизии slug сейчас проверяются кодом (`ExistAsync`),
   уникальный индекс мог бы защитить от гонок, но мешает soft-delete (`DeletedAt`):
   если делать, то частичный `UNIQUE ... WHERE "DeletedAt" IS NULL`. Начать можно
   с неуникального индекса.

4. PostgreSQL — основная цель. Если нужны MsSQL/MySQL — проверить их провайдеры
   (`src/Mars.Datasource` тут ни при чём; речь о `Mars.Host.Data.*` провайдерах БД Mars).

## Проверка

1. `EXPLAIN ANALYZE` запроса из `GetDetailBySlug` на большой таблице — должен быть
   Index Scan по новому индексу.
2. Нагрузочный стенд: `.\benchmarks\run-perf.ps1` — после `post_create`
   (БД раздувается до десятков тысяч постов) `post_update` не должен деградировать
   (до фикса: 627 → 202 ит/с; после фикса оба прогона дают сопоставимые цифры).
3. Прогнать интеграционные тесты постов/WebApiClient — поведение не меняется.
