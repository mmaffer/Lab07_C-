namespace Biblioteca.Entidades;

public class Libro
{
    public int LibroId { get; set; }
    public string Titulo { get; set; }
    public string ISBN { get; set; }
    public int AutorId { get; set; }
    public int Ejemplares { get; set; }
    public bool Activo { get; set; }

    // Dato de lectura que llega del JOIN con Autores (para mostrar en pantalla).
    public string AutorNombre { get; set; }
}
