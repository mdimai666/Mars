using System.Reflection;
using Mars.Host.Data.Entities;
using Mars.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using TestApp.Mars.Host.Data;

Console.WriteLine("Hello, World!");

using var ef = new MarsDbContextFactory().CreateDbContext([]);
string entrypointApp = Assembly.GetEntryAssembly().GetName().Name!;
Console.WriteLine($"entrypointApp={entrypointApp}");
if (entrypointApp != "ef")
{
    //ef.Database.EnsureCreated(); dotnet use its create without migrations table
    if (ef.Database.GetPendingMigrations().Any()) ef.Database.Migrate();
}

//var env = Environment.GetEnvironmentVariables();
//StringBuilder sb = new();
//foreach (var key in env.Keys)
//{
//    sb.AppendLine($"{key}={env[key]}");
//}
//Console.WriteLine(sb);
//Console.WriteLine(args.JoinStr("; "));


var options = ef.Options.ToList();

if (options.Count == 0)
{
    var opt = new OptionEntity()
    {
        Key = "key1",
        Type = typeof(OptionEntity).Name,
        Value = "value1"
    };
    ef.Options.Add(opt);
    ef.SaveChanges();
}

if (ef.Users.Count() == 0)
{
    Enumerable.Range(0, 2).ToList().ForEach(i =>
    {
        var user = new UserEntity()
        {
            Id = Guid.NewGuid(),
            FirstName = "Ivan",
            LastName = "Ivanov",
            Email = "example@mail.ru",
            BirthDate = new DateTime(1991, 2, 15),
            UserName = "username1",
        };
        user.UserName = "test" + i;
        ef.Users.Add(user);
    });
    ef.SaveChanges();
}

//var users = ef.Users.ToList();

if (ef.Files.Count() == 0)
{
    var user = ef.Users.OrderBy(x => x.UserName).First();
    var file = new FileEntity
    {
        FileExt = "txt",
        FileName = "test.txt",
        FilePhysicalPath = "/text.txt",
        FileSize = 100,
        FileVirtualPath = "",
        UserId = user.Id,
    };
    ef.Files.Add(file);
    ef.SaveChanges();
}

if (ef.Posts.Count() == 0)
{
    var user = ef.Users.First();
    var pt = new PostTypeEntity()
    {
        Title = "post",
        TypeName = "post",
    };

    var post1 = new PostEntity()
    {
        Title = "title1",
        Content = "content",
        Slug = "slug1",
        Status = "draft",
        PostTypeId = pt.Id,
        UserId = user.Id,
    };

    ef.Posts.Add(post1);
    ef.PostTypes.Add(pt);
    ef.SaveChanges();
}

//ef.MetaFields.RemoveRange(ef.MetaFields);
//ef.MetaValues.RemoveRange(ef.MetaValues);
//ef.SaveChanges();

if (ef.MetaFields.Count() == 0)
{
    var mf = new MetaFieldEntity()
    {
        Title = "mf title",
        Type = EMetaFieldType.String,
        Key = "string1",
    };
    ef.MetaFields.Add(mf);
    ef.SaveChanges();

    var mv = new MetaValueEntity()
    {
        Type = EMetaFieldType.String,
        StringShort = "value1",
        MetaFieldId = mf.Id,
    };

    ef.MetaValues.Add(mv);
    ef.SaveChanges();

    var pt = ef.PostTypes.First();
    var post1 = ef.Posts.First();
    ef.PostTypeMetaFields.Add(new PostTypeMetaFieldEntity { MetaFieldId = mf.Id, PostTypeId = pt.Id });
    ef.PostMetaValues.Add(new PostMetaValueEntity { MetaValueId = mv.Id, PostId = post1.Id });

    ef.SaveChanges();
}


ef.ChangeTracker.Clear();

//var files = ef.Files.ToList();
//var userFiles = ef.Users.OrderBy(x => x.UserName).Include(x => x.Files).First();
//var ufr = ef.UserFilesEntities.ToList();

var posts = ef.Posts
                //.Include(x=>x.MetaValues)
                //    .ThenInclude(x=>x.MetaField)
                .Include(x => x.PostType)
                    .ThenInclude(x => x.MetaFields)
                .Include(x => x.MetaValues)
                .First();

//options = ef.Options.ToList();

Console.WriteLine(options.Count);
