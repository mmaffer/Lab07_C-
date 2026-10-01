using Biblioteca.Entidades;

namespace Biblioteca.Negocio.Interfaces;

public interface IPrestamoRepositorio
{
    Task<int> RegistrarAsync(Prestamo prestamo);
    Task RegistrarDevolucionAsync(int prestamoId, int libroId, DateTime fechaDevolucion, bool cerrarPrestamo);
    Task<Prestamo> ObtenerAsync(int prestamoId);
    Task<List<ReportePrestamo>> ReportePorFechasAsync(DateTime desde, DateTime hasta);
}
