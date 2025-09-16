#if DEBUG
using Microsoft.Extensions.Logging;
#endif

using PrimeBakes.Services;

using PrimeOrdersLibrary.DataAccess;

using Syncfusion.Blazor;

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
				fonts.AddFont("CascadiaCode-Regular.ttf", "CascadiaCodeRegular");
				fonts.AddFont("CascadiaCode-SemiBold.ttf", "CascadiaCodeSemibold");
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddMauiBlazorWebView();
		builder.Services.AddSyncfusionBlazor();

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
