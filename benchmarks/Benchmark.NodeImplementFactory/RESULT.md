// * Summary *

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8524/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i7-13700 2.10GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.300
  [Host]   : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1
WarmupCount=3

| Method                                   | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------- |----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| DirectInstantiation                      |  2.436 us | 0.7106 us | 0.0389 us |  1.00 |    0.02 |  2.0409 | 0.2251 |  31.27 KB |        1.00 |
| ActivatorInstantiation                   | 83.955 us | 4.5648 us | 0.2502 us | 34.47 |    0.48 | 16.2354 | 1.7090 | 250.02 KB |        7.99 |
| ActivatorUtilitiesCreate                 | 33.696 us | 3.9785 us | 0.2181 us | 13.83 |    0.21 |  4.0283 | 0.4272 |  62.52 KB |        2.00 |
| ActivatorUtilitiesCreateWithTryCatch     | 36.325 us | 9.2600 us | 0.5076 us | 14.91 |    0.27 |  4.0283 | 0.4272 |  62.52 KB |        2.00 |
| ActivatorUtilitiesCreateWithFactoryCache | 12.303 us | 0.8102 us | 0.0444 us |  5.05 |    0.07 |  7.3395 | 0.8087 | 112.52 KB |        3.60 |
| NodeImplementFactoryOriginal             | 36.259 us | 5.8390 us | 0.3201 us | 14.89 |    0.23 |  4.0283 | 0.4272 |  62.52 KB |        2.00 |
