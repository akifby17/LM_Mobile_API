using Dapper;
using LmMobileApi.Dashboard.Domain;
using LmMobileApi.Shared.Data;
using LmMobileApi.Shared.Results;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Globalization;
using System.Threading;

namespace LmMobileApi.Dashboard.Infrastructure.Repositories;
public record PastDatePieChartRequest(string StartDate, string EndDate);
public interface IDashboardRepository : IRepository
{
    Task<Result<ActiveShiftPieChart>> GetActiveShiftPieChartAsync(CancellationToken cancellationToken = default);
    Task<Result<ActiveShiftPieChart>> GetPastDatePieChartAsync(
    PastDateMode mode,
    string? customStartDate,
    string? customEndDate,
    CancellationToken cancellationToken = default);

    // Yeni metodlar - dual data için (PieChart pattern'i ile)
    Task<Result<List<Dictionary<string, object>>>> GetOperationsDataAsync(
        PastDateMode mode,
        string? customStartDate,
        string? customEndDate,
        CancellationToken cancellationToken = default);
    Task<Result<DashboardDualResponse>> GetDashboardDualDataAsync(
        PastDateMode mode,
        string? customStartDate,
        string? customEndDate,
        CancellationToken cancellationToken = default);
}

public class DashboardRepository(IUnitOfWork unitOfWork) : DapperRepository(unitOfWork), IDashboardRepository
{
    public async Task<Result<ActiveShiftPieChart>> GetActiveShiftPieChartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            const string query = @"EXEC dbo.tsp_GetActiveShiftPieChart";

            // Stored procedure Label-Value formatında 7 satır döndürüyor
            var rawResult = await QueryAsync<dynamic>(query, null, cancellationToken: cancellationToken);
            Console.WriteLine($"Stored procedure returned {rawResult.Data?.Count()} rows");

            if (rawResult.Data?.Any() != true)
            {
                Console.WriteLine("No data returned from stored procedure");
                return Result<ActiveShiftPieChart>.Failure("NoData", "No data returned from stored procedure");
            }

            // Label-Value çiftlerini dictionary'ye çevir
            var dataDict = new Dictionary<string, double>();

            foreach (var row in rawResult.Data)
            {
                var dynamicRow = (IDictionary<string, object>)row;
                var label = dynamicRow["Label"]?.ToString() ?? "";
                var valueStr = dynamicRow["Value"]?.ToString() ?? "0";

                Console.WriteLine($"Processing: Label={label}, Value={valueStr}");

                // Türkçe ondalık ayracını (,) nokta (.) ile değiştir
                valueStr = valueStr.Replace(',', '.');

                if (double.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    dataDict[label] = value;
                }
                else
                {
                    Console.WriteLine($"Could not parse value: {valueStr}");
                    dataDict[label] = 0;
                }
            }

            // Dictionary'den ActiveShiftPieChart model'ini oluştur
            var pieChart = new ActiveShiftPieChart
            {
                Efficiency = dataDict.GetValueOrDefault("Efficiency", 0),
                WeftStop = dataDict.GetValueOrDefault("WeftStop", 0),
                WarpStop = dataDict.GetValueOrDefault("WarpStop", 0),
                OtherStop = dataDict.GetValueOrDefault("OtherStop", 0),
                OperationStop = dataDict.GetValueOrDefault("OperationStop", 0),
                PickCounter = dataDict.GetValueOrDefault("PickCounter", 0),
                ProductedLength = dataDict.GetValueOrDefault("ProductedLength", 0),
                WeftKa = dataDict.GetValueOrDefault("WeftKa", 0),
                WarpKa = dataDict.GetValueOrDefault("WarpKa", 0),
                LoomCount = dataDict.GetValueOrDefault("LoomCount", 0.0),
                AvgSpeed = dataDict.GetValueOrDefault("AvgSpeed", 0.0),
                WeaverEff = dataDict.GetValueOrDefault("WeaverEff", 0.0),
                AvgDensity = dataDict.GetValueOrDefault("AvgDensity", 0.0)
            };

            Console.WriteLine($"Final mapped data:");
            Console.WriteLine($"  Efficiency: {pieChart.Efficiency}");
            Console.WriteLine($"  WeftStop: {pieChart.WeftStop}");
            Console.WriteLine($"  WarpStop: {pieChart.WarpStop}");
            Console.WriteLine($"  OtherStop: {pieChart.OtherStop}");
            Console.WriteLine($"  OperationStop: {pieChart.OperationStop}");
            Console.WriteLine($"  PickCounter: {pieChart.PickCounter}");
            Console.WriteLine($"  ProductedLength: {pieChart.ProductedLength}");

            return Result<ActiveShiftPieChart>.Success(pieChart);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetActiveShiftPieChartAsync: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return Result<ActiveShiftPieChart>.Failure("DatabaseError", ex.Message);
        }
    }
    public async Task<Result<ActiveShiftPieChart>> GetPastDatePieChartAsync(
    PastDateMode mode,
    string? customStartDate,
    string? customEndDate,
    CancellationToken cancellationToken = default)
    {
         string query= @"
        EXEC dbo.tsp_Monitoring_BetweenDatePieChart 
            @Mode, @CustomStartDate, @CustomEndDate"; ;
        if (mode == 0)
        {
            query = @"EXEC dbo.tsp_GetActiveShiftPieChart";
        }
         

        // 1) Mode -> string
        var modeString = mode.ToString();

        // 2) Custom tarihleri parse et (sadece Custom modu için)
        DateTime? startDt = null, endDt = null;
        if (mode == PastDateMode.Custom)
        {
            if (!DateTime.TryParse(customStartDate, CultureInfo.InvariantCulture,
                                   DateTimeStyles.None, out var tmpStart) ||
                !DateTime.TryParse(customEndDate, CultureInfo.InvariantCulture,
                                   DateTimeStyles.None, out var tmpEnd))
            {
                return Result<ActiveShiftPieChart>.Failure(
                    "InvalidDate",
                    "Custom modda geçersiz tarih formatı.");
            }
            startDt = tmpStart;
            endDt = tmpEnd;
        }

        // 3) Parametreleri hazırla
        var parameters = new
        {
            Mode = modeString,
            CustomStartDate = (object?)startDt ?? DBNull.Value,
            CustomEndDate = (object?)endDt ?? DBNull.Value
        };

        // 4) SP çağrısı
        var rawResult = await QueryAsync<dynamic>(
            query, parameters, cancellationToken: cancellationToken);

        if (rawResult.Data == null || !rawResult.Data.Any())
            return Result<ActiveShiftPieChart>.Failure("NoData", "No data returned.");

        // 5) Sonuçları sözlüğe dönüştürme
        var dataDict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rawResult.Data.Cast<IDictionary<string, object>>())
        {
            var label = row["Label"]?.ToString() ?? "";
            var valStr = row["Value"]?.ToString()?.Replace(',', '.') ?? "0";
            dataDict[label] = double.TryParse(
                valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var dv)
                ? dv
                : 0.0;
        }

        // 6) Modeli oluştur
        var pieChart = new ActiveShiftPieChart
        {
            Efficiency = dataDict.GetValueOrDefault("Efficiency"),
            WeftStop = dataDict.GetValueOrDefault("WeftStop"),
            WarpStop = dataDict.GetValueOrDefault("WarpStop"),
            OtherStop = dataDict.GetValueOrDefault("OtherStop"),
            OperationStop = dataDict.GetValueOrDefault("OperationStop"),
            PickCounter = dataDict.GetValueOrDefault("PickCounter"),
            ProductedLength = dataDict.GetValueOrDefault("ProductedLength"),
            WeftKa = dataDict.GetValueOrDefault("WeftKa"),
            WarpKa = dataDict.GetValueOrDefault("WarpKa"),
            LoomCount = dataDict.GetValueOrDefault("LoomCount"),
            AvgSpeed = dataDict.GetValueOrDefault("AvgSpeed", 0.0),
            WeaverEff = dataDict.GetValueOrDefault("WeaverEff", 0.0),
            AvgDensity = dataDict.GetValueOrDefault("AvgDensity", 0.0)
        };

        return Result<ActiveShiftPieChart>.Success(pieChart);
    }

    // Operations verilerini al - GetPastDatePieChartAsync pattern'i ile
    public async Task<Result<List<Dictionary<string, object>>>> GetOperationsDataAsync(
        PastDateMode mode,
        string? customStartDate,
        string? customEndDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1) Mode'a göre query ve parametreleri seç
            string query;
            object? parameters;
            
            if (mode == PastDateMode.Active)
            {
                // Active mode - parametresiz SP
                query = @"EXEC dbo.tsp_GetCurrentShiftDurationPercentages";
                parameters = null;
            }
            else
            {
                // Diğer modlar - parametreli SP
                query = @"EXEC dbo.tsp_GetOperationSummaryByPeriod @Mode, @CustomStartDate, @CustomEndDate";
                
                // 2) Mode -> string
                var modeString = mode.ToString();

                // 3) Custom tarihleri parse et
                DateTime? startDt = null, endDt = null;
                if (mode == PastDateMode.Custom)
                {
                    if (!DateTime.TryParse(customStartDate, CultureInfo.InvariantCulture,
                                           DateTimeStyles.None, out var tmpStart) ||
                        !DateTime.TryParse(customEndDate, CultureInfo.InvariantCulture,
                                           DateTimeStyles.None, out var tmpEnd))
                    {
                        return Result<List<Dictionary<string, object>>>.Failure(
                            "InvalidDate",
                            "Custom modda geçersiz tarih formatı.");
                    }
                    startDt = tmpStart;
                    endDt = tmpEnd;
                }

                // 4) Parametreleri hazırla
                parameters = new
                {
                    Mode = modeString,
                    CustomStartDate = (object?)startDt ?? DBNull.Value,
                    CustomEndDate = (object?)endDt ?? DBNull.Value
                };
            }
            
            Console.WriteLine($"🔍 Operations SP Query: {query}");
            Console.WriteLine($"🔍 Operations Mode: {mode}");

            // 5) SP çağrısı (Mode'a göre parametreli/parametresiz)
            var rawResult = await QueryAsync<dynamic>(
                query, 
                parameters, // Active: null, Diğerleri: parametreli
                cancellationToken: cancellationToken);
            
            Console.WriteLine($"🔍 Operations SP Success: {rawResult.IsSuccess}");
            Console.WriteLine($"🔍 Operations SP Data Count: {rawResult.Data?.Count() ?? 0}");
            
            if (!rawResult.IsSuccess)
            {
                Console.WriteLine($"❌ Operations SP failed: {rawResult.Error}");
                return Result<List<Dictionary<string, object>>>.Failure($"SP execution failed: {rawResult.Error}");
            }
            
            if (rawResult.Data?.Any() != true)
            {
                Console.WriteLine("❌ No operations data returned from SP");
                return Result<List<Dictionary<string, object>>>.Success(new List<Dictionary<string, object>>());
            }

            // 6) Sonuçları işle (PieChart pattern'i uyarlanmış)
            var operationsList = new List<Dictionary<string, object>>();
            
            foreach (var row in rawResult.Data.Cast<IDictionary<string, object>>())
            {
                var operationDict = new Dictionary<string, object>();
                
                // Tüm kolonları dictionary'ye ekle (dinamik)
                foreach (var kvp in row)
                {
                    // Value formatını düzenle (PieChart'taki gibi)
                    var value = kvp.Value ?? string.Empty;
                    if (kvp.Key.Equals("DurationPercent", StringComparison.OrdinalIgnoreCase))
                    {
                        // Türkçe ondalık ayracını düzelt
                        var valStr = value.ToString()?.Replace(',', '.') ?? "0";
                        if (double.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleVal))
                        {
                            operationDict[kvp.Key] = doubleVal;
                        }
                        else
                        {
                            operationDict[kvp.Key] = value;
                        }
                    }
                    else
                    {
                        operationDict[kvp.Key] = value;
                    }
                }
                
                operationsList.Add(operationDict);
            }
            
            Console.WriteLine($"✅ Operations final count: {operationsList.Count}");

            return Result<List<Dictionary<string, object>>>.Success(operationsList);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Operations error: {ex.Message}");
            return Result<List<Dictionary<string, object>>>.Failure($"Operations data error: {ex.Message}");
        }
    }

    // Dual data metodunu buraya taşıyıp repository'de birleştir
    public async Task<Result<DashboardDualResponse>> GetDashboardDualDataAsync(
        PastDateMode mode,
        string? customStartDate,
        string? customEndDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Charts verisini al
            var chartsResult = await GetPastDatePieChartAsync(mode, customStartDate, customEndDate, cancellationToken);
            if (!chartsResult.IsSuccess)
                return Result<DashboardDualResponse>.Failure(chartsResult.Error);

            // Operations verisini al (aynı parametrelerle)
            var operationsResult = await GetOperationsDataAsync(mode, customStartDate, customEndDate, cancellationToken);
            if (!operationsResult.IsSuccess)
                return Result<DashboardDualResponse>.Failure(operationsResult.Error);

            // Dual response oluştur
            var dualResponse = new DashboardDualResponse
            {
                Charts = new List<ActiveShiftPieChart> { chartsResult.Data! },
                Operations = operationsResult.Data!
            };

            return Result<DashboardDualResponse>.Success(dualResponse);
        }
        catch (Exception ex)
        {
            return Result<DashboardDualResponse>.Failure($"Dashboard dual data error: {ex.Message}");
        }
    }


    /*string query = @"
    EXEC dbo.tsp_GetOperationSummaryByPeriod 
        @Mode, @CustomStartDate, @CustomEndDate"; ;
    if (mode == 0)
    {
        query = @"EXEC dbo.tsp_GetCurrentShiftDurationPercentages";
    }*/

    


}


