using BenchmarkDotNet.Running;
using Benchmarks.MyHandlebars.Memory;

Console.WriteLine("Start!");

BenchmarkRunner.Run<MyHandlebarsMemoryBenchmark>();

Console.ReadKey();