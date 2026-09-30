using Biblioteca.Datos.Interfaces;
using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

// Lecturas sobre DetallePrestamo. Un libro "pendiente" tiene FechaDevolucion NULL.
public class DetallePrestamoRepositorio : IDetallePrestamoRepositorio
{
    public async Task<DetallePrestamo> ObtenerAsync(int prestamoId, int libroId)
    {
        const string sql = @"
            SELECT PrestamoId, LibroId, FechaDevolucion
            FROM DetallePrestamo
            WHERE PrestamoId = @PrestamoId AND LibroId = @LibroId";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@PrestamoId", prestamoId);
        cmd.Parameters.AddWithValue("@LibroId", libroId);
        await cn.OpenAsync();

        using var dr = await cmd.ExecuteReaderAsync();
        if (!await dr.ReadAsync()) return null;
        return new DetallePrestamo
        {
            PrestamoId = dr.GetInt32(0),
            LibroId = dr.GetInt32(1),
            FechaDevolucion = dr.IsDBNull(2) ? null : dr.GetDateTime(2)
        };
    }

    public Task<int> ContarPendientesPorSocioAsync(int socioId) =>
        ContarAsync(@"SELECT COUNT(1)
                      FROM DetallePrestamo d
                      INNER JOIN Prestamos p ON p.PrestamoId = d.PrestamoId
                      WHERE p.SocioId = @Id AND d.FechaDevolucion IS NULL", socioId);

    public Task<int> ContarPendientesPorPrestamoAsync(int prestamoId) =>
        ContarAsync("SELECT COUNT(1) FROM DetallePrestamo WHERE PrestamoId = @Id AND FechaDevolucion IS NULL", prestamoId);

    public Task<int> ContarPendientesPorLibroAsync(int libroId) =>
        ContarAsync("SELECT COUNT(1) FROM DetallePrestamo WHERE LibroId = @Id AND FechaDevolucion IS NULL", libroId);

    public async Task<List<PrestamoPendiente>> ListarPendientesPorSocioAsync(int socioId)
    {
        const string sql = @"
            SELECT p.PrestamoId, l.LibroId, l.Titulo, p.FechaPrestamo, p.FechaLimite
            FROM DetallePrestamo d
            INNER JOIN Prestamos p ON p.PrestamoId = d.PrestamoId
            INNER JOIN Libros l ON l.LibroId = d.LibroId
            WHERE p.SocioId = @SocioId AND d.FechaDevolucion IS NULL
            ORDER BY p.FechaLimite, l.Titulo";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@SocioId", socioId);
        await cn.OpenAsync();

        var lista = new List<PrestamoPendiente>();
        using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
        {
            lista.Add(new PrestamoPendiente
            {
                PrestamoId = dr.GetInt32(0),
                LibroId = dr.GetInt32(1),
                Titulo = dr.GetString(2),
                FechaPrestamo = dr.GetDateTime(3),
                FechaLimite = dr.GetDateTime(4)
            });
        }
        return lista;
    }

    private static async Task<int> ContarAsync(string sql, int id)
    {
        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cn.OpenAsync();
        return (int)await cmd.ExecuteScalarAsync();
    }
}
