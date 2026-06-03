// * Summary *

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8524/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i7-13700 2.10GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.300
  [Host]   : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1
WarmupCount=3

| Method                | Mean           | Error         | StdDev       | Gen0   | Gen1   | Allocated |
|---------------------- |---------------:|--------------:|-------------:|-------:|-------:|----------:|
| 'Handlebars (Cached)' |       366.9 ns |      14.80 ns |      0.81 ns | 0.0558 |      - |     880 B |
| 'Regex (Cached)'      |       842.1 ns |      57.43 ns |      3.15 ns | 0.1049 |      - |    1648 B |
| 'Regex (Cold)'        |     3,006.0 ns |     685.14 ns |     37.55 ns | 0.4272 |      - |    6808 B |
| 'Scriban (Cached)'    |     9,547.3 ns |  30,768.33 ns |  1,686.52 ns | 2.0447 | 0.1831 |   32291 B |
| 'Scriban (Cold)'      |    12,755.4 ns |   5,448.12 ns |    298.63 ns | 2.2583 | 0.1831 |   35618 B |
| 'RazorStyle (Cached)' |    12,789.8 ns |  48,321.76 ns |  2,648.68 ns | 2.0447 | 0.2136 |   32304 B |
| 'RazorStyle (Cold)'   |    14,537.0 ns |  25,408.60 ns |  1,392.73 ns | 2.2583 | 0.1831 |   36183 B |
| 'Handlebars (Cold)'   | 2,104,955.4 ns | 730,064.76 ns | 40,017.31 ns | 3.9063 |      - |   89425 B |
