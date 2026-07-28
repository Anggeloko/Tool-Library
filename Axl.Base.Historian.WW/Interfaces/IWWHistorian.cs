using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Axl.Base.Models;
using Axl.Base.Historian.WW.Models;

namespace Axl.Base.Historian.WW.Interfaces
{
    public interface IWWHistorian
    {
        /// <summary>
        /// Obtiene los valores actuales de una lista de etiquetas desde v_AnalogLive.
        /// </summary>
        Task<Result<List<ValTag>>> GetLiveValuesAsync(IEnumerable<string> tagPaths);

        /// <summary>
        /// Obtiene valores históricos en un rango de tiempo usando el modo Cyclic.
        /// </summary>
        Task<Result<List<ValTag>>> GetHistoricalValuesAsync(IEnumerable<string> tagNames, DateTime start, DateTime end, int cycleCount = 1);

        /// <summary>
        /// Realiza un chequeo de salud buscando etiquetas con mala calidad.
        /// </summary>
        Task<Result<List<WWResponse>>> GetHealthStatusAsync(bool onlyErrors = true);

        /// <summary>
        /// Helper para actualizar una lista de SupTags con datos del historiador.
        /// </summary>
        Task<Result<List<SupTag>>> UpdateTagsDataAsync(List<SupTag> tags, bool useHistorical = false, int minutesBack = 5);
    }
}
