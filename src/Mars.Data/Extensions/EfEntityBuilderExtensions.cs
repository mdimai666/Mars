using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mars.Host.Data.Extensions;

public static class EfEntityBuilderExtensions
{
    public static PropertyBuilder<TProperty> IgnorePropertyFromUpdate<TProperty>([NotNull] this PropertyBuilder<TProperty> propertyBuilder)
    {

        var meta = propertyBuilder.Metadata;

        meta.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        meta.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

        return propertyBuilder;
    }
}
