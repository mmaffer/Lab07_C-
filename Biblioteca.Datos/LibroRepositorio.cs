using Biblioteca.Datos.Interfaces;
using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos;

// Capa de Datos: solo habla con la base de datos. No valida reglas ni muestra mensajes.
// Consultas siempre parametrizadas (nunca concatenadas) para evitar inyección SQL.
public class LibroRepositorio : ILibroRepositorio, Biblioteca.Negocio.Interfaces.ILibroRepositorio
{
    // Libros con el nombre de su autor (JOIN): LibroId, Titulo, ISBN, AutorId, Ejemplares, Activo, Autor.
    private const string SelectConAutor = @"
        SELECT l.LibroId, l.Titulo, l.ISBN, l.AutorId, l.Ejemplares, l.Activo, a.Nombre
        FROM Libros l
        INNER JOIN Autores a ON a.AutorId = l.AutorId";

    public async Task<List<Libro>> ListarAsync()
    {
        const string sql = SelectConAutor + " WHERE l.Activo = 1 ORDER BY l.Titulo";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        await cn.OpenAsync();
        return await LeerAsync(cmd);
    }

    public async Task<List<Libro>> BuscarAsync(string texto)
    {
        // El comodín % se agrega al valor del parámetro, no al texto del SQL.
        const string sql = SelectConAutor + @"
            WHERE l.Activo = 1 AND (l.Titulo LIKE @Texto OR a.Nombre LIKE @Texto)
            ORDER BY l.Titulo";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Texto", "%" + texto + "%");
        await cn.OpenAsync();
        return await LeerAsync(cmd);
    }

    public async Task<Libro> ObtenerAsync(int libroId)
    {
        const string sql = SelectConAutor + " WHERE l.LibroId = @LibroId";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@LibroId", libroId);
        await cn.OpenAsync();
        return (await LeerAsync(cmd)).FirstOrDefault();
    }

    public async Task<bool> ExisteIsbnAsync(string isbn, int excluirLibroId)
    {
        const string sql = "SELECT COUNT(1) FROM Libros WHERE ISBN = @ISBN AND LibroId <> @LibroId";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@ISBN", isbn);
        cmd.Parameters.AddWithValue("@LibroId", excluirLibroId);
        await cn.OpenAsync();
        return (int)await cmd.ExecuteScalarAsync() > 0;
    }

    public async Task<int> InsertarAsync(Libro libro)
    {
        const string sql = @"
            INSERT INTO Libros (Titulo, ISBN, AutorId, Ejemplares)
            OUTPUT INSERTED.LibroId
            VALUES (@Titulo, @ISBN, @AutorId, @Ejemplares)";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        AgregarParametros(cmd, libro);
        await cn.OpenAsync();
        return (int)await cmd.ExecuteScalarAsync();
    }

    public async Task ActualizarAsync(Libro libro)
    {
        const string sql = @"
            UPDATE Libros
            SET Titulo = @Titulo, ISBN = @ISBN, AutorId = @AutorId, Ejemplares = @Ejemplares
            WHERE LibroId = @LibroId";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        AgregarParametros(cmd, libro);
        cmd.Parameters.AddWithValue("@LibroId", libro.LibroId);
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    // Eliminación lógica: nunca DELETE físico.
    public async Task DesactivarAsync(int libroId)
    {
        const string sql = "UPDATE Libros SET Activo = 0 WHERE LibroId = @LibroId";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@LibroId", libroId);
        await cn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    private static void AgregarParametros(SqlCommand cmd, Libro libro)
    {
        cmd.Parameters.AddWithValue("@Titulo", libro.Titulo);
        cmd.Parameters.AddWithValue("@ISBN", libro.ISBN);
        cmd.Parameters.AddWithValue("@AutorId", libro.AutorId);
        cmd.Parameters.AddWithValue("@Ejemplares", libro.Ejemplares);
    }

    private static async Task<List<Libro>> LeerAsync(SqlCommand cmd)
    {
        var lista = new List<Libro>();
        using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync())
        {
            lista.Add(new Libro
            {
                LibroId = dr.GetInt32(0),
                Titulo = dr.GetString(1),
                ISBN = dr.GetString(2),
                AutorId = dr.GetInt32(3),
                Ejemplares = dr.GetInt32(4),
                Activo = dr.GetBoolean(5),
                AutorNombre = dr.GetString(6)
            });
        }
        return lista;
    }
}
