using AutoFixture;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Shared.Contracts.MetaFields;

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
                                            ParentId = Guid.Empty,
                                            Key = fixture.Create<string>("key-"),
                                            Title = fixture.Create<string>("Title"),
                                            Disabled = false,
                                            Hidden = false,
                                            Description = "Description",
                                            IsNullable = false,
                                            MaxValue = null,
                                            MinValue = null,
                                            ModelName = null,
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
