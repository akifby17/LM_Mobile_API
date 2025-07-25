using LmMobileApi.DataManContracts;
using LmMobileApi.Looms.Domain;
using LmMobileApi.Looms.Infrastructure.Repositories;
using LmMobileApi.Shared.Application;
using LmMobileApi.Shared.Results;

namespace LmMobileApi.Looms.Application.Services;

public interface ILoomService
{
    Task<Result<IEnumerable<Loom>>> GetLoomMonitoringAsync(CancellationToken cancellationToken = default);
    Task<Result> ChangeWeaverAsync(ChangeWeaver changeWeaver, CancellationToken cancellationToken = default);
    Task<Result> OperationStartStopAsync(OperationStartStop operationStartStop, CancellationToken cancellationToken = default);
    Task<Result> PieceCuttingAsync(PieceCutting pieceCutting, CancellationToken cancellationToken = default);
    Task<Result> StyleWorkOrderStartStopPauseAsync(StyleWorkOrderStartStopPause styleWorkOrderStartStopPause, CancellationToken cancellationToken = default);
    Task<Result> WarpWorkOrderStartStopPauseAsync(WarpWorkOrderStartStopPause warpWorkOrderStartStopPause, CancellationToken cancellationToken = default);
    Task<Result> WarpWorkOrder23StartStopPauseAsync(WarpWorkOrder23StartStopPause warpWorkOrder23StartStopPause, CancellationToken cancellationToken = default);
}

public class LoomService : ApplicationService, ILoomService
{
    private readonly HttpClient httpClient;

    public LoomService(ILoomRepository repository, IHttpClientFactory httpClientFactory) : base(repository)
    {
        httpClient = httpClientFactory.CreateClient("DataManApi");
    }

    private ILoomRepository LoomRepository => Repository as ILoomRepository ?? throw new InvalidOperationException("Repository is not of type ILoomRepository");

    public async Task<Result> ChangeWeaverAsync(ChangeWeaver changeWeaver, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("changeWeaver", changeWeaver, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var dataManResponse = await response.Content.ReadFromJsonAsync<DataManResponse>(cancellationToken: cancellationToken);
                if (dataManResponse is null)
                {
                    return Result.Failure("Error while processing the request.");
                }

                if (dataManResponse.Status)
                {
                    return true;
                }
                else
                {
                    return Result.Failure(dataManResponse.Message ?? "Unknown error occurred.");
                }
            }
            else
            {
                return Result.Failure("Error while processing the request.");
            }
        }
        catch (Exception e)
        {
            return Result.Failure($"An error occurred: {e.Message}");
        }
    }

    public Task<Result<IEnumerable<Loom>>> GetLoomMonitoringAsync(CancellationToken cancellationToken = default)
    {
        return LoomRepository.GetLoomsCurrentlyStatusAsync(cancellationToken);
    }

    public async Task<Result> OperationStartStopAsync(OperationStartStop operationStartStop, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("operationStartStop", operationStartStop, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var dataManResponse = await response.Content.ReadFromJsonAsync<DataManResponse>(cancellationToken: cancellationToken);
                if (dataManResponse is null)
                {
                    return Result.Failure("Error while processing the request.");
                }

                if (dataManResponse.Status)
                {
                    return true;
                }
                else
                {
                    return Result.Failure(dataManResponse.Message ?? "Unknown error occurred.");
                }
            }
            else
            {
                return Result.Failure("Error while processing the request.");
            }
        }
        catch (Exception e)
        {
            return Result.Failure($"An error occurred: {e.Message}");
        }
    }

    public async Task<Result> PieceCuttingAsync(PieceCutting pieceCutting, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("pieceCutting", pieceCutting, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var dataManResponse = await response.Content.ReadFromJsonAsync<DataManResponse>(cancellationToken: cancellationToken);
                if (dataManResponse is null)
                {
                    return Result.Failure("Error while processing the request.");
                }

                if (dataManResponse.Status)
                {
                    return true;
                }
                else
                {
                    return Result.Failure(dataManResponse.Message ?? "Unknown error occurred.");
                }
            }
            else
            {
                return Result.Failure("Error while processing the request.");
            }
        }
        catch (Exception e)
        {
            return Result.Failure($"An error occurred: {e.Message}");
        }
    }

    public async Task<Result> StyleWorkOrderStartStopPauseAsync(StyleWorkOrderStartStopPause styleWorkOrderStartStopPause, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("styleWorkOrderStartStopPause", styleWorkOrderStartStopPause, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var dataManResponse = await response.Content.ReadFromJsonAsync<DataManResponse>(cancellationToken: cancellationToken);
                if (dataManResponse is null)
                {
                    return Result.Failure("Error while processing the request.");
                }

                if (dataManResponse.Status)
                {
                    return true;
                }
                else
                {
                    return Result.Failure(dataManResponse.Message ?? "Unknown error occurred.");
                }
            }
            else
            {
                return Result.Failure("Error while processing the request.");
            }
        }
        catch (Exception e)
        {
            return Result.Failure($"An error occurred: {e.Message}");
        }
    }

    public async Task<Result> WarpWorkOrder23StartStopPauseAsync(WarpWorkOrder23StartStopPause warpWorkOrder23StartStopPause, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("warpWorkOrder23StartStopPause", warpWorkOrder23StartStopPause, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var dataManResponse = await response.Content.ReadFromJsonAsync<DataManResponse>(cancellationToken: cancellationToken);
                if (dataManResponse is null)
                {
                    return Result.Failure("Error while processing the request.");
                }

                if (dataManResponse.Status)
                {
                    return true;
                }
                else
                {
                    return Result.Failure(dataManResponse.Message ?? "Unknown error occurred.");
                }
            }
            else
            {
                return Result.Failure("Error while processing the request.");
            }
        }
        catch (Exception e)
        {
            return Result.Failure($"An error occurred: {e.Message}");
        }
    }

    public async Task<Result> WarpWorkOrderStartStopPauseAsync(WarpWorkOrderStartStopPause warpWorkOrderStartStopPause, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("warpWorkOrderStartStopPause", warpWorkOrderStartStopPause, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var dataManResponse = await response.Content.ReadFromJsonAsync<DataManResponse>(cancellationToken: cancellationToken);
                if (dataManResponse is null)
                {
                    return Result.Failure("Error while processing the request.");
                }

                if (dataManResponse.Status)
                {
                    return true;
                }
                else
                {
                    return Result.Failure(dataManResponse.Message ?? "Unknown error occurred.");
                }
            }
            else
            {
                return Result.Failure("Error while processing the request.");
            }
        }
        catch (Exception e)
        {
            return Result.Failure($"An error occurred: {e.Message}");
        }
    }
}
