using BenchmarkDotNet.Running;
using Benchmarks.MyHandlebars;

Console.WriteLine("Start!");

BenchmarkRunner.Run<MyHandlebarsMemoryBenchmark>();

Console.ReadKey();