namespace IsDccSharp.Viewer.ViewModels;

using IsDccSharp.Core;
using IsDccSharp.Viewer.Infrastructure;
using IsDccSharp.Viewer.Services;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

internal sealed class MainWindowViewModel : ObservableObject
{
    private readonly InxDecoder decoder;
    private readonly IFileDialogService fileDialogService;
    private readonly IClipboardService clipboardService;
    private readonly IMessageService messageService;
    private readonly IAppLogger logger;
    private readonly AsyncRelayCommand openCommand;
    private readonly AsyncRelayCommand decodeCommand;
    private readonly RelayCommand saveCommand;
    private readonly RelayCommand copyCommand;
    private readonly RelayCommand aboutCommand;
    private string filePath = string.Empty;
    private string outputText = string.Empty;
    private string statusText = "Ready.";
    private bool isBusy;
    private bool includeStringUserVariables;
    private bool includeNumberUserVariables;
    private bool includeDataTypes;
    private bool includeFunctionPrototypes;
    private bool includeFunctionDefinitions = true;

    public MainWindowViewModel(
        InxDecoder decoder,
        IFileDialogService fileDialogService,
        IClipboardService clipboardService,
        IMessageService messageService,
        IAppLogger logger)
    {
        this.decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
        this.fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        this.clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        openCommand = new AsyncRelayCommand(OpenAsync, CanRunFileCommand);
        decodeCommand = new AsyncRelayCommand(DecodeAsync, CanDecode);
        saveCommand = new RelayCommand(Save, CanUseOutput);
        copyCommand = new RelayCommand(Copy, CanUseOutput);
        aboutCommand = new RelayCommand(ShowAbout, () => !IsBusy);
    }

    public ICommand OpenCommand => openCommand;
    public ICommand DecodeCommand => decodeCommand;
    public ICommand SaveCommand => saveCommand;
    public ICommand CopyCommand => copyCommand;
    public ICommand AboutCommand => aboutCommand;

    public string FilePath
    {
        get => filePath;
        private set
        {
            if (SetProperty(ref filePath, value))
                RefreshCommandStates();
        }
    }

    public string OutputText
    {
        get => outputText;
        private set
        {
            if (SetProperty(ref outputText, value))
                RefreshCommandStates();
        }
    }

    public string StatusText
    {
        get => statusText;
        private set => SetProperty(ref statusText, value);
    }

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                OnPropertyChanged(nameof(OptionsEnabled));
                RefreshCommandStates();
            }
        }
    }

    public bool OptionsEnabled => !IsBusy;

    public bool IncludeStringUserVariables
    {
        get => includeStringUserVariables;
        set
        {
            if (SetProperty(ref includeStringUserVariables, value))
                MarkOutputOptionsChanged();
        }
    }

    public bool IncludeNumberUserVariables
    {
        get => includeNumberUserVariables;
        set
        {
            if (SetProperty(ref includeNumberUserVariables, value))
                MarkOutputOptionsChanged();
        }
    }

    public bool IncludeDataTypes
    {
        get => includeDataTypes;
        set
        {
            if (SetProperty(ref includeDataTypes, value))
                MarkOutputOptionsChanged();
        }
    }

    public bool IncludeFunctionPrototypes
    {
        get => includeFunctionPrototypes;
        set
        {
            if (SetProperty(ref includeFunctionPrototypes, value))
                MarkOutputOptionsChanged();
        }
    }

    public bool IncludeFunctionDefinitions
    {
        get => includeFunctionDefinitions;
        set
        {
            if (SetProperty(ref includeFunctionDefinitions, value))
                MarkOutputOptionsChanged();
        }
    }

    private Task OpenAsync()
    {
        var selectedFile = fileDialogService.OpenInxFile() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(selectedFile))
            return Task.CompletedTask;

        FilePath = selectedFile;
        OutputText = string.Empty;
        StatusText = "Selected: " + selectedFile;
        logger.Info("Selected file: " + selectedFile);
        return Task.CompletedTask;
    }

    private async Task DecodeAsync()
    {
        await DecodeCurrentFileAsync().ConfigureAwait(true);
    }

    private void Save()
    {
        if (string.IsNullOrEmpty(OutputText))
            return;

        var outputFile = fileDialogService.SaveTextFile(BuildDefaultOutputFileName());
        if (string.IsNullOrWhiteSpace(outputFile))
            return;

        try
        {
            File.WriteAllText(outputFile, OutputText, Encoding.ASCII);
            StatusText = "Saved: " + outputFile;
            logger.Info("Saved decoded output: " + outputFile);
        }
        catch (Exception ex)
        {
            StatusText = "Save failed.";
            logger.Exception("Save failed: " + outputFile, ex);
            messageService.ShowError("Save failed", BuildErrorMessage(ex));
        }
    }

    private void Copy()
    {
        if (string.IsNullOrEmpty(OutputText))
            return;

        try
        {
            clipboardService.SetText(OutputText);
            StatusText = "Copied output to clipboard.";
            logger.Info("Copied decoded output to clipboard.");
        }
        catch (Exception ex)
        {
            StatusText = "Copy failed.";
            logger.Exception("Copy failed", ex);
            messageService.ShowError("Copy failed", BuildErrorMessage(ex));
        }
    }

    private void ShowAbout()
    {
        messageService.ShowInformation(
            "About IsDccSharp",
            string.Join(Environment.NewLine, ProjectInfo.GetConsoleHeaderLines()));
    }

    private async Task DecodeCurrentFileAsync()
    {
        var inputPath = FilePath;
        if (string.IsNullOrWhiteSpace(inputPath))
            return;

        IsBusy = true;

        try
        {
            StatusText = "Decoding...";
            var options = BuildDecodeOptions();
            logger.Info("Decode requested: " + inputPath);
            logger.Info("Decode options: " + FormatDecodeOptions(options));

            DecodeResult result;
            using (logger.Time("Decode " + Path.GetFileName(inputPath)))
                result = await decoder.DecodeFileAsync(inputPath, options).ConfigureAwait(true);

            OutputText = ProjectInfo.AddFileHeader(result.Text);

            var status = $"Decoded {Path.GetFileName(inputPath)} ({result.InputLength:N0} bytes)";
            if (result.WasUnscrambled)
                status += " | unscrambled";
            if (result.Warnings.Count > 0)
                status += $" | warnings: {result.Warnings.Count}";

            StatusText = status;
            logger.Info(
                $"Decode completed: {inputPath}; input={FormatLogNumber(result.InputLength)} bytes; output={FormatLogNumber(result.Text.Length)} chars; unscrambled={result.WasUnscrambled}; warnings={result.Warnings.Count}");

            if (result.Warnings.Count > 0)
            {
                foreach (var warning in result.Warnings)
                    logger.Warn(warning);

                messageService.ShowWarning("Decode warnings", string.Join(Environment.NewLine, result.Warnings));
            }
        }
        catch (Exception ex)
        {
            OutputText = string.Empty;
            StatusText = "Decode failed.";
            logger.Exception("Decode failed: " + inputPath, ex);
            messageService.ShowError("Decode failed", BuildErrorMessage(ex));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string BuildDefaultOutputFileName()
    {
        return string.IsNullOrWhiteSpace(FilePath)
            ? "decoded.txt"
            : Path.GetFileNameWithoutExtension(FilePath) + ".txt";
    }

    private DecodeOptions BuildDecodeOptions()
    {
        return new DecodeOptions
        {
            IncludeStringUserVariables = IncludeStringUserVariables,
            IncludeNumberUserVariables = IncludeNumberUserVariables,
            IncludeDataTypes = IncludeDataTypes,
            IncludeFunctionPrototypes = IncludeFunctionPrototypes,
            IncludeFunctionDefinitions = IncludeFunctionDefinitions
        };
    }

    private static string FormatDecodeOptions(DecodeOptions options)
    {
        return
            $"AutoUnscramble={options.AutoUnscramble}, " +
            $"StringUserVars={options.IncludeStringUserVariables}, " +
            $"NumberUserVars={options.IncludeNumberUserVariables}, " +
            $"DataTypes={options.IncludeDataTypes}, " +
            $"FunctionPrototypes={options.IncludeFunctionPrototypes}, " +
            $"FunctionDefinitions={options.IncludeFunctionDefinitions}";
    }

    private static string FormatLogNumber(long value)
    {
        return value.ToString("N0", CultureInfo.InvariantCulture);
    }

    private string BuildErrorMessage(Exception ex)
    {
        return ex.Message +
            Environment.NewLine +
            Environment.NewLine +
            "Log file:" +
            Environment.NewLine +
            logger.CurrentLogFile;
    }

    private void MarkOutputOptionsChanged()
    {
        if (!string.IsNullOrEmpty(OutputText))
            StatusText = "Output options changed.";
    }

    private bool CanRunFileCommand() => !IsBusy;

    private bool CanDecode() => !IsBusy && !string.IsNullOrWhiteSpace(FilePath);

    private bool CanUseOutput() => !IsBusy && !string.IsNullOrEmpty(OutputText);

    private void RefreshCommandStates()
    {
        openCommand.RaiseCanExecuteChanged();
        decodeCommand.RaiseCanExecuteChanged();
        saveCommand.RaiseCanExecuteChanged();
        copyCommand.RaiseCanExecuteChanged();
        aboutCommand.RaiseCanExecuteChanged();
    }
}
