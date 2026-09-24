using Axl.Base.Interfaces;
using Axl.Base.Models;

namespace Axl.Base.Ilo.Domain.Ports
{
    /// <summary>
    /// Alias de compatibilidad hacia atrás para IIloService.
    /// La interfaz principal reside ahora en Axl.Base.Common.Interfaces.IIloService.
    /// </summary>
    public interface IIloService : Axl.Base.Interfaces.IIloService
    {
    }
}
