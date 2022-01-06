using System.Text.Json.Serialization;

namespace HueWebcamSceneSwitcher.Models;

public class Datum
{
    public Color? color { get; set; }

    [JsonPropertyName("color_temperature")]
    public ColorTemperature? color_temperature { get; set; }

    public Dimming? dimming { get; set; }
    public string? id { get; set; }
    public string? id_v1 { get; set; }
    public Metadata? metadata { get; set; }
    public string? mode { get; set; }
    public On? on { get; set; }
}

public class HueLightResponse
{
    public IList<object> errors { get; set; }
    public IList<Datum> data { get; set; }
}

public class On
{
    public bool on { get; set; }
}