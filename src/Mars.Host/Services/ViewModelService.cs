using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using Mars.Host.Data;

namespace Mars.Host.Services;


public class ViewModelService//NOT USED
{
    readonly string _connectionString;
    private readonly IConfiguration configuration;
    private readonly IServiceProvider serviceProvider;

    static Dictionary<string, Type> registered = new Dictionary<string, Type>();

    public ViewModelService(IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        this.configuration = configuration;
        this.serviceProvider = serviceProvider;

        //ModelService.GetInterfaceImplements<IViewModelBasic>();

        //Register<IEditUserViewModel, EditUserViewModel>();
    }


    //void Register<T, T2>()
    //    where T2 : IViewModelBasic
    //{
    //    Type t = typeof(T);
    //    registered.Add(t.Name, typeof(T2));
    //}

    //public static void RegisterAllViewModels<T>()
    //{
    //    var type = typeof(T);
    //    var types = AppDomain.CurrentDomain.GetAssemblies()
    //        .SelectMany(s => s.GetTypes())
    //        .Where(p =>
    //            type.IsAssignableFrom(p)
    //            && p.IsClass
    //            && !p.IsAbstract
    //        );

    //    var tt = typeof(IViewModelBasic);

    //    foreach (var f in types)
    //    {
    //        var q = f;

    //        var ii = f.GetInterfaces().Where(s => s != tt);

    //        if (ii.Count() > 1) throw new Exception("Soo many implement classes");

    //        var clientInterface = ii.First();

    //        registered.Add(clientInterface.Name, f);

    //    }

    //}

    static IEnumerable<Type> GetImplementingTypes(Type itype)
        => AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes())
               .Where(t => t.GetInterfaces().Contains(itype));

    //public object GetViewModelByName(string vmName)
    //{
    //    registered.TryGetValue(vmName, out var viewModel);
    //    if (viewModel == null) return null;

    //    //var instance = ActivatorUtilities.CreateInstance(serviceProvider, viewModel);
    //    IViewModelBasic instance = Activator.CreateInstance(viewModel) as IViewModelBasic;
    //    ////IViewModelBasic instance = (viewModel as IViewModelBasic).Create(serviceProvider, GetEFContext());
    //    IViewModelBasic create = instance.Create(serviceProvider, GetEFContext());

    //    return create;
    //}
}

