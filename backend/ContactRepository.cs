using Npgsql;

namespace Rubrica.Api;

public sealed class ContactRepository(NpgsqlDataSource dataSource)
{
    private const string Columns = "id, first_name, last_name, phone, email, notes, created_at";

    public async Task<List<Contact>> ListAsync(string search, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand($"""
            SELECT {Columns} FROM contacts
            WHERE $1 = '' OR first_name ILIKE $2 OR last_name ILIKE $2
                OR phone ILIKE $2 OR COALESCE(email, '') ILIKE $2
            ORDER BY last_name, first_name, id
            """);
        command.Parameters.AddWithValue(search);
        command.Parameters.AddWithValue($"%{search}%");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var contacts = new List<Contact>();
        while (await reader.ReadAsync(cancellationToken)) contacts.Add(ReadContact(reader));
        return contacts;
    }

    public async Task<Contact?> GetAsync(int id, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand($"SELECT {Columns} FROM contacts WHERE id = $1");
        command.Parameters.AddWithValue(id);
        return await ReadOneAsync(command, cancellationToken);
    }

    public async Task<Contact?> SaveAsync(ContactRequest contact, int? id, CancellationToken cancellationToken)
    {
        var sql = id.HasValue
            ? $"""
                UPDATE contacts SET first_name = $1, last_name = $2, phone = $3,
                    email = NULLIF($4, ''), notes = NULLIF($5, '')
                WHERE id = $6 RETURNING {Columns}
                """
            : $"""
                INSERT INTO contacts (first_name, last_name, phone, email, notes)
                VALUES ($1, $2, $3, NULLIF($4, ''), NULLIF($5, '')) RETURNING {Columns}
                """;
        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue(contact.FirstName!);
        command.Parameters.AddWithValue(contact.LastName!);
        command.Parameters.AddWithValue(contact.Phone!);
        command.Parameters.AddWithValue(contact.Email ?? "");
        command.Parameters.AddWithValue(contact.Notes ?? "");
        if (id.HasValue) command.Parameters.AddWithValue(id.Value);
        return await ReadOneAsync(command, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("DELETE FROM contacts WHERE id = $1");
        command.Parameters.AddWithValue(id);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static async Task<Contact?> ReadOneAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadContact(reader) : null;
    }

    private static Contact ReadContact(NpgsqlDataReader reader) => new(
        reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
        reader.IsDBNull(4) ? null : reader.GetString(4),
        reader.IsDBNull(5) ? null : reader.GetString(5), reader.GetDateTime(6));
}
