using System.Linq.Expressions;
using AutoFixture;
using FluentAssertions;
using Mars.Host.Data.Entities;
using Mars.Host.Data.OwnedTypes.MetaFields;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.QueryLang.Services;
using Mars.Host.Shared.Repositories;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.MetaModelGenerator;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Test.Mars.MetaModelGenerator.Fixtures;

namespace Test.Mars.MetaModelGenerator;

public class ModelEfRequestManualTests : MetaModelGeneratorTests
{
    private readonly IQueryLangLinqDatabaseQueryHandler _handler;

    public ModelEfRequestManualTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _handler = appFixture.ServiceProvider.GetRequiredService<IQueryLangLinqDatabaseQueryHandler>();
    }

    #region SETUP
    async Task<(PostTypeDetail postTypeDetail, PostDetail[] posts)> SetupPostType(string postTypeName = "mytype")
    {
        var pts = AppFixture.ServiceProvider.GetRequiredService<IPostTypeRepository>();
        var ps = AppFixture.ServiceProvider.GetRequiredService<IPostRepository>();

        var postType = _fixture.Create<PostTypeEntity>();
        postType.TypeName = postTypeName;
        postType.MetaFields = [
                new(){ Key = "str1", Title = "Str 1", Type = EMetaFieldType.String },
                new(){ Key = "select1", Title = "Select 1", Type = EMetaFieldType.Select, Variants = [
                                                                                            new() { Id = Guid.NewGuid(), Title = "var1", Value = 1 },
                                                                                            new() { Id = Guid.NewGuid(), Title = "var2", Value = 2 },
                                                                                            new() { Id = Guid.NewGuid(), Title = "var3", Value = 3 },
                                                                                            ] },
                new(){ Key = "selectMany1", Title = "SelectMany 1", Type = EMetaFieldType.SelectMany, Variants = [
                                                                                            new() { Id = Guid.NewGuid(), Title = "var1", Value = 1 },
                                                                                            new() { Id = Guid.NewGuid(), Title = "var2", Value = 2 },
                                                                                            new() { Id = Guid.NewGuid(), Title = "var3", Value = 3 },
                                                                                            new() { Id = Guid.NewGuid(), Title = "var4", Value = 4 },
                                                                                            ] },
            ];

        using var ef = AppFixture.DbFixture.DbContext;
        ef.PostTypes.Add(postType);
        ef.SaveChanges();

        var mfdict = postType.MetaFields.ToDictionary(s => s.Key);

        var posts = _fixture.CreateMany<PostEntity>();
        var i = 1;
        foreach (var post in posts)
        {
            post.Title = $"Post {i}";
            post.PostTypeId = postType.Id;
            post.MetaValues = [
                new() { MetaFieldId = mfdict["str1"].Id, StringShort = $"v{i}" },
                new() { MetaFieldId = mfdict["select1"].Id, VariantId = mfdict["select1"].Variants.ElementAt(1).Id },
                new() { MetaFieldId = mfdict["selectMany1"].Id, VariantsIds = mfdict["selectMany1"].Variants.Skip(1).Take(2).Select(s=>s.Id).ToArray() },
            ];
            i++;
        }

        ef.Posts.AddRange(posts);
        ef.SaveChanges();

        var postTypeDetail = (await pts.GetDetailByName(postType.TypeName, default))!;

        //foreach (var post in posts) await ps.Create(post, default);

        var postList = await ps.ListAllDetail(new() { Type = postType.TypeName }, default);

        return (postTypeDetail, postList.ToArray());
    }
    #endregion

    [IntegrationFact]
    public async Task TestEfQuery()
    {
        var (postTypeDetail, posts) = await SetupPostType();
        var mf = postTypeDetail.MetaFields.First();

        using var ef = AppFixture.DbFixture.DbContext;

        var post = ef.Posts.Include(s => s.PostType)
                        .Include(s => s.MetaValues!)
                            .ThenInclude(s => s.MetaField)
                        .Where(s => s.PostType.TypeName == postTypeDetail.TypeName)
                        .Select(MyPostTypeEntity.selectExpression).FirstOrDefault(post => post.str1 == "v2");// second post

        post.str1.Should().NotBeNull();
        var selectVariant2 = postTypeDetail.MetaFields.First(f => f.Key == "select1").Variants!.ElementAt(1);
        post.select1.Title.Should().Be(selectVariant2.Title);
    }

    class MyPostTypeEntity : PostEntity
    {
        public string str1 { get; set; } = default!;

        public MetaFieldVariant? select1 { get; set; }
        public MetaFieldVariant[]? selectMany1 { get; set; }

        /// <summary>
        /// <see cref="MtFieldInfo.SelectExpressionRow"/>
        /// </summary>
        public static readonly Expression<Func<PostEntity, MyPostTypeEntity>> selectExpression = post => new MyPostTypeEntity()
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            //str1 = post.MetaValues.FirstOrDefault(s => s.MetaField.Key == nameof(MyPostTypeEntity.str1) && s.MetaField.ParentId == Guid.Empty)
            str1 = post.MetaValues!
                .Where(f => f.MetaField.Key == "str1" && f.MetaField.ParentId == Guid.Empty)
                .Select(f => f.StringShort)
                .FirstOrDefault()!,

            select1 = post.MetaValues!
                .Where(f => f.MetaField.Key == "select1" && f.MetaField.ParentId == Guid.Empty)
                .Select(f => f.MetaField.Variants.FirstOrDefault(v => v.Id == f.VariantId))
                .FirstOrDefault()!,

            selectMany1 = post.MetaValues!
                .Where(f => f.MetaField.Key == "selectMany1" && f.MetaField.ParentId == Guid.Empty)
                .SelectMany(f => f.MetaField.Variants.Where(v => f.VariantsIds.Contains(v.Id)))
                .ToArray(),
        };
    }
}
