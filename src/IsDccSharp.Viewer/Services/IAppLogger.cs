namespace IsDccSharp.Viewer.Services;

using System;

internal interface IAppLogger
{
    string CurrentLogFile { get; }
    string LogDirectory { get; }

    void Info(string message);
    void Warn(string message);
    void Error(string message);
    void Exception(string context, Exception exception);
    IDisposable Time(string operationName);
}
