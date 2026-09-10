# Задача: индекс на Slug у постов

**Статус:** сделана (2026-08-16; миграция `20260815151932_AddPostSlugIndexes`, см. «Реализация» внизу).

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

## Реализация (2026-08-16)

1. Миграция `src/Mars.Host.Data.PostgreSQL/Migrations/20260815151932_AddPostSlugIndexes.cs`
   (raw SQL, имена в реальной схеме snake_case — не PascalCase, как в примере выше):
   - `ix_posts_post_type_id_slug_lower` ON `posts` (`post_type_id`, lower(`slug`)) — для
     `GetDetailBySlug` (тип + slug);
   - `ix_posts_slug_lower` ON `posts` (lower(`slug`)) — для QueryLang-запроса фронтов
     `ef.post.First(post.Slug.ToLower() == ...)` без фильтра по типу.

   Через `HasIndex`/`IEntityTypeConfiguration` выражение-индексы не объявляются (проверено:
   EF Core 10 падает на `HasIndex(x => x.Slug.ToLower())`; позиция мейнтейнера Npgsql —
   issue efcore.pg#293: только raw SQL или citext). В `PostEntityConfiguration` оставлен
   комментарий-ссылка на миграцию. Уникальность не добавляли (soft-delete + проверки кодом).

2. Пришлось поменять и сами запросы — одного индекса недостаточно: планировщик PostgreSQL
   **не использует** индекс по `lower(slug)` для `ILIKE` (проверено EXPLAIN'ом, оставался
   seq scan). В `src/Mars.Host.Repositories/PostRepository.cs`:
   - `GetDetailBySlug`: `EF.Functions.ILike(s.Slug, slug)` → `s.Slug.ToLower() == slug.ToLower()`;
   - `ExistAsync(typeName, slug)`: `s.Slug == slug` → сравнение через `ToLower()`
     (заодно регистронезависимость приведена к единой семантике).

## Результаты проверки (2026-08-16, БД раздута до ~36 000 постов)

- `EXPLAIN ANALYZE`: все формы запросов — Index Scan по новым индексам,
  0,06–0,09 мс (до фикса: Seq Scan 10–12 мс, в т.ч. для несуществующего slug).
- Интеграционные тесты `Mars.WebApiClient.Integration.Tests`: 177/177 (поведение не изменилось).
- k6 на раздутой БД (1000 посеяно + ~35k создано `post_create`), 30s full:

  | Сценарий | 1000 постов (до фикса) | раздутая БД до фикса | раздутая БД после фикса |
  |---|---:|---:|---:|
  | post_update | 627 ит/с | **202 ит/с** | **641 ит/с** |
  | post_read | 2 894 ит/с | — | 3 091 ит/с |
  | post_create | 1 224 ит/с | — | 1 174 ит/с (индексы запись не замедлили) |

  Деградация ×3 после `post_create` устранена.
