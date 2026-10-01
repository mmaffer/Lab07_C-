using Biblioteca.Datos;
using Biblioteca.Negocio.Interfaces;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

// Reglas de préstamos y devoluciones. Los valores de las reglas viven aquí, no en Datos ni en WPF.
public class PrestamoNegocio
{
    public const int MaxLibrosPendientes = 3;
    public const int DiasDePrestamo = 7;
    public const decimal MultaPorDia = 1.50m;   // S/ por día de retraso

    public const string EstadoPendiente = "Pendiente";
    public const string EstadoDevuelto = "Devuelto";

    private readonly IPrestamoRepositorio _prestamos;
    private readonly IDetallePrestamoRepositorio _detalles;
    private readonly ISocioRepositorio _socios;
    private readonly ILibroRepositorio _libros;

    public PrestamoNegocio()
        : this(new PrestamoRepositorio(), new DetallePrestamoRepositorio(), new SocioRepositorio(), new LibroRepositorio()) { }

    public PrestamoNegocio(IPrestamoRepositorio prestamos, IDetallePrestamoRepositorio detalles,
                           ISocioRepositorio socios, ILibroRepositorio libros)
    {
        _prestamos = prestamos;
        _detalles = detalles;
        _socios = socios;
        _libros = libros;
    }

    // Multa = días de retraso x S/ 1.50. Sin retraso no hay multa.
    public static int CalcularDiasRetraso(DateTime fechaLimite, DateTime fechaDevolucion) =>
        Math.Max(0, (fechaDevolucion.Date - fechaLimite.Date).Days);

    public static decimal CalcularMulta(DateTime fechaLimite, DateTime fechaDevolucion) =>
        CalcularDiasRetraso(fechaLimite, fechaDevolucion) * MultaPorDia;

    // Registra el préstamo con uno o varios libros. Devuelve el número de préstamo.
    public async Task<int> RegistrarAsync(int socioId, List<int> libroIds)
    {
        if (socioId <= 0)
            throw new ReglaNegocioException("Debe seleccionar un socio.");

        if (libroIds == null || libroIds.Count == 0)
            throw new ReglaNegocioException("Debe agregar al menos un libro.");

        if (libroIds.Distinct().Count() != libroIds.Count)
            throw new ReglaNegocioException("No puede repetir el mismo libro en un préstamo.");

        var socio = await _socios.ObtenerAsync(socioId);
        if (socio == null || !socio.Activo)
            throw new ReglaNegocioException("El socio no existe o está dado de baja.");

        int pendientes = await _detalles.ContarPendientesPorSocioAsync(socioId);
        if (pendientes + libroIds.Count > MaxLibrosPendientes)
            throw new ReglaNegocioException(
                $"Un socio no puede tener más de {MaxLibrosPendientes} libros pendientes. " +
                $"{socio.Nombre} tiene {pendientes} y está pidiendo {libroIds.Count}.");

        var prestamo = new Prestamo
        {
            SocioId = socioId,
            FechaPrestamo = DateTime.Today,
            FechaLimite = DateTime.Today.AddDays(DiasDePrestamo),
            Estado = EstadoPendiente
        };

        foreach (int libroId in libroIds)
        {
            var libro = await _libros.ObtenerAsync(libroId);
            if (libro == null || !libro.Activo)
                throw new ReglaNegocioException("Uno de los libros no existe o está dado de baja.");

            if (libro.Ejemplares <= 0)
                throw new ReglaNegocioException($"El libro '{libro.Titulo}' no tiene ejemplares disponibles.");

            prestamo.Detalles.Add(new DetallePrestamo { LibroId = libroId });
        }

        // Una sola transacción en Datos: cabecera + detalles + descuento de stock.
        return await _prestamos.RegistrarAsync(prestamo);
    }

    public Task<List<PrestamoPendiente>> ListarPendientesAsync(int socioId) =>
        _detalles.ListarPendientesPorSocioAsync(socioId);

    // Registra la devolución de un libro y calcula la multa. La fecha de devolución es hoy.
    public async Task<ResultadoDevolucion> RegistrarDevolucionAsync(int prestamoId, int libroId)
    {
        var prestamo = await _prestamos.ObtenerAsync(prestamoId);
        if (prestamo == null)
            throw new ReglaNegocioException("El préstamo no existe.");

        var detalle = await _detalles.ObtenerAsync(prestamoId, libroId);
        if (detalle == null)
            throw new ReglaNegocioException("Ese libro no pertenece al préstamo.");

        if (detalle.FechaDevolucion != null)
            throw new ReglaNegocioException("Ese libro ya fue devuelto.");

        DateTime hoy = DateTime.Today;
        int pendientes = await _detalles.ContarPendientesPorPrestamoAsync(prestamoId);
        bool cerrar = pendientes == 1;   // este es el último libro pendiente

        await _prestamos.RegistrarDevolucionAsync(prestamoId, libroId, hoy, cerrar);

        return new ResultadoDevolucion
        {
            FechaDevolucion = hoy,
            DiasRetraso = CalcularDiasRetraso(prestamo.FechaLimite, hoy),
            Multa = CalcularMulta(prestamo.FechaLimite, hoy),
            PrestamoCerrado = cerrar
        };
    }

    public Task<List<ReportePrestamo>> ReporteAsync(DateTime desde, DateTime hasta)
    {
        if (desde.Date > hasta.Date)
            throw new ReglaNegocioException("La fecha 'desde' no puede ser mayor que la fecha 'hasta'.");

        return _prestamos.ReportePorFechasAsync(desde, hasta);
    }
}
