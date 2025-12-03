#if DEBUG
using Microsoft.Extensions.Logging;
#endif

using PrimeBakes.Services;
using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.DataAccess;

using Syncfusion.Blazor;

using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace PrimeBakes;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Secrets.SyncfusionLicense);

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .RegisterServices()
            .RegisterViews()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Add device-specific services used by the PrimeBakes.Shared project
        builder.Services.AddSingleton<IFormFactor, FormFactor>();
        builder.Services.AddSingleton<ISaveAndViewService, SaveAndViewService>();
        builder.Services.AddSingleton<IUpdateService, UpdateService>();
        builder.Services.AddSingleton<IDataStorageService, DataStorageService>();
        builder.Services.AddSingleton<IVibrationService, VibrationService>();
        builder.Services.AddSingleton<ISoundService, SoundService>();
        builder.Services.AddScoped<INotificationService, NotificationService>();

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSyncfusionBlazor();
		builder.Services.AddHotKeys2();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    public static MauiAppBuilder RegisterServices(this MauiAppBuilder builder)
    {
#if ANDROID
        builder.Services.AddSingleton<IDeviceInstallationService, Platforms.Android.DeviceInstallationService>();
#endif

        builder.Services.AddSingleton<IPushDemoNotificationActionService, PushDemoNotificationActionService>();
        builder.Services.AddSingleton<INotificationRegistrationService>(new NotificationRegistrationService(Config.BackendServiceEndpoint, Config.ApiKey));

        return builder;
    }

    public static MauiAppBuilder RegisterViews(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<MainPage>();
        return builder;
    }
}
