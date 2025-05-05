using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.Host.Data;
using Mars.Host.QueryLang;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Templators;
using Microsoft.Extensions.DependencyInjection;
using Test.Mars.Host.TestHostApp;
using Xunit.Abstractions;

namespace Test.Mars.HandlebarsProcessor;

public class UnitTestContextCache : UnitTestHostBaseClass
{

    private readonly ITestOutputHelper output;

    public UnitTestContextCache(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void TestCache()
    {

        var renderContext = _renderContext();
        XInterpreter ppt = new(renderContext, null);
        var ef = _serviceProvider.GetService<MarsDbContextLegacy>();
        EntityQuery eq = new EntityQuery(_serviceProvider, ppt, null);

        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        //string query1 = @"doctor.Count()";
        //int doctorCount = (int)eq.Query(query1);

        var mlocator = _serviceProvider.GetRequiredService<IMetaModelTypesLocator>();

        mlocator.TryUpdateMetaModelMtoRuntimeCompiledTypes(_serviceProvider);

        string contextQuery = @"services = service.OrderBy(Created).Take(100).Fill()
    photos = photo.OrderBy(Created).Take(100).Fill()
    doctors = doctor.Take(100).Fill()
    news = news.OrderByDescending(Created).Take(10).Fill()
    price_categories = price_category.OrderBy(Created).Take(100)
    prices = price.OrderBy(Created).Take(500).Fill()
    works = work.OrderByDescending(Created).Take(100).Fill()
    reviews = review.OrderByDescending(Created).Take(100).Fill()
    faqs = faq.Take(100)
    videos = video.OrderByDescending(Created).Take(100)";

        foreach(var row in contextQuery.Split('\n').Take(2))
        {
            string q = row.Split('=')[1].Trim();
            var result = eq.Query(q);
        }

        stopWatch.Stop();

        output.WriteLine($"elapsed: {stopWatch.Elapsed.Milliseconds}ms");


        //Assert.True(doctorCount > 0);
    }
}
