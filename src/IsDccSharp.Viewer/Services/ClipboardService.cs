namespace IsDccSharp.Viewer.Services;

using System.Windows;

internal sealed class ClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        Clipboard.SetText(text);
    }
}
