using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Factories;
using Mars.Nodes.Host.Shared;
using Microsoft.Extensions.DependencyInjection;

BenchmarkRunner.Run<InstantiationBenchmark>();

[MemoryDiagnoser]
[ShortRunJob]
public class InstantiationBenchmark
{
    private const int NodeCount = 800;
    private IRuntimeNodeScope _rns = null!;
    private Type _instantiateType = default!;
    private InjectNode _injectNode = default!;
    private IServiceProvider _serviceProvider = default!;
    private INodeImplementFactory _nodeImplementFactory = default!;
    private readonly ConcurrentDictionary<Type, ObjectFactory> _factoryCache = new();

    // === ARRANGE (Подготовка данных) ===
    [GlobalSetup]
    public void Setup()
    {
        _instantiateType = typeof(InjectNodeImpl);
        _injectNode = new InjectNode();
        _serviceProvider = new ServiceCollection().BuildServiceProvider();
        _rns = new RuntimeNodeScopeMock(_serviceProvider);

        var factory = _factoryCache.GetOrAdd(_instantiateType, type =>
                            ActivatorUtilities.CreateFactory(type, [_injectNode.GetType(), typeof(IRuntimeNodeScope)]));

        _nodeImplementFactory = new NodeImplementFactory();
        _nodeImplementFactory.RegisterAssembly(typeof(InjectNodeImpl).Assembly);
        _ = _nodeImplementFactory.Dict[typeof(InjectNode)];
    }

    //=== ACT & ASSERT(Тестирование подходов) ===

    [Benchmark(Baseline = true)]
    public INodeImplement[] DirectInstantiation()
    {
        // Нативный вызов через 'new' (Эталонная скорость)
        var results = new INodeImplement[NodeCount];

        for (int i = 0; i < NodeCount; i++)
        {
            results[i] = new InjectNodeImpl(_injectNode, _rns);
        }

        return results;
    }

    [Benchmark]
    public INodeImplement[] ActivatorInstantiation()
    {
        // Вызов через Activator.CreateInstance с передачей массива аргументов
        var results = new INodeImplement[NodeCount];

        for (int i = 0; i < NodeCount; i++)
        {
            // Эмуляция вашей логики с проверкой ctorArgumentsCount == 2
            results[i] = (INodeImplement)Activator.CreateInstance(_instantiateType, [_injectNode, _rns])!;
        }

        return results;
    }

    [Benchmark]
    public INodeImplement[] ActivatorUtilitiesCreate()
    {
        // Вызов через ActivatorUtilities.CreateInstance с передачей массива аргументов
        var results = new INodeImplement[NodeCount];

        for (int i = 0; i < NodeCount; i++)
        {
            // Эмуляция вашей логики с проверкой ctorArgumentsCount == 2
            results[i] = (INodeImplement)ActivatorUtilities.CreateInstance(_serviceProvider, instanceType: _instantiateType, parameters: [_injectNode, _rns])!;
        }

        return results;
    }

    [Benchmark]
    public INodeImplement[] ActivatorUtilitiesCreateWithTryCatch()
    {
        // Вызов через ActivatorUtilities.CreateInstance с передачей массива аргументов
        var results = new INodeImplement[NodeCount];

        for (int i = 0; i < NodeCount; i++)
        {
            // Эмуляция вашей логики с проверкой ctorArgumentsCount == 2
            try
            {
                results[i] = (INodeImplement)ActivatorUtilities.CreateInstance(_serviceProvider, instanceType: _instantiateType, parameters: [_injectNode, _rns])!;
            }
            catch (Exception ex)
            {
                string x = ex.Message;
            }
        }

        return results;
    }

    [Benchmark]
    public INodeImplement[] ActivatorUtilitiesCreateWithFactoryCache()
    {
        // Вызов через ActivatorUtilities.CreateInstance с передачей массива аргументов
        var results = new INodeImplement[NodeCount];

        for (int i = 0; i < NodeCount; i++)
        {
            var factory = _factoryCache.GetOrAdd(_instantiateType, type =>
                            ActivatorUtilities.CreateFactory(type, [_injectNode.GetType(), typeof(IRuntimeNodeScope)]));

            results[i] = (INodeImplement)factory(_serviceProvider, [_injectNode, _rns])!;
        }

        return results;
    }

    [Benchmark]
    public INodeImplement[] NodeImplementFactoryOriginal()
    {
        // Вызов через ActivatorUtilities.CreateInstance с передачей массива аргументов
        var results = new INodeImplement[NodeCount];

        for (int i = 0; i < NodeCount; i++)
        {

            results[i] = _nodeImplementFactory.Create(_injectNode, _rns);
        }

        return results;
    }
}
