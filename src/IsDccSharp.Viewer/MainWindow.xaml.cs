namespace IsDccSharp.Viewer;

using System.Windows;
using IsDccSharp.Core;
using IsDccSharp.Viewer.Services;
using IsDccSharp.Viewer.ViewModels;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(
            new InxDecoder(),
            new FileDialogService(),
            new ClipboardService(),
            new MessageService(),
            App.Logger);
        App.Logger.Info("Main window initialized.");
    }
}
