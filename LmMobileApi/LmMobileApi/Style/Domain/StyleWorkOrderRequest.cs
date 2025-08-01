using System.Text.Json.Serialization;

namespace LmMobileApi.Style.Domain;

public class StyleWorkOrderRequest
{
    /// <summary>
    /// 0 = Tüm veriler + filtreler, 1 = Filtrelenmiş veriler + filtreler
    /// </summary>
    [JsonPropertyName("mode")]
    public int Mode { get; set; }

    /// <summary>
    /// Mode 1 ise zorunlu, Mode 0 ise görmezden gelinir
    /// </summary>
    [JsonPropertyName("filter")]
    public StyleWorkOrderFilter? Filter { get; set; }
}