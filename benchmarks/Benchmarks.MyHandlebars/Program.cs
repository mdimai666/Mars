using BenchmarkDotNet.Running;
using Benchmarks.MyHandlebars;

Console.WriteLine("Start!");

BenchmarkRunner.Run<MyHandlebarsCompileBenchmark>();

Console.ReadKey();