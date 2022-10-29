using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.WindowManagement;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KindNoise
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LaunchWindow : Window
    {
        ApplicationDataContainer settings = Windows.Storage.ApplicationData.Current.LocalSettings;

        public LaunchWindow()
        {
            this.InitializeComponent();

            this.ExtendsContentIntoTitleBar = true;

            var handle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
            var window = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            var presenter = window.Presenter as OverlappedPresenter;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;

            window.Resize(new Windows.Graphics.SizeInt32(826, 358));

            bool showLaunch = true;
            var parsed = bool.TryParse((string)settings.Values["showLaunchWindow"], out showLaunch);
            launchCheckbox.IsChecked = !parsed || showLaunch;

            launchCheckbox.Checked += (_, _) =>
                settings.Values["showLaunchWindow"] = true.ToString();
            launchCheckbox.Unchecked += (_, _) =>
                settings.Values["showLaunchWindow"] = false.ToString();
            ;
        }
    }
}
