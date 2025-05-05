using AutoFixture;

namespace Mars.Test.Common.FixtureCustomizes;

public sealed class FixtureCustomize : ICustomization
{
    public static DateTimeOffset DefaultCreated = DateTimeOffset.Now;

    public static readonly string[] TopTags = ["top", "popular", "news", "post", "tag1", "tag2", "category1", "category2"];

    public void Customize(IFixture fixture)
    {
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        fixture.Customizations.Add(new MailAddressGenerator());

        fixture.Customize(new EntitiesCustomize());
        fixture.Customize(new RequestCustomize());
        fixture.Customize(new MetaFieldRequestCustomize());
    }

    public static Func<T> Chance<T>(T[] variants)
    {
        return () => Random.Shared.GetItems(variants, 1)[0];
    }
}
