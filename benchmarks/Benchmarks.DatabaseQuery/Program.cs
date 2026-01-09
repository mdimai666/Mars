using BenchmarkDotNet.Running;
using Benchmarks.DatabaseQuery;

Console.WriteLine("Start!");


#if !DEBUG
BenchmarkRunner.Run<DatabaseQueryBenchmarkRun>(); 
#else
Console.WriteLine(new TestAppRun().Call2());
#endif


Console.ReadKey();