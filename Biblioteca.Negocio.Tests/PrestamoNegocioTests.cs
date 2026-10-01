using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Biblioteca.Negocio;
using Xunit;

namespace Biblioteca.Negocio.Tests;

public class PrestamoNegocioTests
{
    [Fact]
    public void CalcularDiasRetraso_SinRetraso_RetornaCero()
    {
        var fechaLimite = new DateTime(2026, 9, 10);
        var fechaDevolucion = new DateTime(2026, 9, 10);
        int dias = PrestamoNegocio.CalcularDiasRetraso(fechaLimite, fechaDevolucion);
        Assert.Equal(0, dias);
    }

    [Fact]
    public void CalcularMulta_Retrasado_RetornaMultiplicacion()
    {
        var fechaLimite = new DateTime(2026, 9, 10);
        var fechaDevolucion = new DateTime(2026, 9, 13);
        decimal multa = PrestamoNegocio.CalcularMulta(fechaLimite, fechaDevolucion);
        Assert.Equal(3 * PrestamoNegocio.MultaPorDia, multa);
    }

    [Fact]
    public async Task RegistrarAsync_SocioInvalido_LanzaReglaNegocioException()
    {
        var negocio = new PrestamoNegocio(null, null, null, null);
        await Assert.ThrowsAsync<ReglaNegocioException>(() => negocio.RegistrarAsync(0, new List<int>()));
    }
}
