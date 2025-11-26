using PrimeBakesLibrary.Models.Notification;

namespace PrimeBakes.Services;

public interface IPushDemoNotificationActionService : INotificationActionService
{
    event EventHandler<PushDemoAction> ActionTriggered;
}