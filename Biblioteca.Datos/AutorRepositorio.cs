using Biblioteca.Datos.Interfaces;
using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

// Capa de Datos: solo habla con la base de datos. No valida reglas ni muestra mensajes.
public class AutorRepositorio : IAutorRepositorio, Biblioteca.Negocio.Interfaces.IAutorRepositorio
{
    public async Task<List<Autor>> ListarActivosAsync()
    {
        const string sql = "SELECT AutorId, Nombre, Nacionalidad, Activo FROM Autores WHERE Activo = 1 ORDER BY Nombre";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        await cn.OpenAsync();
        return await LeerAsync(cmd);
    }

    public async Task<Autor> ObtenerAsync(int autorId)
    {
        const string sql = "SELECT AutorId, Nombre, Nacionalidad, Activo FROM Autores WHERE AutorId = @AutorId";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@AutorId", autorId);
        await cn.OpenAsync();
        return (await LeerAsync(cmd)).FirstOrDefault();
    }

    private static async Task<List<Autor>> LeerAsync(SqlCommand cmd)
    {
        var lista = new List<Autor>();
        using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
        {
            lista.Add(new Autor
            {
                AutorId = dr.GetInt32(0),
                Nombre = dr.GetString(1),
                Nacionalidad = dr.IsDBNull(2) ? null : dr.GetString(2),
                Activo = dr.GetBoolean(3)
            });
        }
        return lista;
    }
}
