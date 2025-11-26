using Plugin.Maui.Audio;

using PrimeBakes.Shared.Services;

namespace PrimeBakes.Services;

public class SoundService : ISoundService
{
    public async Task PlaySound(string soundFileName) =>
        AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync(soundFileName)).Play();
}
