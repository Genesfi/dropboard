using System;
using System.Configuration;
using System.Data;
using System.Windows;

namespace DropBoard.Native;

public partial class App : Application
{
    public static string? StartupFilePath { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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

