using System;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace DropBoard.Native;

public partial class App : Application
{
    public static string? StartupFilePath { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Ensure entire UI, calendars, DatePicker, and date formatting run in clean English
        var enCulture = CultureInfo.GetCultureInfo("en-US");
        Thread.CurrentThread.CurrentCulture = enCulture;
        Thread.CurrentThread.CurrentUICulture = enCulture;
        CultureInfo.DefaultThreadCurrentCulture = enCulture;
        CultureInfo.DefaultThreadCurrentUICulture = enCulture;
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("en-US")));

        if (e.Args.Length > 0 && !string.IsNullOrWhiteSpace(e.Args[0]))
        {
            StartupFilePath = e.Args[0];
        }

        try
        {
            var win = new MainWindow(StartupFilePath);
            win.Show();
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText("app_error.txt", ex.ToString());
            MessageBox.Show($"Startup Error:\n{ex}", "DropBoard Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

