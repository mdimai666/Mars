using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Services;
using Mars.Shared.Options;
using Mars.Test.Common.Constants;
using Mars.Test.Common.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Performance.Stand;

public static class PerfSeeder
{
    public const string FrontSlug = "perf-default";
    public const string PostSlugPrefix = "post-";

    /// <summary>
    /// Регистрирует фронт на дефолтном стартовом шаблоне (src/Mars.WebApp/Res/front_templates/default):
    /// "/" — статика, "/posts" — список из БД (QueryLang), "/posts/{Slug}" — страница поста из БД.
    /// </summary>
    public static void EnsureFront(IServiceProvider serviceProvider)
    {
        var optionService = serviceProvider.GetRequiredService<IOptionService>();
        var option = optionService.GetOption<FrontsOption>();
        if (option.Fronts.Any(s => s.Slug == FrontSlug)) return;

        var themePath = SolutionPathHelper.Resolve("src", "Mars.WebApp", "Res", "front_templates", "default");
        option.Fronts.Add(new FrontItem
        {
            Slug = FrontSlug,
            Title = "Perf: default theme",
            Url = "",
            Path = themePath,
            EngineId = FrontItem.HandlebarsEngine,
            Enabled = true,
        });
        optionService.SaveOption(option);
        Console.WriteLine($"[seed] фронт '{FrontSlug}' -> {themePath}");
    }

    /// <summary>
    /// Посты для чтения: post-0001..post-N типа "post", статус publish.
    /// </summary>
    public static async Task SeedPostsAsync(IServiceProvider serviceProvider, int count)
    {
        var ef = serviceProvider.GetRequiredService<IMarsDbContextFactory>().CreateInstance();

        var postType = await ef.PostTypes.AsNoTracking().FirstAsync(t => t.TypeName == "post");
        var user = await ef.Users.AsNoTracking().FirstAsync(u => u.UserName == UserConstants.TestUserUsername);
        var statuses = await ef.PostStatuses.AsNoTracking()
                                            .Where(s => s.PostTypeId == postType.Id)
                                            .OrderBy(s => s.Order)
                                            .ToListAsync();
        var statusId = statuses.FirstOrDefault(s => s.Slug == "publish")?.Id
                       ?? statuses.FirstOrDefault()?.Id;

        var existing = await ef.Posts.CountAsync(p => p.Slug.StartsWith(PostSlugPrefix));
        if (existing >= count)
        {
            Console.WriteLine($"[seed] посты уже в базе ({existing} >= {count}) — пропускаю");
            return;
        }

        Console.WriteLine($"[seed] создаю {count} постов '{PostSlugPrefix}0001'..'{PostSlugPrefix}{count:D4}'...");
        var sw = Stopwatch.StartNew();
        const int batchSize = 200;
        var now = DateTimeOffset.Now;

        for (int i = 1; i <= count; i++)
        {
            ef.Posts.Add(new PostEntity
            {
                Id = Guid.NewGuid(),
                Title = $"Perf post {i:D4}",
                Slug = $"{PostSlugPrefix}{i:D4}",
                PostTypeId = postType.Id,
                UserId = user.Id,
                StatusId = statusId,
                Content = BuildContent(i),
                Excerpt = $"Отрывок поста №{i} для нагрузочного стенда.",
                Tags = ["perf", $"tag{i % 5}"],
                // разнесённые даты — OrderByDescending(CreatedAt) в /posts не вырождается
                CreatedAt = now.AddDays(-i / 24.0),
            });

            if (i % batchSize == 0)
            {
                await ef.SaveChangesAsync();
                ef.ChangeTracker.Clear();
                Console.WriteLine($"[seed] {i}/{count} ({sw.Elapsed.TotalSeconds:F1}s)");
            }
        }

        await ef.SaveChangesAsync();
        Console.WriteLine($"[seed] готово: {count} постов за {sw.Elapsed.TotalSeconds:F1}s");
    }

    /// <summary> Прогрев шаблонов и JIT до замеров. </summary>
    public static async Task WarmupHttpAsync(string baseUrl)
    {
        using var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromMinutes(1) };
        var urls = new[]
        {
            "/",
            "/posts",
            $"/posts/{PostSlugPrefix}0001",
            "/api/Post/list/page?page=1&pageSize=20",
            $"/api/Post/by-type/post/item/{PostSlugPrefix}0001",
        };

        for (int round = 1; round <= 2; round++)
        {
            foreach (var url in urls)
            {
                try { await http.GetAsync(url); }
                catch (Exception ex) { Console.WriteLine($"[warmup] {url}: {ex.Message}"); }
            }
        }
        Console.WriteLine("[warmup] страницы и API прогреты");
    }

    /// <summary>
    /// Быстрая проверка всех сценариев k6 одним запросом каждый.
    /// Возвращает число упавших проверок (0 — стенд готов к замерам).
    /// </summary>
    public static async Task<int> SmokeAsync(string baseUrl)
    {
        Console.WriteLine();
        Console.WriteLine("=== Smoke-проверка стенда ===");
        var failed = 0;

        using var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromMinutes(1) };

        failed += await Check("рендер статики        GET /", async () =>
        {
            var body = await http.GetStringAsync("/");
            return body.Contains("Welcome to Mars Handlebars Site") ? null : "маркер 'Welcome to Mars Handlebars Site' не найден";
        });

        failed += await Check("рендер с данными БД   GET /posts", async () =>
        {
            var body = await http.GetStringAsync("/posts");
            return body.Contains("Perf post") ? null : "посты не найдены на странице /posts";
        });

        failed += await Check("рендер поста из БД    GET /posts/post-0001", async () =>
        {
            var body = await http.GetStringAsync($"/posts/{PostSlugPrefix}0001");
            return body.Contains("Perf post 0001") ? null : "пост post-0001 не найден";
        });

        string? token = null;
        failed += await Check("логин                 POST /api/Account/Login", async () =>
        {
            var res = await http.PostAsJsonAsync("/api/Account/Login", new
            {
                login = UserConstants.TestUserUsername,
                password = UserConstants.TestUserPassword,
            });
            if (res.StatusCode != System.Net.HttpStatusCode.OK)
                return $"статус {(int)res.StatusCode}";
            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            token = json.GetProperty("token").GetString();
            return string.IsNullOrEmpty(token) ? "пустой token в ответе" : null;
        });

        if (token is null)
        {
            Console.WriteLine("  [SKIP] авторизованные проверки без token");
            return failed;
        }

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        failed += await Check("чтение поста          GET /api/Post/by-type/post/item/post-0001", async () =>
        {
            var res = await http.GetAsync($"/api/Post/by-type/post/item/{PostSlugPrefix}0001");
            return await ExpectStatus(res, 200);
        });

        failed += await Check("список постов         GET /api/Post/list/page", async () =>
        {
            var res = await http.GetAsync("/api/Post/list/page?page=1&pageSize=20");
            return await ExpectStatus(res, 200);
        });

        var slug = $"smoke-create-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        failed += await Check("создание поста        POST /api/Post", async () =>
        {
            var res = await http.PostAsJsonAsync("/api/Post", CreatePostBody(slug, "Smoke create"));
            return await ExpectStatus(res, 201);
        });

        failed += await Check("обновление поста      PUT /api/Post", async () =>
        {
            var readBack = await http.GetFromJsonAsync<JsonElement>($"/api/Post/by-type/post/item/{slug}");
            var id = readBack.GetProperty("id").GetGuid();
            var res = await http.PutAsJsonAsync("/api/Post", UpdatePostBody(id, slug, "Smoke create (updated)"));
            return await ExpectStatus(res, 200);
        });

        Console.WriteLine(failed == 0
            ? "=== Smoke: все проверки прошли ==="
            : $"=== Smoke: {failed} проверок упало ===");
        return failed;
    }

    public static object CreatePostBody(string slug, string title) => new
    {
        id = (Guid?)null,
        title,
        type = "post",
        slug,
        tags = Array.Empty<string>(),
        content = "<p>Создан нагрузочным стендом.</p>",
        status = "publish",
        excerpt = "",
        langCode = "",
        categoryIds = Array.Empty<Guid>(),
        metaValues = Array.Empty<object>(),
    };

    public static object UpdatePostBody(Guid id, string slug, string title) => new
    {
        id,
        title,
        type = "post",
        slug,
        tags = Array.Empty<string>(),
        content = "<p>Обновлён нагрузочным стендом.</p>",
        status = "publish",
        excerpt = "",
        langCode = "",
        categoryIds = Array.Empty<Guid>(),
        metaValues = Array.Empty<object>(),
    };

    public static void PrintK6Hint(string baseUrl)
    {
        Console.WriteLine();
        Console.WriteLine("Замер k6 (из корня репозитория):");
        Console.WriteLine($"  $env:MARS_URL=\"{baseUrl}\"; k6 run benchmarks/k6/run-all.js");
    }

    private static async Task<string?> ExpectStatus(HttpResponseMessage res, int expected)
    {
        if ((int)res.StatusCode == expected) return null;
        var body = await res.Content.ReadAsStringAsync();
        return $"ожидал {expected}, получил {(int)res.StatusCode}: {Truncate(body)}";
    }

    private static async Task<int> Check(string name, Func<Task<string?>> action)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var error = await action();
            if (error is null)
            {
                Console.WriteLine($"  [OK]   {name} ({sw.ElapsedMilliseconds} ms)");
                return 0;
            }
            Console.WriteLine($"  [FAIL] {name}: {error}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] {name}: {Truncate(ex.Message)}");
            return 1;
        }
    }

    private static string Truncate(string value, int max = 300)
        => value.Length <= max ? value : value[..max] + "...";

    private static string BuildContent(int i) => $"""
        <p>Это тестовый пост №{i} для нагрузочного стенда Mars Performance Stand.</p>
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor
        incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud
        exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.</p>
        <p>Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu
        fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa
        qui officia deserunt mollit anim id est laborum.</p>
        <ul><li>Элемент A-{i}</li><li>Элемент B-{i}</li><li>Элемент C-{i}</li></ul>
        """;
}
