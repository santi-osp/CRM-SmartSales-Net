using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartSales.Sales.Infrastructure.ContactsDirectory;

public sealed class ContactsDirectoryClient(HttpClient client)
{
    public Task<DirectoryContact?> GetContactAsync(Guid id, string bearer, CancellationToken cancellationToken) =>
        GetAsync<DirectoryContact>($"/api/contacts/v1/references/contacts/{id}", bearer, cancellationToken);

    public Task<DirectoryUser?> GetUserAsync(Guid id, string bearer, CancellationToken cancellationToken) =>
        GetAsync<DirectoryUser>($"/api/contacts/v1/references/users/{id}", bearer, cancellationToken);

    private async Task<T?> GetAsync<T>(string path, string bearer, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        if (Activity.Current?.Id is { } traceparent)
            request.Headers.TryAddWithoutValidation("traceparent", traceparent);
        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return default;
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new DirectoryFailureException((int)response.StatusCode, "directory_authorization_failed",
                    "No se pudo autorizar la referencia.");
            if (!response.IsSuccessStatusCode)
                throw Unavailable();
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        }
        catch (HttpRequestException) { throw Unavailable(); }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { throw Unavailable(); }
        catch (JsonException) { throw Unavailable(); }
    }

    private static DirectoryFailureException Unavailable() =>
        new(503, "directory_unavailable", "El directorio de contactos no está disponible.");
}

public sealed record DirectoryContact(Guid Id, string Name, string Type);
public sealed record DirectoryUser(Guid Id, string DisplayName, string Role, bool IsActive);
public sealed class DirectoryFailureException(int statusCode, string code, string title) : Exception(title)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
}
