using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

using Firebase.Messaging;

using PrimeBakes.Services;

namespace PrimeBakes;

[Activity(
Theme = "@style/Maui.SplashTheme",
MainLauncher = true,
LaunchMode = LaunchMode.SingleTop,
ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity, Android.Gms.Tasks.IOnSuccessListener
{
    IPushDemoNotificationActionService _notificationActionService;
    IDeviceInstallationService _deviceInstallationService;

    IPushDemoNotificationActionService NotificationActionService => _notificationActionService ??= IPlatformApplication.Current.Services.GetService<IPushDemoNotificationActionService>();

    IDeviceInstallationService DeviceInstallationService => _deviceInstallationService ??= IPlatformApplication.Current.Services.GetService<IDeviceInstallationService>();

    public void OnSuccess(Java.Lang.Object result)
    {
        DeviceInstallationService.Token = result.ToString();
    }

    void ProcessNotificationsAction(Intent intent)
    {
        try
        {
            if (intent?.HasExtra("action") == true)
            {
                var action = intent.GetStringExtra("action");

                if (!string.IsNullOrEmpty(action))
                    NotificationActionService.TriggerAction(action);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        ProcessNotificationsAction(intent);
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (DeviceInstallationService.NotificationsSupported)
            FirebaseMessaging.Instance.GetToken().AddOnSuccessListener(this);

        ProcessNotificationsAction(Intent);
    }
}
