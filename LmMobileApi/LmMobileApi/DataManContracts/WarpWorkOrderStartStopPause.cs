namespace LmMobileApi.DataManContracts;

public record WarpWorkOrderStartStopPause(string LoomNo, int PersonelId, int WarpWorkOrderNo, double WarpLength, int Status);