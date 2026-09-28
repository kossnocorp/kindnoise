using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using NVorbis;
using NAudio;
using NAudio.Vorbis;
using H.NotifyIcon;
using static System.Net.WebRequestMethods;
using System.Drawing;
using Microsoft.UI.Xaml.Markup;
using NAudio.Wave;
using H.NotifyIcon.Core;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KindNoise
{
    enum Sound
    {
        Ocean,
        Rain,
        Forest
    }

    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public const string TickIcon =
            "M470.6 105.4c12.5 12.5 12.5 32.8 0 45.3l-256 256c-12.5 12.5-32.8 12.5-45.3 0l-128-128c-12.5-12.5-12.5-32.8 0-45.3s32.8-12.5 45.3 0L192 338.7 425.4 105.4c12.5-12.5 32.8-12.5 45.3 0z";

        #region Properties

        public static TaskbarIcon? TrayIcon { get; private set; }
        public static LaunchWindow? Window { get; set; }

        NAudio.Wave.WaveOut player = new NAudio.Wave.WaveOut();

        NAudio.Vorbis.VorbisWaveReader rainSound;
        NAudio.Vorbis.VorbisWaveReader oceanSound;
        NAudio.Vorbis.VorbisWaveReader forestSound;

        Sound currentSound;

        ApplicationDataContainer settings = Windows.Storage.ApplicationData.Current.LocalSettings;

        #endregion


        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            rainSound = new NAudio.Vorbis.VorbisWaveReader(AssetPath("Assets/rain.ogg"));
            oceanSound = new NAudio.Vorbis.VorbisWaveReader(AssetPath("Assets/ocean.ogg"));
            forestSound = new NAudio.Vorbis.VorbisWaveReader(AssetPath("Assets/forest.ogg"));

            if (!Enum.TryParse<Sound>((string)settings.Values["currentSound"], out currentSound))
                currentSound = Sound.Rain;

            LoadSound();

            InitializeTrayIcon();

            await UpdateStartupState();

            bool showLaunch;
            if (
                !bool.TryParse((string)settings.Values["showLaunchWindow"], out showLaunch)
                || showLaunch
            )
                ShowLaunchWindow();
        }

        private void LoadSound()
        {
            LoopStream loop = new LoopStream(CurrentSoundReader());
            player = new WaveOut();
            player.Init(loop);
        }

        #region Event Handlers

        private void InitializeTrayIcon()
        {
            var togglePlayCommand = (XamlUICommand)Resources["TogglePlayCommand"];
            togglePlayCommand.ExecuteRequested += TogglePlay;

            SelectCurrentSoundCommand();

            ((XamlUICommand)Resources["RainCommand"]).ExecuteRequested += (_, _) =>
                SwitchSound(Sound.Rain);

            ((XamlUICommand)Resources["OceanCommand"]).ExecuteRequested += (_, _) =>
                SwitchSound(Sound.Ocean);

            ((XamlUICommand)Resources["ForestCommand"]).ExecuteRequested += (_, _) =>
                SwitchSound(Sound.Forest);

            ((XamlUICommand)Resources["LaunchWindowCommand"]).ExecuteRequested += (_, _) =>
                ShowLaunchWindow();

            ((XamlUICommand)Resources["ExitApplicationCommand"]).ExecuteRequested +=
                ExitApplicationCommand_ExecuteRequested;

            ((XamlUICommand)Resources["AutoStartCommand"]).ExecuteRequested += async (_, _) =>
                await ToggleLaunchOnStartup();

            TrayIcon = (TaskbarIcon)Resources["TrayIcon"];
            SetPausedIcon();
            TrayIcon.ForceCreate();
        }

        private async Task UpdateStartupState()
        {
            var startup = await StartupTask.GetAsync("LaunchOnStartupTaskId");
            ((XamlUICommand)Resources["AutoStartCommand"]).IconSource = new PathIconSource
            {
                Data = startup.State == StartupTaskState.Enabled ? GeometryPath(TickIcon) : null
            };
        }

        private async Task ToggleLaunchOnStartup()
        {
            var startup = await StartupTask.GetAsync("LaunchOnStartupTaskId");
            switch (startup.State)
            {
                case StartupTaskState.Enabled:
                    startup.Disable();
                    await UpdateStartupState();
                    break;

                case StartupTaskState.Disabled:
                    var updatedState = await startup.RequestEnableAsync();
                    await UpdateStartupState();
                    break;

                case StartupTaskState.DisabledByUser:
                    await new MessageDialog(
                        "Unable to change state of startup task via the application - enable via Startup tab on Task Manager (Ctrl+Shift+Esc)"
                    ).ShowAsync();
                    break;

                default:
                    await new MessageDialog("Unable to change state of startup task").ShowAsync();
                    break;
            }
        }

        private void SwitchSound(Sound sound)
        {
            var playing = player.PlaybackState == PlaybackState.Playing;
            if (playing)
                player.Stop();

            CurrentSoundCommand().IconSource = null;

            currentSound = sound;

            LoadSound();
            if (playing)
                player.Play();

            SelectCurrentSoundCommand();

            settings.Values["currentSound"] = sound.ToString();
        }

        private void SelectCurrentSoundCommand()
        {
            CurrentSoundCommand().IconSource = new PathIconSource { Data = GeometryPath(TickIcon) };
        }

        private void ShowLaunchWindow()
        {
            if (Window == null)
            {
                Window = new LaunchWindow();
                Window.Show();
                Window.Closed += (_, _) => Window = null;
            }
            else
            {
                Window.Show();
            }
        }

        private void ExitApplicationCommand_ExecuteRequested(
            object? _,
            ExecuteRequestedEventArgs args
        )
        {
            TrayIcon?.Dispose();
            Window?.Close();
        }

        #endregion

        private void TogglePlay(object? _, ExecuteRequestedEventArgs args)
        {
            if (player.PlaybackState == PlaybackState.Playing)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }

        private void Play()
        {
            player.Play();

            var command = (XamlUICommand)Resources["TogglePlayCommand"];
            command.Label = "Pause";
            command.IconSource = new PathIconSource
            {
                Data = GeometryPath(
                    "M48 64C21.5 64 0 85.5 0 112V400c0 26.5 21.5 48 48 48H80c26.5 0 48-21.5 48-48V112c0-26.5-21.5-48-48-48H48zm192 0c-26.5 0-48 21.5-48 48V400c0 26.5 21.5 48 48 48h32c26.5 0 48-21.5 48-48V112c0-26.5-21.5-48-48-48H240z"
                )
            };

            SetPlayingIcon();
        }

        private void Pause()
        {
            player.Stop();

            var command = (XamlUICommand)Resources["TogglePlayCommand"];
            command.Label = "Play";
            command.IconSource = new PathIconSource
            {
                Data = GeometryPath(
                    "M73 39c-14.8-9.1-33.4-9.4-48.5-.9S0 62.6 0 80V432c0 17.4 9.4 33.4 24.5 41.9s33.7 8.1 48.5-.9L361 297c14.3-8.7 23-24.2 23-41s-8.7-32.2-23-41L73 39z"
                )
            };

            SetPausedIcon();
        }

        void SetPlayingIcon()
        {
            TrayIcon.IconSource = new BitmapImage(new Uri("ms-appx:///Assets/playing.ico"));
        }

        void SetPausedIcon()
        {
            TrayIcon.IconSource = new BitmapImage(new Uri("ms-appx:///Assets/paused.ico"));
        }

        private NAudio.Vorbis.VorbisWaveReader CurrentSoundReader()
        {
            switch (currentSound)
            {
                case Sound.Rain:
                    return rainSound;
                case Sound.Ocean:
                    return oceanSound;
                case Sound.Forest:
                    return forestSound;

                default:
                    return rainSound;
            }
        }

        private XamlUICommand CurrentSoundCommand()
        {
            switch (currentSound)
            {
                case Sound.Rain:
                    return (XamlUICommand)Resources["RainCommand"];
                case Sound.Ocean:
                    return (XamlUICommand)Resources["OceanCommand"];
                case Sound.Forest:
                    return (XamlUICommand)Resources["ForestCommand"];

                default:
                    return (XamlUICommand)Resources["RainCommand"];
            }
        }

        private String AssetPath(String path)
        {
            return System.IO.Path.Combine(
                Windows.ApplicationModel.Package.Current.InstalledPath,
                //Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                path
            );
        }

        private Geometry GeometryPath(String path)
        {
            return (Geometry)
                XamlReader.Load(
                    "<Geometry xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>"
                        + path
                        + "</Geometry>"
                );
        }
    }

    // Source: https://markheath.net/post/looped-playback-in-net-with-naudio

    /// <summary>
    /// Stream for looping playback
    /// </summary>
    public class LoopStream : WaveStream
    {
        WaveStream sourceStream;

        /// <summary>
        /// Creates a new Loop stream
        /// </summary>
        /// <param name="sourceStream">The stream to read from. Note: the Read method of this stream should return 0 when it reaches the end
        /// or else we will not loop to the start again.</param>
        public LoopStream(WaveStream sourceStream)
        {
            this.sourceStream = sourceStream;
            this.EnableLooping = true;
        }

        /// <summary>
        /// Use this to turn looping on or off
        /// </summary>
        public bool EnableLooping { get; set; }

        /// <summary>
        /// Return source stream's wave format
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get { return sourceStream.WaveFormat; }
        }

        /// <summary>
        /// LoopStream simply returns
        /// </summary>
        public override long Length
        {
            get { return sourceStream.Length; }
        }

        /// <summary>
        /// LoopStream simply passes on positioning to source stream
        /// </summary>
        public override long Position
        {
            get { return sourceStream.Position; }
            set { sourceStream.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int bytesRead = sourceStream.Read(
                    buffer,
                    offset + totalBytesRead,
                    count - totalBytesRead
                );
                if (bytesRead == 0)
                {
                    if (sourceStream.Position == 0 || !EnableLooping)
                    {
                        // something wrong with the source stream
                        break;
                    }
                    // loop
                    sourceStream.Position = 0;
                }
                totalBytesRead += bytesRead;
            }
            return totalBytesRead;
        }
    }
}
