using AutoFixture;

namespace Mars.Test.Common.FixtureCustomizes;

public sealed class FixtureCustomize : ICustomization
{
    public static DateTimeOffset DefaultCreated = DateTimeOffset.Now;

    public static readonly string[] TopTags = ["top", "popular", "news", "post", "tag1", "tag2", "category1", "category2"];

    private readonly TestEntityRefs _refs;

    public FixtureCustomize(TestEntityRefs? refs = null) => _refs = refs ?? TestEntityRefs.CreateDefault();

    public void Customize(IFixture fixture)
    {
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        fixture.Customizations.Add(new MailAddressGenerator());

        fixture.Customize(new EntitiesCustomize(_refs));
        fixture.Customize(new RequestCustomize());
        fixture.Customize(new MetaFieldRequestCustomize());
        fixture.Customize(new MetaFieldDtoCustomize());
    }

    public static Func<T> Chance<T>(T[] variants)
    {
        return () => Random.Shared.GetItems(variants, 1)[0];
    }
}
