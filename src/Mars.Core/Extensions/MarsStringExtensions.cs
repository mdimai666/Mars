using System.Text.RegularExpressions;

namespace Mars.Core.Extensions;

public static class MarsStringExtensions
{
    public static string HelloText()
    {
        return """
              __  __          _____   _____ 
             |  \/  |   /\   |  __ \ / ____|
             | \  / |  /  \  | |__) | (___  
             | |\/| | / /\ \ |  _  / \___ \ 
             | |  | |/ ____ \| | \ \ ____) |
             |_|  |_/_/    \_\_|  \_\_____/ 
                                       
        """;
    }

    public static string AppFrontHelloText()
    {
        return """
                _                _____                _   
               / \   _ __  _ __ |  ___| __ ___  _ __ | |_ 
              / _ \ | '_ \| '_ \| |_ | '__/ _ \| '_ \| __|
             / ___ \| |_) | |_) |  _|| | | (_) | | | | |_ 
            /_/   \_\ .__/| .__/|_|  |_|  \___/|_| |_|\__|
                    |_|   |_|                             

        """;
    }

    public static string Right(this string str, int Length)
    {
        if (Length < 0) throw new ArgumentOutOfRangeException("Length");
        if (Length == 0 || str == null) return string.Empty;
        int len = str.Length;
        if (Length >= len) return str;
        else return str.Substring(len - Length, Length);
    }
    public static string Left(this string str, int Length)
    {
        if (Length < 0) throw new ArgumentOutOfRangeException("Length");
        if (Length == 0 || str == null) return string.Empty;
        int len = str.Length;
        if (Length >= len) return str;
        else return str.Substring(0, Length);
    }

    public static string TextEllipsis(this string str, int Length)
    {
        if (Length < 0) throw new ArgumentOutOfRangeException("Length");
        if (Length == 0 || str == null) return string.Empty;
        int len = str.Length;
        if (Length >= len) return str;
        else return str.Substring(0, Length) + "...";
    }

    public static string StripHTML(this string input)
    {
        return Regex.Replace(input, "<.*?>", String.Empty);
    }
}
