#if DEBUG
using Microsoft.Extensions.Logging;
#endif

#if ANDROID
using Plugin.LocalNotification;
#endif

using PrimeOrdersLibrary.DataAccess;

using Syncfusion.Maui.Core.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;

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
			.ConfigureSyncfusionToolkit()
			.ConfigureSyncfusionCore()
#if ANDROID
			.UseLocalNotification()
#endif
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("CascadiaCode-Regular.ttf", "CascadiaCodeRegular");
				fonts.AddFont("CascadiaCode-SemiBold.ttf", "CascadiaCodeSemibold");
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
