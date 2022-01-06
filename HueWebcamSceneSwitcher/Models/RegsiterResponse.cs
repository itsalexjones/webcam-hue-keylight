using System.Text.Json.Serialization;

namespace HueWebcamSceneSwitcher.Models;

public class RegisterResponse
{
    [JsonPropertyName("success")] public Success? Success { get; set; }

    [JsonPropertyName("error")] public Error? Error { get; set; }
}

public class Error
{
    [JsonPropertyName("type")] public long Type { get; set; }

    [JsonPropertyName("address")] public string Address { get; set; }

    [JsonPropertyName("description")] public string Description { get; set; }
}

public class Success
{
    [JsonPropertyName("username")] public string Username { get; set; }

    [JsonPropertyName("clientkey")] public string Clientkey { get; set; }
}