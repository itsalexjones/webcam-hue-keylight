using HueWebcamSceneSwitcher;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) => { services.AddHostedService<Worker>(); })
    .UseWindowsService(options => { options.ServiceName = "Hue Webcam Key Light Service"; })
    .Build();

await host.RunAsync();