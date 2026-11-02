namespace Rubrica.Api;

public static class ContactEndpoints
{
    public static void MapContactEndpoints(this WebApplication app)
    {
        var contacts = app.MapGroup("/contacts");
        contacts.MapGet("/", async (string? search, ContactRepository repository, CancellationToken cancellationToken) =>
        {
            var contatti = await repository.ListAsync(search?.Trim() ?? "", cancellationToken);
            return Results.Ok(contatti);
        });
            //Results.Ok(await repository.ListAsync(search?.Trim() ?? "", cancellationToken)));

        contacts.MapGet("/{id:int:min(1)}", async (int id, ContactRepository repository, CancellationToken cancellationToken) =>
            await repository.GetAsync(id, cancellationToken) is { } contact
                ? Results.Ok(contact)
                : Results.NotFound(new ApiError("Contatto non trovato")));

        contacts.MapPost("/", async (ContactRequest request, ContactRepository repository, CancellationToken cancellationToken) =>
        {
            var normalized = request.Normalize();
            if (normalized.Validate() is { } error) return Results.BadRequest(new ApiError(error));
            var contact = await repository.SaveAsync(normalized, null, cancellationToken)
                ?? throw new InvalidOperationException("Inserimento senza risultato");
            return Results.Created($"/contacts/{contact.Id}", contact);
        });

        contacts.MapPut("/{id:int:min(1)}", async (int id, ContactRequest request, ContactRepository repository, CancellationToken cancellationToken) =>
        {
            var normalized = request.Normalize();
            if (normalized.Validate() is { } error) return Results.BadRequest(new ApiError(error));
            return await repository.SaveAsync(normalized, id, cancellationToken) is { } contact
                ? Results.Ok(contact)
                : Results.NotFound(new ApiError("Contatto non trovato"));
        });

        contacts.MapDelete("/{id:int:min(1)}", async (int id, ContactRepository repository, CancellationToken cancellationToken) =>
            await repository.DeleteAsync(id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound(new ApiError("Contatto non trovato")));
    }
}
