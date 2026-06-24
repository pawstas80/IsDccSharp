namespace IsDccSharp.Viewer.Services;

internal interface IFileDialogService
{
    string? OpenInxFile();
    string? SaveTextFile(string defaultFileName);
}
