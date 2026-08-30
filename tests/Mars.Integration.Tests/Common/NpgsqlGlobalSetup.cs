using System.Runtime.CompilerServices;
using Npgsql;

namespace Mars.Integration.Tests.Common;

internal static class NpgsqlGlobalSetup
{
    // Npgsql читает EnableLegacyTimestampBehavior один раз при первом использовании в процессе.
    // Если тест вне фикстуры (например, PostgreSqlContainerTests) откроет соединение раньше,
    // чем DatabaseFixture.InitializeAsync выставит переключатель, запись DateTimeOffset
    // с локальным оффсетом в 'timestamp with time zone' начинает падать с ArgumentException.
    [ModuleInitializer]
    internal static void Init()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
#pragma warning disable CS0618 // Type or member is obsolete
        NpgsqlConnection.GlobalTypeMapper.UseJsonNet();
        NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
