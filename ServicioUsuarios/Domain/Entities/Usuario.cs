namespace ServicioUsuarios.Domain.Entities;

public sealed class Usuario
{
    public long Id { get; init; }
    public required string NombreCompleto { get; init; }
    public required string NombreUsuario { get; init; }
    public required string Correo { get; init; }
    public required string PasswordHash { get; init; }
    public required string Rol { get; init; }
    public bool Activo { get; init; }
    public bool DebeCambiarPassword { get; init; }
    public string? CreadoPor { get; init; }
    public DateTime FechaCreacion { get; init; }
    public string? ModificadoPor { get; init; }
    public DateTime? FechaModificacion { get; init; }
    public string? DadoDeBajaPor { get; init; }
    public DateTime? FechaBaja { get; init; }
}
