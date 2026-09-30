using Biblioteca.Entidades;

namespace Biblioteca.Datos.Interfaces;

public interface IDetallePrestamoRepositorio
{
    Task<DetallePrestamo> ObtenerAsync(int prestamoId, int libroId);
    Task<int> ContarPendientesPorSocioAsync(int socioId);
    Task<int> ContarPendientesPorPrestamoAsync(int prestamoId);
    Task<int> ContarPendientesPorLibroAsync(int libroId);
    Task<List<PrestamoPendiente>> ListarPendientesPorSocioAsync(int socioId);
}
