using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HueWebcamSceneSwitcher.Models;
using Microsoft.Win32;

namespace HueWebcamSceneSwitcher;

public class Worker : BackgroundService
{
    private readonly double _brightness;
    private readonly int _colourTemp;
    private readonly IConfiguration _config;
    private readonly HttpClientHandler _httpClientHandler;
    private readonly JsonSerializerOptions _jsonOpts;
    private readonly ILogger<Worker> _logger;
    private readonly int _timeout;
    private string _appKey;
    private string _bridgeIp;
    private Datum _keyLight;
    private bool _wasRunning;

    public Worker(ILogger<Worker> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
        _timeout = config.GetValue<int>("CheckTimeout");
        _appKey = config.GetValue<string>("HueBridge:AppKey");
        _colourTemp = config.GetValue<int>("ActiveColourTemp");
        _brightness = config.GetValue<double>("ActiveBrightnessPercent");
        _httpClientHandler = new HttpClientHandler();
        _httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
        _jsonOpts = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _bridgeIp = await DiscoverBridge() ?? throw new Exception("No hue bridges found");

        if (_appKey == string.Empty)
        {
            // We have not registered with the bridge yet. So do that!
            _appKey = await DoRegistration();
            _logger.LogWarning("Registered with bridge. Save this key to appsettings AppKey var {AppKey}", _appKey);
        }

        var httpClient = new HttpClient(_httpClientHandler);
        httpClient.BaseAddress = new Uri($"https://{_bridgeIp}/clip/v2/");
        httpClient.DefaultRequestHeaders.Add("hue-application-key", _appKey);

        // Get key light info
        var lights =
            await httpClient.GetFromJsonAsync<HueLightResponse>("resource/light", stoppingToken);
        if (lights == null) throw new Exception("No lights found in hue system");

        _keyLight = lights.data.First(l => l.metadata.name == _config.GetValue<string>("LightName"));
        _logger.LogDebug("Found light: {LightName}", _keyLight.metadata.name);

        _logger.LogInformation("Found bridge and lights. Ready to go!");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_timeout, stoppingToken);

            var running = IsWebCamInUse();
            if (running) // Is the Webcam active?
            {
                if (_wasRunning) // If we already knew it was active, do nothing
                {
                    _logger.LogTrace("Webcam still active");
                    continue;
                }

                // It is freshly active. So change scene
                _wasRunning = true;
                _logger.LogInformation("Webcam active! Changing lights");

                // Check if the light we want to change is on first!
                var r = await httpClient.GetFromJsonAsync<HueLightResponse>($"resource/light/{_keyLight.id}",
                    stoppingToken);
                if (r == null) throw new Exception("Light not found??????");
                var on = r.data[0].on;
                if (on is { on: false })
                {
                    _logger.LogInformation("Key light is off. Bailing out");
                    continue;
                }

                // Store the current state to be restored when the webcam turns off.
                _keyLight = r.data.First();

                // Change the light settings
                var body = new Datum
                {
                    color_temperature = new ColorTemperature
                    {
                        mirek = _colourTemp
                    },
                    dimming = new Dimming
                    {
                        brightness = _brightness
                    }
                };
                var res = await httpClient.PutAsJsonAsync($"resource/light/{_keyLight.id}", body, _jsonOpts,
                    stoppingToken);
                res.EnsureSuccessStatusCode();
            }
            else
            {
                if (_wasRunning) // The webcam was active last time we checked & now it isn't. Change scene back
                {
                    _logger.LogInformation("Webcam inactive. Changing lights");
                    _wasRunning = false;
                    var body = new Datum
                    {
                        color = _keyLight.color,
                        dimming = _keyLight.dimming
                    };
                    var result = await httpClient.PutAsJsonAsync($"resource/light/{_keyLight.id}", body, _jsonOpts,
                        stoppingToken);
                    result.EnsureSuccessStatusCode();
                }
                else
                {
                    _logger.LogTrace("Webcam still inactive");
                }
            }
        }
    }

    private async ValueTask<string> DoRegistration()
    {
        _logger.LogWarning("Not registered to bridge. PRESS BRIDGE BUTTON WITHIN NEXT 10 SECONDS");
        await Task.Delay(TimeSpan.FromSeconds(10));
        _logger.LogInformation("Starting registration process");
        var result =
            await new HttpClient(_httpClientHandler).PostAsJsonAsync($"https://{_bridgeIp}/api", new RegisterMessage());
        result.EnsureSuccessStatusCode();
        var t = await result.Content.ReadAsStringAsync();
        var response =
            await JsonSerializer.DeserializeAsync<RegisterResponse[]>(await result.Content.ReadAsStreamAsync());
        if (response == null)
            throw new Exception("Failed to deserialise register response");

        if (response[0].Error != null) throw new Exception($"Hue registration error: {response[0].Error.Description}");

        if (response[0].Success != null) return response[0].Success.Username;

        throw new Exception("Response was null. This should never happen");
    }

    private async ValueTask<string?> DiscoverBridge()
    {
        _logger.LogInformation("Getting bridges from Hue Discovery API (requires internet access for app and bridge)");
        var bridges = await new HttpClient().GetFromJsonAsync<List<HueBridge>>("https://discovery.meethue.com/");

        if (bridges != null) return bridges.First().InternalIpAddress;
        Console.WriteLine("No bridges found");
        return null;
    }

    private static bool IsWebCamInUse()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam\NonPackaged");
        if (key == null) return false;
        foreach (var subKeyName in key.GetSubKeyNames())
        {
            using var subKey = key.OpenSubKey(subKeyName);
            if (!subKey.GetValueNames().Contains("LastUsedTimeStop")) continue;
            var endTime = subKey.GetValue("LastUsedTimeStop") is long
                ? (long)subKey.GetValue("LastUsedTimeStop")
                : -1;
            if (endTime <= 0) return true;
        }

        return false;
    }
}