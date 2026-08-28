using AutoFixture;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Contracts.MetaFields;

namespace Mars.Test.Common.FixtureCustomizes;

public sealed class MetaFieldDtoCustomize : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<MetaFieldDto>(composer => composer
                                    .FromFactory(() =>
                                    {
                                        return new MetaFieldDto()
                                        {
                                            Id = Guid.NewGuid(),
                                            Key = fixture.Create<string>("key-"),
                                            Title = fixture.Create<string>("Title"),
                                            Disabled = false,
                                            Hidden = false,
                                            Description = "Description",
                                            IsNullable = false,
                                            IsMultiple = false,
                                            MaxValue = null,
                                            MinValue = null,
                                            ModelName = null,
                                            Default = null,
                                            Options = null,
                                            Order = 0,
                                            Tags = [],
                                            Type = MetaFieldType.Bool,
                                            Variants = null,
                                        };
                                    })
                                    .OmitAutoProperties()
                                    );
    }
}
