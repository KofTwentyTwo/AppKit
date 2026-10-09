using System.Runtime.InteropServices;
using KofTwentyTwo.AppKit.Logging;
using KofTwentyTwo.AppKit.Settings;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace KofTwentyTwo.AppKit.WinUI;

/// <summary>App-level wiring every WinUI app repeats: crash net and theme.</summary>
public static class WinUIShell
{
    /// <summary>
    /// Last-resort net: an exception escaping an async void event handler is rethrown
    /// on the dispatcher with no frame above it, and the process dies as a stowed
    /// exception (0xc000027b) with nothing in the log. This records it and keeps the app
    /// running. Call from the App constructor.
    /// </summary>
    /// <param name="application">The app.</param>
    /// <param name="log">Supplies the log; may return null early in startup.</param>
    public static void InstallCrashNet(Application application, Func<IActivityLog?> log)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(log);
        application.UnhandledException += (_, e) =>
        {
            e.Handled = true;
            try
            {
                log()?.Error($"Unhandled UI exception: {e.Message}", e.Exception);
            }
            catch
            {
                // Logging must never turn a survivable error fatal; the stowed
                // Exception property itself can throw when type info was lost.
            }
        };
    }

    /// <summary>
    /// Applies the startup theme. Only valid in the App constructor, before any window
    /// exists; for live changes use <see cref="ApplyTheme(FrameworkElement, AppTheme)"/>.
    /// </summary>
    public static void ApplyTheme(Application application, AppTheme theme)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (theme == AppTheme.Light)
        {
            application.RequestedTheme = ApplicationTheme.Light;
        }
        else if (theme == AppTheme.Dark)
        {
            application.RequestedTheme = ApplicationTheme.Dark;
        }
    }

    /// <summary>Applies a theme change to a window's root element while the app runs.</summary>
    public static void ApplyTheme(FrameworkElement root, AppTheme theme)
    {
        ArgumentNullException.ThrowIfNull(root);
        root.RequestedTheme = theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
    }
}

/// <summary>DPI-aware sizing and branding for WinUI windows.</summary>
public static partial class WindowExtensions
{
    /// <summary>The window's display scale (1.0 at 100%, 1.5 at 150%).</summary>
    public static double DpiScale(this Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        uint dpi = GetDpiForWindow(hwnd);
        return dpi == 0 ? 1.0 : dpi / 96.0;
    }

    /// <summary>
    /// Resizes to <paramref name="width"/> x <paramref name="height"/> effective pixels.
    /// AppWindow.Resize takes physical pixels, so this scales by the monitor DPI.
    /// </summary>
    public static void ResizeForDpi(this Window window, int width, int height)
    {
        double scale = window.DpiScale();
        window.AppWindow.Resize(new Windows.Graphics.SizeInt32((int)(width * scale), (int)(height * scale)));
    }

    /// <summary>Keeps the window from shrinking below a usable size, in effective pixels.</summary>
    public static void SetMinimumSize(this Window window, int width, int height)
    {
        double scale = window.DpiScale();
        if (window.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = (int)(width * scale);
            presenter.PreferredMinimumHeight = (int)(height * scale);
        }
    }

    /// <summary>
    /// Sets the brand icon on the title bar (and taskbar / Alt-Tab for unpackaged runs).
    /// The .ico must be copied to the output directory. A missing file is ignored.
    /// </summary>
    public static void SetAppIcon(this Window window, AppInfo app)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(app);
        string path = Path.Combine(AppContext.BaseDirectory, app.Brand.IconIcoPath);
        if (File.Exists(path))
        {
            window.AppWindow.SetIcon(path);
        }
    }

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(IntPtr hwnd);
}
