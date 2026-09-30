namespace Biblioteca.Entidades;

// Libro pendiente de devolución (lectura para el formulario de devolución).
public class PrestamoPendiente
{
    public int PrestamoId { get; set; }
    public int LibroId { get; set; }
    public string Titulo { get; set; }
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaLimite { get; set; }
}
