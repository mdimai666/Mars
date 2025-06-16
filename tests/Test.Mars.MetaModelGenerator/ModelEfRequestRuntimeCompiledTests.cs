using System.Reflection;
using AutoFixture;
using FluentAssertions;
using Mars.Host.Data.Common;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Data.OwnedTypes.MetaFields;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Repositories;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.MetaModelGenerator;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Test.Mars.MetaModelGenerator.Fixtures;

namespace Test.Mars.MetaModelGenerator;

public class ModelEfRequestRuntimeCompiledTests : MetaModelGeneratorTests
{
    private readonly RuntimeMetaTypeCompiler _runtimeMetaTypeCompiler;

    public ModelEfRequestRuntimeCompiledTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _runtimeMetaTypeCompiler = new RuntimeMetaTypeCompiler();
    }

    [IntegrationTheory]
    [InlineData([EMetaFieldType.Bool])]
    [InlineData([EMetaFieldType.Int])]
    [InlineData([EMetaFieldType.Long])]
    [InlineData([EMetaFieldType.Float])]
    [InlineData([EMetaFieldType.Decimal])]
    [InlineData([EMetaFieldType.DateTime])]
    [InlineData([EMetaFieldType.String])]
    [InlineData([EMetaFieldType.Text])]
    [InlineData([EMetaFieldType.Select])]
    [InlineData([EMetaFieldType.SelectMany])]
    public async Task EfMetaModelQuery_ListOfPosts_Success(EMetaFieldType type)
    {
        //Arrange
        var newClassName = "MyTypeMto";
        var typeName = "myType";
        var mfId = Guid.NewGuid();
        var mfKey = $"key_{type}".ToLower();

        var mf = new MetaFieldEntity() { Key = mfKey, Type = type, Id = mfId, Title = $"Title - {mfKey}" };
        MetaValueEntity mv;

        if (type == EMetaFieldType.Select)
        {
            mf.Variants = _fixture.CreateMany<MetaFieldVariant>(3).ToList();
            mv = new() { MetaFieldId = mfId, VariantId = mf.Variants.ElementAt(1).Id };
        }
        else if (type == EMetaFieldType.SelectMany)
        {
            mf.Variants = _fixture.CreateMany<MetaFieldVariant>(4).ToList();
            mv = new() { MetaFieldId = mfId, VariantsIds = mf.Variants.Skip(1).Take(2).Select(s => s.Id).ToArray() };
        }
        else
        {
            mv = _fixture.MetaValueEntity(mfId, type);
        }

        var (postType, _) = await SetupPostType2(typeName, [mf], [mv]);
        var mti = new MetaTypeInfo(newClassName, typeof(PostEntity), postType.MetaFields.ToArray(), new());
        var dict = await _runtimeMetaTypeCompiler.Compile([mti], null);
        var compiledType = dict[newClassName];

        using var ef = AppFixture.DbFixture.DbContext;

        var query = ef.Posts.Include(post => post.PostType)
                            .Include(post => post.MetaValues!)
                                .ThenInclude(mv => mv.MetaField)
                            .Where(post => post.PostType.TypeName == typeName);

        var posts = query.ToList();

        //Act
        var result = SelectQuery(query, compiledType);

        //Assert
        typeof(PostEntity).IsAssignableFrom(compiledType);
        result.Should().NotBeNull();
        foreach (var _mtPost in (result as IEnumerable<object>))
        {
            var mtPost = _mtPost as IBasicUserEntity;
            var post = posts.First(s => s.Id == mtPost.Id);
            var d = mtPost as dynamic;
            Assert.Equal(post.Title, d.Title);

            var mfValue = post.MetaValues!.First(f => f.MetaField.Key == mfKey).Get();
            //Assert.Equal(mfValue, d.str1);

            var mtFieldValue = mtPost.GetType().GetProperty(mfKey, BindingFlags.Instance | BindingFlags.Public).GetValue(mtPost);

            //Assert.Equalen(mfValue, mtFieldValue);
            //mfValue.Should().BeEquivalentTo(mtFieldValue);
            mtFieldValue.Should().BeEquivalentTo(mfValue);
        }
    }

    public class ScriptContext
    {
        public MarsDbContext ef = default!;
    }

    public object? SelectQuery(IQueryable<PostEntity> query, Type compiledType)
    {
        var selectExpression = compiledType.GetField(GenSourceCodeMaster.selectExpressionGetterName, BindingFlags.Static | BindingFlags.Public).GetValue(null)!;

        MethodInfo selectMethod = typeof(Queryable)
              .GetMethods(BindingFlags.Static | BindingFlags.Public)
              .First(mi => mi.Name == nameof(Queryable.Select)
                         && mi.IsGenericMethodDefinition
                         && mi.GetParameters().Length == 2
                && mi.GetParameters()[1].Name == "selector")
              .MakeGenericMethod(typeof(PostEntity), compiledType);

        MethodInfo toListMethod = typeof(Enumerable)
              .GetMethods(BindingFlags.Static | BindingFlags.Public)
              .First(mi => mi.Name == nameof(Enumerable.ToList)
                         && mi.IsGenericMethodDefinition
                         && mi.GetParameters().Length == 1
                && mi.GetParameters()[0].Name == "source")
              .MakeGenericMethod(compiledType);

        var result = selectMethod.Invoke(query, [query, selectExpression]);
        result = toListMethod.Invoke(result, [result]);

        return result;
    }

    #region SETUP

    async Task<(PostTypeEntity postType, PostDetail[] posts)> SetupPostType2(string postTypeName,
                                                                                    List<MetaFieldEntity> metaFields,
                                                                                    List<MetaValueEntity> metaValues)
    {
        //var pts = AppFixture.ServiceProvider.GetRequiredService<IPostTypeRepository>();
        var ps = AppFixture.ServiceProvider.GetRequiredService<IPostRepository>();

        var postType = _fixture.Create<PostTypeEntity>();
        postType.TypeName = postTypeName;
        postType.MetaFields = metaFields;

        using var ef = AppFixture.DbFixture.DbContext;
        ef.PostTypes.Add(postType);
        ef.SaveChanges();

        var mfdict = postType.MetaFields.ToDictionary(s => s.Key);

        var posts = _fixture.CreateMany<PostEntity>(1);
        var i = 1;
        foreach (var post in posts)
        {
            post.Title = $"Post {i}";
            post.PostTypeId = postType.Id;
            post.MetaValues = metaValues;
            i++;
        }

        ef.Posts.AddRange(posts);
        ef.SaveChanges();

        //var postTypeDetail = (await pts.GetDetailByName(postType.TypeName, default))!;
        //foreach (var post in posts) await ps.Create(post, default);

        var postList = await ps.ListAllDetail(new() { Type = postType.TypeName }, default);

        //return (postTypeDetail, postList.ToArray());
        return (postType, postList.ToArray());
    }
    #endregion
}
