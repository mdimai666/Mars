using AppShared.Models;
using Mars.Host.Data;
using Mars.Host.QueryLang;
using Mars.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Test.Mars.Host.Models;
using Test.Mars.Host.TestHostApp;

namespace Test.Mars.Host.QueryLang;

public class TestQueryLangGetModelAsObject : UnitTestHostBaseClass
{
    [Fact]
    public void GenerateRawSqlForMetaObject()
    {
        var pctx = _pctx();
        var ef = pctx.ef;

        string postTypeName = "testPost";

        PostType postType = ef.PostTypes
                                .Include(s => s.MetaFields)
                                .First(s => s.TypeName == postTypeName);

        List<string> fields = new();

        foreach (var t in postType.MetaFields)
        {
            string key = t.Key;

            string typeFieldColumn = MetaValue.GetColName(t.Type);

            string f = @$"(
					SELECT ""MetaValues"".""{typeFieldColumn}""
					FROM ""PostMetaValues"" 
 					INNER JOIN ""MetaValues"" on ""MetaValues"".""Id"" = ""PostMetaValues"".""MetaValueId"" 
 					INNER JOIN ""MetaFields"" ON ""MetaFields"".""Id"" = ""MetaValues"".""MetaFieldId""	
					WHERE ""PostMetaValues"".""PostId"" = ""r"".""Id"" AND ""MetaFields"".""Key"" = '{key}'
					LIMIT 1
				) as {key}";
            fields.Add(f);
        }

        string extraFields = (fields.Count > 0 ? ',' : ' ') + fields.JoinStr(", \n");

        string preparedQuery = $@"
				SELECT ""r"".*
					{extraFields}
				FROM
				(
					-- SELECT ""posts"".""Id"",""posts"".""Title""--,""Int"" , ""StringShort""
                    SELECT *
					FROM ""posts""
					-- JOIN ""PostMetaValues"" on ""PostMetaValues"".""PostId"" = ""posts"".""Id"" 
					-- INNER JOIN ""MetaValues"" on ""MetaValues"".""Id"" = ""PostMetaValues"".""MetaValueId""
					-- -- INNER JOIN ""PostTypeMetaFields"" on ""PostTypeMetaFields"".""MetaFieldId"" = ""MetaValues"".""MetaFieldId""
					-- INNER JOIN ""MetaFields"" on ""MetaFields"".""Id"" = ""MetaValues"".""MetaFieldId""
					WHERE ""posts"".""Type"" = '{postTypeName}' -- AND ""MetaFields"".""Key"" = 'int1' --AND ""MetaValues"".""Int"">=11
				) as r
				-- GROUP BY ""r"".""Id"",""r"".""Title"" --,""Int"" , ""StringShort""
				-- LIMIT 1";

#pragma warning disable CS0162 // Unreachable code detected
        if (false)
        {
            preparedQuery = $@"
			SELECT *
			FROM ({preparedQuery}) as dtos
			WHERE <condition>;
			";
        }
#pragma warning restore CS0162 // Unreachable code detected


        ////      var d = ef.Set<PostDto>().FromSqlRaw();

        ////ef.Database.sql

        //IConfiguration cfg = _serviceProvider.GetRequiredService<IConfiguration>();


        //string connectionString = TestMarsDbContext.GetConnectionString();

        //      await using var conn = new NpgsqlConnection(connectionString);
        //      await conn.OpenAsync();

        //      await using var cmd = new NpgsqlCommand(sql2, conn);
        //      await using var reader = await cmd.ExecuteReaderAsync();

        //      var cols = await reader.GetColumnSchemaAsync();

        //      Dictionary<string, QTableColumn> dict = new();

        //      //foreach (var col in cols)
        //      //{
        //      //    dict.Add(col.ColumnName, QTableColumnExtensions.QTableColumn(col));
        //      //}

        //List<string> d = new List<string>() { "1" };

        //      Assert.True(d.Count() > 0);

        ////return Task.CompletedTask;
        ///
        var posts = ef.SqlQuery<testPost>(preparedQuery);

        Assert.True(posts.Any());


    }

    [Fact]
    public void NonEntityTypeMapping()
    {
        using var ef = _serviceProvider.GetService<MarsDbContextLegacy>();

        var posts = ef.SqlQuery<AnonPost>("SELECT * FROM posts LIMIT 10");

        Assert.True(posts.Any());


    }

    class AnonPost : BasicEntityNonUser
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
    }

    [Fact]
    public void AnotherWay()
    {
        using var ef = _serviceProvider.GetService<MarsDbContextLegacy>();

        var posts = ef.Posts
                        .Include(s => s.MetaValues)
                        .ThenInclude(s => s.MetaField)
                        //.Where(s => s.MetaValues.Any(x => x.MetaField.Key == metaField && x.Int == equalValue))
                        .Where(s => s.Type == nameof(testPost))
                        .Select(post => new testPost
                        {
                            Id = post.Id,
                            Title = post.Title,
                            Content = post.Content,
                            int1 = post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(testPost.int1)).Int,
                            str1 = post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(testPost.str1)).StringShort
                        })
                        .Where(s => s.int1 > 11)
                        .ToList();

        var sql = ef.Posts
                        .Include(s => s.MetaValues)
                        .ThenInclude(s => s.MetaField)
                        //.Where(s => s.MetaValues.Any(x => x.MetaField.Key == metaField && x.Int == equalValue))
                        .Where(s => s.Type == nameof(testPost))
                        .Select(post => new testPost
                        {
                            Id = post.Id,
                            Title = post.Title,
                            Content = post.Content,
                            int1 = post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(testPost.int1)).Int,
                            str1 = post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(testPost.str1)).StringShort
                        })
                        .Where(s => s.int1 > 11)
                        .ToQueryString();

        Assert.Contains(posts, s => s.int1 > 1);
        Assert.Contains(posts, s => s.str1 is not null);

        Assert.Equal(2, posts.Count);
    }

    [Fact]
    public void TestMtoSelectExpression()
    {
        using var ef = _serviceProvider.GetService<MarsDbContextLegacy>();

        Func<Post, testPost> exp = post => new testPost
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            int1 = post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(testPost.int1)).Int,
            str1 = post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(testPost.str1) && post.ParentId == Guid.Empty).StringShort

        };

        var posts = ef.Posts
                        .Where(s => s.Type == nameof(testPost))
                        .Include(s => s.MetaValues)
                        .ThenInclude(s => s.MetaField)
                        .Select(exp)
                        .Where(s => s.int1 > 11)
                        .ToList();

        Assert.Equal(2, posts.Count);

    }


    [Fact]
    public void TestMtoSelectExpression2()
    {
        using var ef = _serviceProvider.GetService<MarsDbContextLegacy>();

        //Func<Post, testPost2> exp = q => new testPost2
        //{
        //    Id = q.post.Id,
        //    Title = q.post.Title,
        //    Content = q.post.Content,
        //    int1 = q.post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(testPost.int1)).Int,
        //    //str1 = post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(testPost.str1) && post.ParentId == Guid.Empty).StringShort,
        //    //d = post.MetaValues.Select(s => new KeyValuePair<string, MetaValue>(s.MetaField.Key, s)).ToDictionary(s => s.Key, s => s.Value),
        //    //int1 = EF.Functions.()
        //    //str1 = post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(testPost.int1)).StringShort
        //    //str1 = post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(testPost.int1)).StringShort => ""
        //};

        //Func<Post, testPost2> exp = q => new testPost2
        //{
        //    Id = q.post.Id,
        //    Title = q.post.Title,
        //    Content = q.post.Content,
        //    int1 = q.post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(testPost.int1)).Int,
        //};

        //Func<testPost2, testPost2> exp2 = post => new testPost2
        //{
        //    Id = post.Id,
        //    Title = post.Title,
        //    Content = post.Content,
        //    int1 = post.d[nameof(testPost.int1)].Int
        //};

        //var posts = ef.Posts
        //                .Where(s => s.Type == nameof(testPost))
        //                .Include(s => s.MetaValues)
        //                .ThenInclude(s => s.MetaField)
        //                //.Select(s=>(new TQHelper<Post> { post = s }))
        //                .Select(exp)
        //                //.Select(exp2)
        //                .Where(s => s.int1 > 11)
        //                .ToList();

        //Assert.Equal(2, posts.Count);

    }

    //public class TQHelper<T>
    //{
    //    public T post { get; set; }
    //    public Dictionary<string, MetaValue> d { get; set; }


    //}
}
