namespace Biblioteca.Entidades;

// Fila del reporte: resultado del INNER JOIN Prestamos + DetallePrestamo + Libros + Socios.
public class ReportePrestamo
{
    public int PrestamoId { get; set; }
    public string Socio { get; set; }
    public string Libro { get; set; }
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaLimite { get; set; }
    public DateTime? FechaDevolucion { get; set; }
    public string Estado { get; set; }
}
