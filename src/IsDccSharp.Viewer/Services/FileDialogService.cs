namespace IsDccSharp.Viewer.Services;

using Microsoft.Win32;

internal sealed class FileDialogService : IFileDialogService
{
    public string? OpenInxFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open INX file",
            Filter = "InstallShield INX files (*.inx)|*.inx|All files (*.*)|*.*"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SaveTextFile(string defaultFileName)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save decoded output",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = defaultFileName
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
