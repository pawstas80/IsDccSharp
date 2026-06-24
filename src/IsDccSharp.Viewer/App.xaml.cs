namespace IsDccSharp.Viewer;

using System.Windows;
using System.Windows.Threading;
using IsDccSharp.Core;
using IsDccSharp.Viewer.Services;

public partial class App : Application
{
    internal static SmartLogger Logger { get; } = new(ProjectInfo.AppName);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Logger.HookExceptionHandlers();
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        Logger.Info("Viewer startup.");
        Logger.Info("Log file: " + Logger.CurrentLogFile);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Info("Viewer exit.");
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        Logger.UnhookExceptionHandlers();
        Logger.Dispose();

        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.Exception("Unhandled WPF dispatcher exception", e.Exception);
        e.Handled = true;

        MessageBox.Show(
            "Unexpected error. Details were written to the log file:" +
            System.Environment.NewLine +
            Logger.CurrentLogFile,
            ProjectInfo.AppName,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
