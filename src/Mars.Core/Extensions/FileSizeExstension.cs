namespace Mars.Core.Extensions;

public static class FileSizeExstension
{
    public static string ToHumanizedSize(this long size)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = size;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
        // show a single decimal place, and no space.
        string result = string.Format("{0:0.##} {1}", len, sizes[order]);

        return result;
    }
    public static string ToHumanizedSize(this ulong size)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = size;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
        // show a single decimal place, and no space.
        string result = string.Format("{0:0.##} {1}", len, sizes[order]);

        return result;
    }

    public static string ToHumanizedSize(this int size)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = size;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
        // show a single decimal place, and no space.
        string result = string.Format("{0:0.##} {1}", len, sizes[order]);

        return result;
    }


    public static string ToCountHumanize(this int count)
    {

        if (count < 1000) return count.ToString();
        else if (count < 1000000) return string.Format("{0:0.00}K", count / 1000f);
        else return string.Format("{0:0.00}M", count / 1000000f);
    }

    public static string ToMoneyHumanize(this decimal count)
    {

        if (count < 1000) return count.ToString("#,###");
        else if (count > 1000000000) return string.Format("{0:0} млрд", count / 1000000000m);
        else if (count < 1000000) return string.Format("{0:0} тыс", count / 1000m);
        else return string.Format("{0:0} млн", count / 1000000m);
    }
}
