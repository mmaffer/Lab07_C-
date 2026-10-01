using Biblioteca.Entidades;

namespace Biblioteca.Negocio.Interfaces;

public interface ISocioRepositorio
{
    Task<List<Socio>> ListarAsync();
    Task<List<Socio>> BuscarAsync(string texto);
    Task<Socio> ObtenerAsync(int socioId);
    Task<bool> ExisteDniAsync(string dni, int excluirSocioId);
    Task<int> InsertarAsync(Socio socio);
    Task ActualizarAsync(Socio socio);
    Task DesactivarAsync(int socioId);
}
