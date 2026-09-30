using Biblioteca.Entidades;

namespace Biblioteca.Datos.Interfaces;

public interface IPrestamoRepositorio
{
    // Cabecera + detalles + descuento de ejemplares en UNA sola transacción.
    Task<int> RegistrarAsync(Prestamo prestamo);

    // Fecha de devolución + ejemplar al stock (+ cerrar el préstamo si corresponde) en UNA transacción.
    Task RegistrarDevolucionAsync(int prestamoId, int libroId, DateTime fechaDevolucion, bool cerrarPrestamo);

    Task<Prestamo> ObtenerAsync(int prestamoId);
    Task<List<ReportePrestamo>> ReportePorFechasAsync(DateTime desde, DateTime hasta);
}
