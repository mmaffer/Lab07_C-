using System.Net.Mail;
using Biblioteca.Datos;
using Biblioteca.Datos.Interfaces;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

// Reglas de socios: valida antes de llamar a Datos.
public class SocioNegocio
{
    private const int LongitudDni = 8;

    private readonly ISocioRepositorio _socios;
    private readonly IDetallePrestamoRepositorio _detalles;

    public SocioNegocio() : this(new SocioRepositorio(), new DetallePrestamoRepositorio()) { }

    public SocioNegocio(ISocioRepositorio socios, IDetallePrestamoRepositorio detalles)
    {
        _socios = socios;
        _detalles = detalles;
    }

    public Task<List<Socio>> ListarAsync() => _socios.ListarAsync();

    // Busca por nombre o DNI.
    public Task<List<Socio>> BuscarAsync(string texto) =>
        string.IsNullOrWhiteSpace(texto) ? ListarAsync() : _socios.BuscarAsync(texto.Trim());

    public async Task InsertarAsync(Socio socio)
    {
        await ValidarAsync(socio);
        await _socios.InsertarAsync(socio);
    }

    public async Task ActualizarAsync(Socio socio)
    {
        if (socio.SocioId <= 0)
            throw new ReglaNegocioException("Debe seleccionar un socio para actualizar.");

        await ValidarAsync(socio);
        await _socios.ActualizarAsync(socio);
    }

    // Eliminación lógica (Activo = 0). No se da de baja un socio con préstamos pendientes.
    public async Task EliminarAsync(int socioId)
    {
        if (socioId <= 0)
            throw new ReglaNegocioException("Debe seleccionar un socio.");

        if (await _detalles.ContarPendientesPorSocioAsync(socioId) > 0)
            throw new ReglaNegocioException("No se puede dar de baja al socio: tiene libros pendientes de devolución.");

        await _socios.DesactivarAsync(socioId);
    }

    private async Task ValidarAsync(Socio socio)
    {
        socio.DNI = socio.DNI?.Trim();
        socio.Nombre = socio.Nombre?.Trim();
        socio.Email = string.IsNullOrWhiteSpace(socio.Email) ? null : socio.Email.Trim();

        if (string.IsNullOrWhiteSpace(socio.DNI))
            throw new ReglaNegocioException("El DNI es obligatorio.");

        if (socio.DNI.Length != LongitudDni || !socio.DNI.All(char.IsDigit))
            throw new ReglaNegocioException($"El DNI debe tener {LongitudDni} dígitos numéricos.");

        if (string.IsNullOrWhiteSpace(socio.Nombre))
            throw new ReglaNegocioException("El nombre es obligatorio.");

        if (socio.Email != null && !EsEmailValido(socio.Email))
            throw new ReglaNegocioException("El formato del email no es válido.");

        if (await _socios.ExisteDniAsync(socio.DNI, socio.SocioId))
            throw new ReglaNegocioException($"El DNI '{socio.DNI}' ya está registrado.");
    }

    private static bool EsEmailValido(string email) =>
        MailAddress.TryCreate(email, out var direccion) && direccion.Address == email;
}
