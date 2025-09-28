using PrimeBakes.Shared.Services;

namespace PrimeBakes.Services;

public class VibrationService : IVibrationService
{
	public void VibrateHapticClick() =>
		HapticFeedback.Default.Perform(HapticFeedbackType.Click);

	public void VibrateHapticLongPress() =>
		HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);

	public void VibrateWithTime(int milliseconds)
	{
		if (Vibration.Default.IsSupported)
			Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(milliseconds));
	}
}