using Npgsql;

namespace Rubrica.Api;

internal static class DatabaseConfiguration
{
    public static string GetConnectionString(IConfiguration configuration)
    {
        if (configuration.GetConnectionString("Contacts") is { Length: > 0 } connectionString)
        {
            return connectionString;
        }

        string Required(string key) => configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Configurazione obbligatoria mancante: {key}");

        return new NpgsqlConnectionStringBuilder
        {
            Host = Required("DB_HOST"),
            Port = configuration.GetValue("DB_PORT", 5432),
            Username = Required("DB_USER"),
            Password = Required("DB_PASSWORD"),
            Database = Required("DB_NAME"),
            Timeout = 5,
            CommandTimeout = 15
        }.ConnectionString;
    }
}
