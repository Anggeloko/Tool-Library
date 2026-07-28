﻿using System.Threading.Tasks;
using Axl.Base.Models;

namespace Axl.Base.Interfaces
{
    /// <summary>
    /// Define un contrato para componentes que pueden reportar su estado de salud o disponibilidad.
    /// </summary>
    public interface ICheckable
    {
        /// <summary>
        /// Verifica de forma asíncrona si el componente está operativo.
        /// </summary>
        /// <returns>Un Result<bool> indicando el éxito de la verificación.</returns>
        Task<Result<bool>> CheckAsync();
    }
}

