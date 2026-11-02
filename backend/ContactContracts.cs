using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Rubrica.Api;

public sealed record ApiError(string Error);

public sealed record Contact(
    int Id,
    [property: JsonPropertyName("first_name")] string FirstName,
    [property: JsonPropertyName("last_name")] string LastName,
    string Phone,
    string? Email,
    string? Notes,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt);

public sealed partial record ContactRequest(
    string? FirstName, string? LastName, string? Phone, string? Email, string? Notes)
{
    public ContactRequest Normalize() => new(
        FirstName?.Trim() ?? "", LastName?.Trim() ?? "", Phone?.Trim() ?? "",
        Email?.Trim() ?? "", Notes?.Trim() ?? "");

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName)
            || string.IsNullOrWhiteSpace(Phone))
            return "Nome, cognome e telefono sono obbligatori";
        if (FirstName.Length > 80 || LastName.Length > 80)
            return "Nome e cognome possono contenere al massimo 80 caratteri";
        if (Phone.Length > 30)
            return "Il telefono può contenere al massimo 30 caratteri";
        if (Email?.Length > 150)
            return "L'email può contenere al massimo 150 caratteri";
        if (!string.IsNullOrEmpty(Email) && !EmailPattern().IsMatch(Email))
            return "Formato email non valido";
        return null;
    }

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex EmailPattern();
}
