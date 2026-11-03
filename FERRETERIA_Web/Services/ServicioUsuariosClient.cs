using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FERRETERIA_Web.Models;

namespace FERRETERIA_Web.Services;

public sealed class ServicioUsuariosClient(HttpClient httpClient) : IServicioUsuariosClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<(bool Exitoso, string Token, string Error)> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/auth/login", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return (false, string.Empty, await LeerErrorAsync(response, cancellationToken));

        LoginResponse? resultado = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions, cancellationToken);
        return resultado is null
            ? (false, string.Empty, "No fue posible iniciar sesión.")
            : (true, resultado.Token, string.Empty);
    }

    public async Task<IReadOnlyList<UsuarioResponse>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<List<UsuarioResponse>>("/api/usuarios", JsonOptions, cancellationToken)
            ?? [];
    }

    public async Task<UsuarioResponse?> ObtenerAsync(long id, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await httpClient.GetAsync($"/api/usuarios/{id}", cancellationToken);
        return response.StatusCode == HttpStatusCode.NotFound
            ? null
            : await response.Content.ReadFromJsonAsync<UsuarioResponse>(JsonOptions, cancellationToken);
    }

    public async Task<(bool Exitoso, UsuarioResponse? Usuario, string Error)> CrearAsync(CrearUsuarioRequest request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/usuarios", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return (false, null, await LeerErrorAsync(response, cancellationToken));
        UsuarioResponse? usuario = await response.Content.ReadFromJsonAsync<UsuarioResponse>(JsonOptions, cancellationToken);
        return (usuario is not null, usuario, usuario is null ? "No fue posible crear el usuario." : string.Empty);
    }

    public async Task<(bool Exitoso, string Error)> EditarAsync(long id, EditarUsuarioRequest request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await httpClient.PutAsJsonAsync($"/api/usuarios/{id}", request, JsonOptions, cancellationToken);
        return response.IsSuccessStatusCode
            ? (true, string.Empty)
            : (false, await LeerErrorAsync(response, cancellationToken));
    }

    public async Task<(bool Exitoso, string Error)> DarDeBajaAsync(long id, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await httpClient.DeleteAsync($"/api/usuarios/{id}", cancellationToken);
        return response.IsSuccessStatusCode
            ? (true, string.Empty)
            : (false, await LeerErrorAsync(response, cancellationToken));
    }

    public async Task<(bool Exitoso, string Error)> ActivarAsync(string token, ActivarCuentaRequest request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            $"/api/auth/activate?token={Uri.EscapeDataString(token)}",
            request,
            JsonOptions,
            cancellationToken);
        return response.IsSuccessStatusCode
            ? (true, string.Empty)
            : (false, await LeerErrorAsync(response, cancellationToken));
    }

    public async Task<(bool Exitoso, string Error)> CambiarPasswordAsync(CambiarPasswordRequest request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/usuarios/cambiar-password", request, JsonOptions, cancellationToken);
        return response.IsSuccessStatusCode
            ? (true, string.Empty)
            : (false, await LeerErrorAsync(response, cancellationToken));
    }

    private static async Task<string> LeerErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            JsonDocument documento = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (documento.RootElement.TryGetProperty("mensaje", out JsonElement mensaje))
                return mensaje.GetString() ?? "Ocurrió un error al procesar la solicitud.";
        }
        catch (JsonException)
        {
        }

        return response.StatusCode == HttpStatusCode.Unauthorized
            ? "Credenciales inválidas."
            : "Ocurrió un error al procesar la solicitud.";
    }
}
