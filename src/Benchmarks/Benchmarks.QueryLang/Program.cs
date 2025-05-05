using BenchmarkDotNet.Running;
using Benchmarks.QueryLang;

Console.WriteLine("Start!");

BenchmarkRunner.Run<SampleBenchmarks>();

//var z = new SampleBenchmarks();

//int count = z.ReflectionCount();

//Console.WriteLine("count=" + count);

Console.ReadKey();