using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mars.Core.Features;

public struct BracketPair
{
    private int startIndex;
    private int endIndex;
    private int depth;

    public BracketPair(int startIndex, int endIndex, int depth)
    {
        if (startIndex > endIndex)
            throw new ArgumentException("startIndex must be less than endIndex");

        this.startIndex = startIndex;
        this.endIndex = endIndex;
        this.depth = depth;
    }

    public int StartIndex
    {
        get { return startIndex; }
    }

    public int EndIndex
    {
        get { return endIndex; }
    }

    public int Depth
    {
        get { return depth; }
    }
}

public struct ChainPair
{
    public int StartIndex { get; init; }
    public int EndIndex { get; init; }
    public int Depth { get; init; }
    public string Method { get; init; }
    public string Argument { get; init; }

    public ChainPair(int startIndex, int endIndex, int depth, string method, string argument)
    {
        if (startIndex > endIndex)
            throw new ArgumentException("startIndex must be less than endIndex");

        StartIndex = startIndex;
        EndIndex = endIndex;
        Depth = depth;
        Method = method;
        Argument = argument;
    }
}

public static class TextHelper
{
    public static IEnumerable<BracketPair> ParseBracketPairs(string text)
    {
        var startPositions = new Stack<int>();

        for (int index = 0; index < text.Length; index++)
            if (text[index] == '(')
            {
                startPositions.Push(index);
            }
            else if (text[index] == ')')
            {
                if (startPositions.Count == 0)
                    throw new ArgumentException(string.Format("mismatched end bracket at index {0}", index));

                var depth = startPositions.Count - 1;
                var start = startPositions.Pop();

                yield return new BracketPair(start, index, depth);
            }

        if (startPositions.Count > 0)
            throw new ArgumentException(string.Format("mismatched start brackets, {0} total", startPositions.Count));
    }

    // You can even go one step further and handle TextReaders  
    // Remember you need using System.IO
    public static IEnumerable<BracketPair> ParseBracketPairs(TextReader reader)
    {
        var startPositions = new Stack<int>();

        for (int index = 0; reader.Peek() != -1; ++index)
        {
            // Detect overflow
            if (index < 0)
                throw new ArgumentException(string.Format("input text too long, must be shorter than {0} characters", int.MaxValue));

            var c = (char)reader.Read();
            if (c == '(')
            {
                startPositions.Push(index);
            }
            else if (c == ')')
            {
                // Error on mismatch
                if (startPositions.Count == 0)
                    throw new ArgumentException(string.Format("mismatched end bracket at index {0}", index));

                // Depth tends to be zero-based
                var depth = startPositions.Count - 1;
                var start = startPositions.Pop();

                yield return new BracketPair(start, index, depth);
            }
        }

        // Error on mismatch
        if (startPositions.Count > 0)
            throw new ArgumentException(string.Format("mismatched start brackets, {0} total", startPositions.Count));
    }

    public static IEnumerable<ChainPair> ParseChainPair(string text)
    {
        IEnumerable<BracketPair> pairs = ParseBracketPairs(text);

        foreach (var a in pairs.Where(s => s.Depth == 0))
        {
            int si = a.StartIndex;
            var c = text[si];
            while (si > 0 && text[si] != '.')
            {
                si--;
            }
            c = text[si];

            string method = "";
            string argument = "";

            if (c == '.' || si == 0)
            {
                method = text.Substring(si + 1, a.StartIndex - si - 1);
                argument = text.Substring(a.StartIndex + 1, a.EndIndex - a.StartIndex - 1);
            }

            yield return new ChainPair(a.StartIndex, a.EndIndex, a.Depth, method, argument);
        }
    }

    public static IEnumerable<KeyValuePair<string, string>> ParseChainPairKeyValue(string text)
    {
        return ParseChainPair(text).Select(s => new KeyValuePair<string, string>(s.Method, s.Argument));
    }

    public static string[] ParseArguments(string text)
    {
        var chain = ParseChainPair(text);
        List<string> args = new();

        if (chain.Count() != 1) throw new ArgumentException($"ParseArguments BracketPair must be 1, give {chain.Count()}");

        var t = chain.First().Argument;

        var pairs = ParseBracketPairs(t);

        if (pairs.Count() == 0) return t.Split(",");

        int startIndex = 0;
        for (int i = 1; i < t.Length; i++)
        {
            bool last = i == t.Length - 1;
            if (t[i - 1] == ',' || last)
            {
                bool inPairs = false;
                foreach (var pair in pairs)
                {
                    if (pair.StartIndex >= i && pair.EndIndex <= i)
                    {
                        inPairs = true;
                        break;
                    }
                }

                if (!inPairs)
                {
                    int length = last ? (i - startIndex + 1) : (i - startIndex - 1);
                    string cp = t.Substring(startIndex, length).Trim();
                    args.Add(cp);

                    startIndex = i;
                }

            }
        }

        return args.ToArray();
    }

    public static string[] SplitArguments(string text)
    {
        string roval = "";
        int isOpen = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (isOpen <= 0 && text[i] == ',')
            {
                roval += ",~,";
            }
            else if (isOpen > 0 && (text[i] == ')' || text[i] == ']' || text[i] == '}'))
            {
                isOpen--;
                roval += text[i];
            }
            else if ((text[i] == '(' || text[i] == '[' || text[i] == '{'))
            {
                isOpen++;
                roval += text[i];
            }
            else
            {
                roval += text[i];
            }
        }

        string[] split = roval.Split(",~,", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return split;
    }

}
