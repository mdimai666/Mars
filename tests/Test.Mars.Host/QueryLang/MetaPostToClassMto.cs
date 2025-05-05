using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AppShared.Models;
using Mars.GenSourceCode;
using Mars.Host.Data;
using Mars.Host.Services;
using Mars.Host.Shared.Services;
using Mars.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Test.Mars.Host.TestHostApp;

namespace Test.Mars.Host.QueryLang;

public class MetaPostToClassMto : UnitTestHostBaseClass
{
    [Fact]
    public void TestPostTypesAsMtoString()
    {
        using var ef = _serviceProvider.GetService<MarsDbContextLegacy>();

        var postTypeList = ef.PostTypes
                                .Include(s => s.MetaFields)
                                //.First(s => s.TypeName == postTypeName);
                                .ToList();

        var userService = _serviceProvider.GetService<UserService>();

        var userMetaFields = userService.UserMetaFields(ef);

        IMetaModelTypesLocator mlocator = _serviceProvider.GetRequiredService<IMetaModelTypesLocator>();

        string code = GenClassMto.GenPostTypesAsMtoString(postTypeList, userMetaFields, mlocator);

        Assert.True(code.Length > 100);
    }


}
