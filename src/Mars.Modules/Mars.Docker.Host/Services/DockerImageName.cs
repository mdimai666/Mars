namespace Mars.Docker.Host.Services;

/// <summary>
/// Разбор и нормализация имён образов: <c>[registry[:port]/][owner/]name[:tag]</c>.
/// </summary>
public static class DockerImageName
{
    /// <summary>
    /// Содержит ли имя собственный registry: первый сегмент с точкой/портом или <c>localhost</c>
    /// (правило docker CLI).
    /// </summary>
    public static bool HasRegistry(string image)
    {
        var slash = image.IndexOf('/');
        if (slash < 0)
        {
            return false;
        }

        var firstSegment = image[..slash];
        return firstSegment.Contains('.') || firstSegment.Contains(':') || firstSegment == "localhost";
    }

    /// <summary>Отделяет тег (последний <c>:</c> после последнего <c>/</c>); без тега — <c>latest</c>.</summary>
    public static (string Name, string Tag) SplitTag(string image)
    {
        var lastSlash = image.LastIndexOf('/');
        var colon = image.IndexOf(':', lastSlash + 1);
        if (colon < 0)
        {
            return (image, "latest");
        }

        return (image[..colon], image[(colon + 1)..]);
    }

    /// <summary>Подставляет registry-источник, если имя ещё не содержит registry.</summary>
    public static string ApplyRegistry(string image, string? registry)
    {
        if (string.IsNullOrWhiteSpace(registry) || HasRegistry(image))
        {
            return image;
        }

        return $"{registry.TrimEnd('/')}/{image}";
    }

    /// <summary>Полное локальное имя образа: registry + нормализованный тег.</summary>
    public static string ResolveFull(string image, string? registry)
    {
        var (name, tag) = SplitTag(image);
        return $"{ApplyRegistry(name, registry)}:{tag}";
    }
}
