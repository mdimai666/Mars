using BenchmarkDotNet.Running;
using Benchmarks.DatabaseQuery;

Console.WriteLine("Start!");

BenchmarkRunner.Run<DatabaseQueryBenchmarkRun>();

Console.ReadKey();
