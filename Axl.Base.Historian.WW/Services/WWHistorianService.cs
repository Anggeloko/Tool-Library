using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Historian.WW.Interfaces;
using Axl.Base.Historian.WW.Models;

namespace Axl.Base.Historian.WW.Services
{
    public class WWHistorianService : IWWHistorian
    {
        private readonly ISql _db;
        private readonly int _chunkSize;

        public WWHistorianService(ISql db, int chunkSize = 100)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _chunkSize = chunkSize;
        }

        public async Task<Result<List<ValTag>>> GetLiveValuesAsync(IEnumerable<string> tagPaths)
        {
            if (tagPaths == null || !tagPaths.Any())
                return Result<List<ValTag>>.Success(new List<ValTag>());

            var allResults = new List<ValTag>();
            var tagsList = tagPaths.ToList();

            for (int i = 0; i < tagsList.Count; i += _chunkSize)
            {
                var chunk = tagsList.Skip(i).Take(_chunkSize).ToList();
                var formattedTags = string.Join(",", chunk.Select(t => $"'{t}'"));

                var sql = $@"
                    SELECT 
                        Tag = v_AnalogLive.TagName, 
                        Actualizacion = DateTime, 
                        Valor = Value, 
                        Unit = ISNULL(Cast(EngineeringUnit.Unit as VarChar(20)),'N/A'), 
                        Quality, 
                        QualityDetail = v_AnalogLive.QualityDetail 
                    FROM v_AnalogLive 
                    LEFT JOIN AnalogTag ON AnalogTag.TagName = v_AnalogLive.TagName 
                    LEFT JOIN EngineeringUnit ON AnalogTag.EUKey = EngineeringUnit.EUKey 
                    WHERE v_AnalogLive.TagName IN ({formattedTags})";

                var result = await _db.GetList<ValTag>(sql);
                if (!result.IsSuccess) return Result<List<ValTag>>.Failure(result.Error);
                
                if (result.Value != null)
                    allResults.AddRange(result.Value);
            }

            return Result<List<ValTag>>.Success(allResults);
        }

        public async Task<Result<List<ValTag>>> GetHistoricalValuesAsync(IEnumerable<string> tagNames, DateTime start, DateTime end, int cycleCount = 1)
        {
            if (tagNames == null || !tagNames.Any())
                return Result<List<ValTag>>.Success(new List<ValTag>());

            var allResults = new List<ValTag>();
            var tagsList = tagNames.ToList();

            for (int i = 0; i < tagsList.Count; i += _chunkSize)
            {
                var chunk = tagsList.Skip(i).Take(_chunkSize).ToList();
                var formattedTags = string.Join(",", chunk.Select(t => $"'{t}'"));

                var sql = $@"
                    SELECT 
                        Tag = temp.TagName, 
                        Actualizacion = DateTime, 
                        Valor = Value, 
                        Unit = ISNULL(Cast(EngineeringUnit.Unit as VarChar(20)),'N/A'), 
                        Quality, 
                        QualityDetail = temp.QualityDetail 
                    FROM ( 
                        SELECT * FROM History 
                        WHERE History.TagName IN ({formattedTags}) 
                        AND wwRetrievalMode = 'Cyclic' 
                        AND wwCycleCount = {cycleCount} 
                        AND wwQualityRule = 'Extended' 
                        AND wwVersion = 'Latest' 
                        AND DateTime >= @start 
                        AND DateTime <= @end
                    ) temp 
                    LEFT JOIN AnalogTag ON AnalogTag.TagName = temp.TagName 
                    LEFT JOIN EngineeringUnit ON AnalogTag.EUKey = EngineeringUnit.EUKey 
                    WHERE temp.StartDateTime >= @start 
                    ORDER BY Actualizacion DESC";

                var parameters = new { start, end };
                var result = await _db.GetList<ValTag>(sql, parameters);
                if (!result.IsSuccess) return Result<List<ValTag>>.Failure(result.Error);

                if (result.Value != null)
                    allResults.AddRange(result.Value);
            }

            return Result<List<ValTag>>.Success(allResults);
        }

        public async Task<Result<List<WWResponse>>> GetHealthStatusAsync(bool onlyErrors = true)
        {
            var sqlFinal = $@"
                SELECT 
                    Tag = TagName, 
                    TagKey = wwTagKey, 
                    Mode = wwRetrievalMode, 
                    TimeDB = wwTimeDeadband, 
                    ValueDB = wwValueDeadband, 
                    TimeZone = wwTimeZone, 
                    Parameter = wwParameters, 
                    Selector = wwValueSelector 
                FROM v_Live";
            
            if (onlyErrors) sqlFinal += " WHERE Quality <> 192";

            return await _db.GetList<WWResponse>(sqlFinal);
        }

        public async Task<Result<List<SupTag>>> UpdateTagsDataAsync(List<SupTag> tags, bool useHistorical = false, int minutesBack = 5)
        {
            if (tags == null || !tags.Any()) return Result<List<SupTag>>.Success(tags);

            var tagNames = tags.Select(t => t.Tag).Where(t => !string.IsNullOrEmpty(t)).Distinct();
            Result<List<ValTag>> result;

            if (useHistorical)
            {
                var end = DateTime.Now;
                var start = end.AddMinutes(-minutesBack);
                result = await GetHistoricalValuesAsync(tagNames, start, end);
            }
            else
            {
                result = await GetLiveValuesAsync(tagNames);
            }

            if (!result.IsSuccess) return Result<List<SupTag>>.Failure(result.Error);

            var data = result.Value ?? new List<ValTag>();
            foreach (var tag in tags)
            {
                tag.Resultado = data.FirstOrDefault(d => d.Tag == tag.Tag);
            }

            return Result<List<SupTag>>.Success(tags);
        }
    }
}
