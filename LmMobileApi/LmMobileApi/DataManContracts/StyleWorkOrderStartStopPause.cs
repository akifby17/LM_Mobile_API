namespace LmMobileApi.DataManContracts;

public record StyleWorkOrderStartStopPause(string LoomNo, int PersonelId, string OperationCode, double PickDensity, double StyleLength, double ManuelLength, double ManuelWeight, int Status);