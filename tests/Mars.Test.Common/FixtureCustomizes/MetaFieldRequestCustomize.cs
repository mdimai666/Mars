using AutoFixture;
using Mars.Host.Data.Entities;
using Mars.Shared.Contracts.MetaFields;

namespace Mars.Test.Common.FixtureCustomizes;

public sealed class MetaFieldRequestCustomize : ICustomization
{
    public void Customize(IFixture fixture)
    {

        fixture.Customize<CreateMetaFieldRequest>(composer => composer
                                    //.OmitAutoProperties()
                                    .With(s => s.Id)
                                    .With(s => s.ParentId, Guid.Empty)
                                    .With(s => s.Variants)
                                    );

        fixture.Customize<CreateMetaValueRequest>(composer => composer
                                    //.OmitAutoProperties()
                                    .With(s => s.Id)
                                    .With(s => s.ParentId, Guid.Empty)
                                    .With(s => s.VariantsIds, [])
                                    );

    }
}

public static class MetaValueFixtureCustomizeExtension
{
    public static CreateMetaValueRequest CreateSimpleCreateMetaValueRequest(this IFixture fixture, Guid metaFieldId, EMetaFieldType type)
    {
        return fixture.Build<CreateMetaValueRequest>()
                                    .OmitAutoProperties()
                                    .With(s => s.Id)
                                    .With(s => s.ParentId, Guid.Empty)
                                    .With(s => s.MetaFieldId, metaFieldId)
                                    .With(s => s.VariantsIds, [])
                                    .Create()
                                    .SetMetaValue(fixture, metaFieldId, type);
    }

    public static UpdateMetaValueRequest UpdateSimpleCreateMetaValueRequest(this IFixture fixture, Guid metaValueId, Guid metaFieldId, EMetaFieldType type)
    {
        return fixture.Build<UpdateMetaValueRequest>()
                                    .OmitAutoProperties()
                                    .With(s => s.Id, metaValueId)
                                    .With(s => s.ParentId, Guid.Empty)
                                    .With(s => s.MetaFieldId, metaFieldId)
                                    .With(s => s.VariantsIds, [])
                                    .Create()
                                    .SetMetaValue(fixture, metaFieldId, type);
    }

    public static MetaValueEntity MetaValueEntity(this IFixture fixture, Guid metaFieldId, EMetaFieldType type)
    {
        return fixture.Build<MetaValueEntity>()
                                    .OmitAutoProperties()
                                    .With(s => s.Id)
                                    .With(s => s.ParentId, Guid.Empty)
                                    .With(s => s.MetaFieldId, metaFieldId)
                                    .With(s => s.Type, type)
                                    .Create()
                                    .SetMetaValue(fixture, metaFieldId, type);
    }

    public static MetaValueEntity SetMetaValue(this MetaValueEntity mv, IFixture _fixture, Guid metaFieldId, EMetaFieldType type)
    {
        if (type == EMetaFieldType.Int) mv.Int = _fixture.Create<int>();
        else if (type == EMetaFieldType.Bool) mv.Bool = _fixture.Create<bool>();
        else if (type == EMetaFieldType.Float) mv.Float = _fixture.Create<float>();
        else if (type == EMetaFieldType.Decimal) mv.Decimal = _fixture.Create<decimal>();
        else if (type == EMetaFieldType.Long) mv.Long = _fixture.Create<long>();
        else if (type == EMetaFieldType.String) mv.StringShort = _fixture.Create<string>();
        else if (type == EMetaFieldType.Text) mv.StringText = _fixture.Create<string>();
        else if (type == EMetaFieldType.DateTime) mv.DateTime = _fixture.Create<DateTime>();
        else throw new NotImplementedException();
        return mv;
    }

    public static CreateMetaValueRequest SetMetaValue(this CreateMetaValueRequest mv, IFixture _fixture, Guid metaFieldId, EMetaFieldType type)
    {
        if (type == EMetaFieldType.Int) mv = mv with { Int = _fixture.Create<int>() };
        else if (type == EMetaFieldType.Bool) mv = mv with { Bool = _fixture.Create<bool>() };
        else if (type == EMetaFieldType.Float) mv = mv with { Float = _fixture.Create<float>() };
        else if (type == EMetaFieldType.Decimal) mv = mv with { Decimal = _fixture.Create<decimal>() };
        else if (type == EMetaFieldType.Long) mv = mv with { Long = _fixture.Create<long>() };
        else if (type == EMetaFieldType.String) mv = mv with { StringShort = _fixture.Create<string>() };
        else if (type == EMetaFieldType.Text) mv = mv with { StringText = _fixture.Create<string>() };
        else if (type == EMetaFieldType.DateTime) mv = mv with { DateTime = _fixture.Create<DateTime>() };
        else throw new NotImplementedException();
        return mv;
    }

    public static UpdateMetaValueRequest SetMetaValue(this UpdateMetaValueRequest mv, IFixture _fixture, Guid metaFieldId, EMetaFieldType type)
    {
        if (type == EMetaFieldType.Int) mv = mv with { Int = _fixture.Create<int>() };
        else if (type == EMetaFieldType.Bool) mv = mv with { Bool = _fixture.Create<bool>() };
        else if (type == EMetaFieldType.Float) mv = mv with { Float = _fixture.Create<float>() };
        else if (type == EMetaFieldType.Decimal) mv = mv with { Decimal = _fixture.Create<decimal>() };
        else if (type == EMetaFieldType.Long) mv = mv with { Long = _fixture.Create<long>() };
        else if (type == EMetaFieldType.String) mv = mv with { StringShort = _fixture.Create<string>() };
        else if (type == EMetaFieldType.Text) mv = mv with { StringText = _fixture.Create<string>() };
        else if (type == EMetaFieldType.DateTime) mv = mv with { DateTime = _fixture.Create<DateTime>() };
        else throw new NotImplementedException();
        return mv;
    }
}
