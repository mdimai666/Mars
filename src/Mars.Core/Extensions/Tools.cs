using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Mars.Core.Extensions;

public static class Tools
{

    public static List<E> ShuffleList<E>(List<E> inputList)
    {
        List<E> randomList = new List<E>();
        Random random = new();

        int randomIndex = 0;
        while (inputList.Count > 0)
        {
            randomIndex = random.Next(0, inputList.Count); //Choose a random object in the list
            randomList.Add(inputList[randomIndex]); //add it to the new, random list
            inputList.RemoveAt(randomIndex); //remove to avoid duplicates
        }

        return randomList; //return the new random list  
    }

    /// <summary>
    /// n - 1
    /// Return arrange int etc. 0 1 2 3 4 
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public static IEnumerable<int> IntRange(int count)
    {
        return Enumerable.Range(0, count);
    }


    /// <summary>   
    /// Return Unique int enumerate
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public static IEnumerable<int> RandomShuffledInt(int count)
    {
        List<int> arr = IntRange(count).ToList();
        List<int> list = new List<int>();
        Random random = new();

        //System.Random r = new System.Random();

        for (int i = 0; i < count; i++)
        {
#if UNITY
            //int index = r.Next(arr.Count);  если быстро вызывать то рандом не рандом, читать C# random
            fix it please throw new NotImplementedException();
#endif
            int index = random.Next(0, arr.Count);
            list.Add(arr[index]);
            arr.RemoveAt(index);
        }

        return list;
    }

    public static List<E> Shuffle<E>(this List<E> inputList)
        => ShuffleList(inputList);

    public static IEnumerable<E> TakeRandomElements<E>(IEnumerable<E> inputList, int returnItemsCount)
    {
        //IEnumerable<E> shuffled = inputList.OrderBy(s => Guid.NewGuid()).Take(returnItemsCount).ToList();
        //return shuffled;
        int[] e = RandomShuffledInt(inputList.Count()).Take(returnItemsCount).ToArray();
        List<E> list = new List<E>();

        for (int i = 0; i < e.Length; i++)
        {
            list.Add(inputList.ElementAt(e[i]));
        }

        return list;
    }

    /// <summary>
    /// Alias of Linq.Distinct
    /// </summary>
    public static IEnumerable<E> UniqueItems<E>(this IEnumerable<E> inputList)
    {
        return inputList.Distinct<E>();
    }

    /// <summary>
    /// Alias of Linq.Distinct
    /// </summary>

    public static IEnumerable<T> UniqueItems<T, TKey>(this IEnumerable<T> items, Func<T, TKey> property)
    {
        QGeneralPropertyComparer<T, TKey> comparer = new QGeneralPropertyComparer<T, TKey>(property);
        return items.Distinct(comparer);
    }

    //not requred in new NET6 was added
    //public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> property)
    //{
    //    QGeneralPropertyComparer<T, TKey> comparer = new QGeneralPropertyComparer<T, TKey>(property);
    //    return items.Distinct(comparer);
    //}

    class QGeneralPropertyComparer<T, TKey> : IEqualityComparer<T>
    {
        private Func<T, TKey> expr { get; set; }
        public QGeneralPropertyComparer(Func<T, TKey> expr)
        {
            this.expr = expr;
        }
        public bool Equals(T? left, T? right)
        {
            if (left is null || right is null) return false;
            var leftProp = expr.Invoke(left);
            var rightProp = expr.Invoke(right);
            if (leftProp == null && rightProp == null)
                return true;
            else if (leftProp == null ^ rightProp == null)
                return false;
            else
                return leftProp?.Equals(rightProp) ?? false;
        }
        public int GetHashCode(T obj)
        {
            var prop = expr.Invoke(obj);
            return (prop == null) ? 0 : prop.GetHashCode();
        }
    }

    public static IEnumerable<T> Except<T>(this IEnumerable<T> first, T second)
    {
        return first.Except<T>(new T[] { second });
    }

    public static IEnumerable<E> TakeRandomRange<E>(this IEnumerable<E> inputList, int returnItemsCount)
        => TakeRandomElements(inputList, returnItemsCount);

    public static E TakeRandom<E>(this IEnumerable<E> inputList)
    {
#if NET8_0_OR_GREATER
        int index = Random.Shared.Next(0, inputList.Count());
#else
        Random random = new();
        int index = random.Next(0, inputList.Count()); 
#endif
        return inputList.ElementAt(index);
    }

    public static string JoinStr(this IEnumerable<string> first, string separator)
    {
        return string.Join(separator, first);
    }

    public static string JoinStr(this IEnumerable<char> first, string separator = "")
    {
        return string.Join(separator, first);
    }

    public static IEnumerable<string> TrimNulls(this IEnumerable<string>? arr)
    {
        if (arr == null) throw new ArgumentException();
        return arr.Where(s => s != null).ToArray()!;
    }

    public static string[] TrimNulls(this string?[] arr)
    {
        if (arr == null) throw new ArgumentException();
        return arr.Where(s => s != null).ToArray()!;
    }

    public static string[] TrimNullOrEmpty(this string?[] arr)
    {
        if (arr == null) throw new ArgumentException();
        return arr.Where(s => !string.IsNullOrEmpty(s)).ToArray()!;
    }

    public static void SetTimeout(Action action, int delayMillis)
        => SetTimeout(action, TimeSpan.FromMilliseconds(delayMillis));

    //TODO: Not tested with Unity Main thread only functions
    public static void SetTimeout(Action action, TimeSpan delay)
    {
        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            await System.Threading.Tasks.Task.Delay(delay);
            action();
        });
    }

    static JsonSerializerSettings _defaultConvertSetting = new JsonSerializerSettings
    {
        Formatting = Formatting.None,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    };

    public static T CopyViaJsonConversion<T>(this object source)
    {
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(source, _defaultConvertSetting);
        T? b = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json, _defaultConvertSetting);
        return b!;
    }

    public static string Translit(string str)
    {
        string[] lat_up = { "A", "B", "V", "G", "D", "E", "Yo", "Zh", "Z", "I", "Y", "K", "L", "M", "N", "O", "P", "R", "S", "T", "U", "F", "Kh", "Ts", "Ch", "Sh", "Shch", "\"", "Y", "'", "E", "Yu", "Ya" };
        string[] lat_low = { "a", "b", "v", "g", "d", "e", "yo", "zh", "z", "i", "y", "k", "l", "m", "n", "o", "p", "r", "s", "t", "u", "f", "kh", "ts", "ch", "sh", "shch", "\"", "y", "'", "e", "yu", "ya" };
        string[] rus_up = { "А", "Б", "В", "Г", "Д", "Е", "Ё", "Ж", "З", "И", "Й", "К", "Л", "М", "Н", "О", "П", "Р", "С", "Т", "У", "Ф", "Х", "Ц", "Ч", "Ш", "Щ", "Ъ", "Ы", "Ь", "Э", "Ю", "Я" };
        string[] rus_low = { "а", "б", "в", "г", "д", "е", "ё", "ж", "з", "и", "й", "к", "л", "м", "н", "о", "п", "р", "с", "т", "у", "ф", "х", "ц", "ч", "ш", "щ", "ъ", "ы", "ь", "э", "ю", "я" };
        for (int i = 0; i <= 32; i++)
        {
            str = str.Replace(rus_up[i], lat_up[i]);
            str = str.Replace(rus_low[i], lat_low[i]);
        }
        return str;
    }

    static readonly Regex reg_whitespace = new Regex(@"\s+");
    static readonly Regex reg_nonValidSymbols = new Regex(@"[^\d\w-_.]");

    public static string TranslateToPostSlug(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return "";
        string translited = Translit(str.Trim().ToLower());
        return reg_nonValidSymbols.Replace(reg_whitespace.Replace(translited, "_"), "");
    }

    static readonly Regex slugReg = new Regex(@"^[a-z\d_](?:[a-z\d-_.]*[a-z\d_])?$");
    static readonly Regex slugWithUpperReg = new Regex(@"^[A-Za-z\d_](?:[A-Za-z\d-_.]*[A-Za-z\d_])?$");

    public static bool IsValidSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return false;
        return slugReg.IsMatch(slug);
    }

    public static bool IsValidSlugWithUpperCase(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return false;
        return slugWithUpperReg.IsMatch(slug);
    }
}
