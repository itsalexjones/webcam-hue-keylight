using System.Text.Json.Serialization;

namespace HueWebcamSceneSwitcher.Models;

public class RegisterMessage
{
    [JsonPropertyName("devicetype")] public string DeviceType { get; set; } = "WebcamKeylight";

    [JsonPropertyName("generateclientkey")]
    public bool GenerateClientKey { get; set; } = true;
}