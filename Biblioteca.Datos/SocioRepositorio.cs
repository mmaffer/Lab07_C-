using Biblioteca.Datos.Interfaces;
using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

// Capa de Datos: solo habla con la base de datos. No valida reglas ni muestra mensajes.
public class SocioRepositorio : ISocioRepositorio, Biblioteca.Negocio.Interfaces.ISocioRepositorio
{
    private const string SelectColumnas = "SELECT SocioId, DNI, Nombre, Email, Activo FROM Socios";

    public async Task<List<Socio>> ListarAsync()
    {
        const string sql = SelectColumnas + " WHERE Activo = 1 ORDER BY Nombre";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        await cn.OpenAsync();
        return await LeerAsync(cmd);
    }

    public async Task<List<Socio>> BuscarAsync(string texto)
    {
        const string sql = SelectColumnas + @"
            WHERE Activo = 1 AND (Nombre LIKE @Texto OR DNI LIKE @Texto)
            ORDER BY Nombre";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Texto", "%" + texto + "%");
        await cn.OpenAsync();
        return await LeerAsync(cmd);
    }

    public async Task<Socio> ObtenerAsync(int socioId)
    {
        const string sql = SelectColumnas + " WHERE SocioId = @SocioId";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@SocioId", socioId);
        await cn.OpenAsync();
        return (await LeerAsync(cmd)).FirstOrDefault();
    }

    public async Task<bool> ExisteDniAsync(string dni, int excluirSocioId)
    {
        const string sql = "SELECT COUNT(1) FROM Socios WHERE DNI = @DNI AND SocioId <> @SocioId";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@DNI", dni);
        cmd.Parameters.AddWithValue("@SocioId", excluirSocioId);
        await cn.OpenAsync();
        return (int)await cmd.ExecuteScalarAsync() > 0;
    }

    public async Task<int> InsertarAsync(Socio socio)
    {
        const string sql = @"
            INSERT INTO Socios (DNI, Nombre, Email)
            OUTPUT INSERTED.SocioId
            VALUES (@DNI, @Nombre, @Email)";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        AgregarParametros(cmd, socio);
        await cn.OpenAsync();
        return (int)await cmd.ExecuteScalarAsync();
    }

    public async Task ActualizarAsync(Socio socio)
    {
        const string sql = @"
            UPDATE Socios SET DNI = @DNI, Nombre = @Nombre, Email = @Email
            WHERE SocioId = @SocioId";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        AgregarParametros(cmd, socio);
        cmd.Parameters.AddWithValue("@SocioId", socio.SocioId);
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    // Eliminación lógica: nunca DELETE físico.
    public async Task DesactivarAsync(int socioId)
    {
        const string sql = "UPDATE Socios SET Activo = 0 WHERE SocioId = @SocioId";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@SocioId", socioId);
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    private static void AgregarParametros(SqlCommand cmd, Socio socio)
    {
        cmd.Parameters.AddWithValue("@DNI", socio.DNI);
        cmd.Parameters.AddWithValue("@Nombre", socio.Nombre);
        cmd.Parameters.AddWithValue("@Email", (object)socio.Email ?? DBNull.Value);
    }

    private static async Task<List<Socio>> LeerAsync(SqlCommand cmd)
    {
        var lista = new List<Socio>();
        using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
        {
            lista.Add(new Socio
            {
                SocioId = dr.GetInt32(0),
                DNI = dr.GetString(1),
                Nombre = dr.GetString(2),
                Email = dr.IsDBNull(3) ? null : dr.GetString(3),
                Activo = dr.GetBoolean(4)
            });
        }
        return lista;
    }
}
