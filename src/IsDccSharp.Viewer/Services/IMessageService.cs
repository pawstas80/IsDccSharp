namespace IsDccSharp.Viewer.Services;

internal interface IMessageService
{
    void ShowInformation(string title, string message);
    void ShowWarning(string title, string message);
    void ShowError(string title, string message);
}
