namespace Biblioteca.Entidades;

public class ResultadoDevolucion
{
    public DateTime FechaDevolucion { get; set; }
    public int DiasRetraso { get; set; }
    public decimal Multa { get; set; }
    public bool PrestamoCerrado { get; set; }
}
