using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace Benchmarks.QueryLang;

[MemoryDiagnoser, ShortRunJob]
public class SampleBenchmarks
{

    IQueryable<int> list = default!;

    //public SampleBenchmarks()
    //{
    //    list = Enumerable.Range(0, 100).AsQueryable();
    //}

    [GlobalSetup]
    public void GlobalSetup()
    {
        list = Enumerable.Range(0, 100).AsQueryable();
    }

    [Benchmark(Baseline = true)]
    public void NativeCount()
    {
        for (int i = 0; i < 10; i++)
        {
            var result = list.Count();
        }
    }

    [Benchmark]
    public void ReflectionCount()
    {
        for (int i = 0; i < 10; i++)
        {
            MethodInfo method = typeof(Queryable)
              .GetMethods(BindingFlags.Static | BindingFlags.Public)
              //narrow the search before doing 'Single()'
              .Single(mi => mi.Name == "Count"
                         // this check technically not required, but more future proof
                         && mi.IsGenericMethodDefinition
                         && mi.GetParameters().Length == 1
                         )
              .MakeGenericMethod(typeof(int));

            var result = method.Invoke(list, new object[] { list }) as int?;

            //result!.Value;
        }
    }

}
