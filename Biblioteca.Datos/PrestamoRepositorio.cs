using Biblioteca.Datos.Interfaces;
using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

// Capa de Datos: ejecuta las transacciones, pero NO decide reglas. Las reglas ya las validó Negocio.
public class PrestamoRepositorio : IPrestamoRepositorio
{
    // Cabecera + detalles + descuento de ejemplares: si algo falla, el rollback deja todo como estaba.
    public async Task<int> RegistrarAsync(Prestamo prestamo)
    {
        using var cn = DbConnectionFactory.Create();
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction();
        try
        {
            const string sqlCabecera = @"
                INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado)
                OUTPUT INSERTED.PrestamoId
                VALUES (@SocioId, @FechaPrestamo, @FechaLimite, @Estado)";

            int prestamoId;
            using (var cmd = new SqlCommand(sqlCabecera, cn, tx))
            {
                cmd.Parameters.AddWithValue("@SocioId", prestamo.SocioId);
                cmd.Parameters.AddWithValue("@FechaPrestamo", prestamo.FechaPrestamo);
                cmd.Parameters.AddWithValue("@FechaLimite", prestamo.FechaLimite);
                cmd.Parameters.AddWithValue("@Estado", prestamo.Estado);
                prestamoId = (int)await cmd.ExecuteScalarAsync();
            }

            foreach (var detalle in prestamo.Detalles)
            {
                const string sqlDetalle = @"
                    INSERT INTO DetallePrestamo (PrestamoId, LibroId) VALUES (@PrestamoId, @LibroId)";
                using (var cmd = new SqlCommand(sqlDetalle, cn, tx))
                {
                    cmd.Parameters.AddWithValue("@PrestamoId", prestamoId);
                    cmd.Parameters.AddWithValue("@LibroId", detalle.LibroId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Condición Ejemplares > 0 en el UPDATE: protege de que otro usuario se lleve el último ejemplar.
                const string sqlStock = @"
                    UPDATE Libros SET Ejemplares = Ejemplares - 1
                    WHERE LibroId = @LibroId AND Ejemplares > 0";
                using (var cmd = new SqlCommand(sqlStock, cn, tx))
                {
                    cmd.Parameters.AddWithValue("@LibroId", detalle.LibroId);
                    if (await cmd.ExecuteNonQueryAsync() == 0)
                        throw new InvalidOperationException(
                            $"El libro {detalle.LibroId} ya no tiene ejemplares disponibles. No se guardó el préstamo.");
                }
            }

            await tx.CommitAsync();
            return prestamoId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task RegistrarDevolucionAsync(int prestamoId, int libroId, DateTime fechaDevolucion, bool cerrarPrestamo)
    {
        using var cn = DbConnectionFactory.Create();
        await cn.OpenAsync();
        using var tx = cn.BeginTransaction();
        try
        {
            const string sqlDetalle = @"
                UPDATE DetallePrestamo SET FechaDevolucion = @Fecha
                WHERE PrestamoId = @PrestamoId AND LibroId = @LibroId AND FechaDevolucion IS NULL";
            using (var cmd = new SqlCommand(sqlDetalle, cn, tx))
            {
                cmd.Parameters.Add("@Fecha", System.Data.SqlDbType.Date).Value = fechaDevolucion.Date;
                cmd.Parameters.AddWithValue("@PrestamoId", prestamoId);
                cmd.Parameters.AddWithValue("@LibroId", libroId);
                if (await cmd.ExecuteNonQueryAsync() == 0)
                    throw new InvalidOperationException("Ese libro ya estaba devuelto. No se registró nada.");
            }

            const string sqlStock = "UPDATE Libros SET Ejemplares = Ejemplares + 1 WHERE LibroId = @LibroId";
            using (var cmd = new SqlCommand(sqlStock, cn, tx))
            {
                cmd.Parameters.AddWithValue("@LibroId", libroId);
                await cmd.ExecuteNonQueryAsync();
            }

            if (cerrarPrestamo)
            {
                const string sqlEstado = "UPDATE Prestamos SET Estado = N'Devuelto' WHERE PrestamoId = @PrestamoId";
                using var cmd = new SqlCommand(sqlEstado, cn, tx);
                cmd.Parameters.AddWithValue("@PrestamoId", prestamoId);
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<Prestamo> ObtenerAsync(int prestamoId)
    {
        const string sql = @"
            SELECT PrestamoId, SocioId, FechaPrestamo, FechaLimite, Estado
            FROM Prestamos WHERE PrestamoId = @PrestamoId";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@PrestamoId", prestamoId);
        await cn.OpenAsync();

        using var dr = await cmd.ExecuteReaderAsync();
        if (!await dr.ReadAsync()) return null;
        return new Prestamo
        {
            PrestamoId = dr.GetInt32(0),
            SocioId = dr.GetInt32(1),
            FechaPrestamo = dr.GetDateTime(2),
            FechaLimite = dr.GetDateTime(3),
            Estado = dr.GetString(4)
        };
    }

    // Reporte con INNER JOIN entre Prestamos, DetallePrestamo, Libros y Socios.
    public async Task<List<ReportePrestamo>> ReportePorFechasAsync(DateTime desde, DateTime hasta)
    {
        const string sql = @"
            SELECT p.PrestamoId, s.Nombre, l.Titulo, p.FechaPrestamo, p.FechaLimite, d.FechaDevolucion, p.Estado
            FROM Prestamos p
            INNER JOIN DetallePrestamo d ON d.PrestamoId = p.PrestamoId
            INNER JOIN Libros l ON l.LibroId = d.LibroId
            INNER JOIN Socios s ON s.SocioId = p.SocioId
            WHERE p.FechaPrestamo BETWEEN @Desde AND @Hasta
            ORDER BY p.FechaPrestamo, p.PrestamoId, l.Titulo";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add("@Desde", System.Data.SqlDbType.Date).Value = desde.Date;
        cmd.Parameters.Add("@Hasta", System.Data.SqlDbType.Date).Value = hasta.Date;
        await cn.OpenAsync();

        var lista = new List<ReportePrestamo>();
        using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
        {
            lista.Add(new ReportePrestamo
            {
                PrestamoId = dr.GetInt32(0),
                Socio = dr.GetString(1),
                Libro = dr.GetString(2),
                FechaPrestamo = dr.GetDateTime(3),
                FechaLimite = dr.GetDateTime(4),
                FechaDevolucion = dr.IsDBNull(5) ? null : dr.GetDateTime(5),
                Estado = dr.GetString(6)
            });
        }
        return lista;
    }
}
