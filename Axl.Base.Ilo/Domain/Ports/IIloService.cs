using Axl.Base.Ilo.Domain.Models;

namespace Axl.Base.Ilo.Domain.Ports
{
    /// <summary>
    /// Puerto de Entrada/Salida (Port) que expone la capacidad de obtener métricas.
    /// Define el contrato de lo que requiere el dominio para interactuar con la infraestructura.
    /// </summary>
    public interface IIloService
    {
        IloMetrics GetMetrics(string ip, string username, string password);
    }
}
