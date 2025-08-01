using LmMobileApi.Style.Domain;
using LmMobileApi.Style.Infrastructure.Repositories;
using LmMobileApi.Shared.Results;

namespace LmMobileApi.Style.Application.Services;

public class StyleWorkOrderService : IStyleWorkOrderService
{
    private readonly IStyleWorkOrderRepository _repository;

    public StyleWorkOrderService(IStyleWorkOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<StyleWorkOrdersWithFilters>> GetStyleWorkOrdersByModeAsync(StyleWorkOrderRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"🔧 StyleWorkOrderService - Mode: {request.Mode}, Filter: {request.Filter?.LoomGroupName ?? "null"}");
            
            var result = request.Mode switch
            {
                0 => await _repository.GetStyleWorkOrdersWithFiltersAsync(null, cancellationToken), // Tüm veriler + filtreler
                1 => await _repository.GetStyleWorkOrdersWithFiltersAsync(request.Filter, cancellationToken), // Filtrelenmiş veriler + filtreler
                _ => Result<StyleWorkOrdersWithFilters>.Failure("Invalid mode. Use 0 for all data or 1 for filtered data.")
            };
            
            Console.WriteLine($"🔧 StyleWorkOrderService Result - Success: {result.IsSuccess}, Error: {(result.Error != null ? result.Error.ToString() : "none")}");
            
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ StyleWorkOrderService Exception: {ex.Message}");
            return Result<StyleWorkOrdersWithFilters>.Failure($"Error processing request: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<StyleWorkOrder>>> GetStyleWorkOrdersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetStyleWorkOrdersAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<StyleWorkOrder>>.Failure($"Error getting style work orders: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<StyleWorkOrderFilterOption>>> GetFilterOptionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetFilterOptionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<StyleWorkOrderFilterOption>>.Failure($"Error getting filter options: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<StyleWorkOrder>>> GetFilteredStyleWorkOrdersAsync(StyleWorkOrderFilter filter, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetFilteredStyleWorkOrdersAsync(filter, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<StyleWorkOrder>>.Failure($"Error getting filtered style work orders: {ex.Message}");
        }
    }

    public async Task<Result<StyleWorkOrdersWithFilters>> GetStyleWorkOrdersWithFiltersAsync(StyleWorkOrderFilter? filter = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetStyleWorkOrdersWithFiltersAsync(filter, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<StyleWorkOrdersWithFilters>.Failure($"Error getting style work orders with filters: {ex.Message}");
        }
    }
} 