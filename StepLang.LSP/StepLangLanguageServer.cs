using StepLang.LSP.Messages;
using StreamJsonRpc;

namespace StepLang.LSP;

public class StepLangLanguageServer : IAsyncDisposable
{
    private readonly CancellationTokenRegistration stoppingTokenRegistration;

    private bool isInitialized;
    private bool isShuttingDown;

    public event EventHandler<EventArgs>? OnExit;

    public StepLangLanguageServer(CancellationToken stoppingToken)
    {
        stoppingTokenRegistration = stoppingToken.Register(OnInternalShutdown);
    }

    private void CheckState()
    {
        if (!isInitialized)
            throw new InvalidOperationException("Language server not initialized");

        if (isShuttingDown)
            throw new InvalidOperationException("Language server is shutting down");
    }

    public async Task<InitializeResult> InitializeAsync(
        int? processId,
        ClientInfo? clientInfo,
        string? locale,
        string? rootPath,
        Uri? rootUri,
        ClientCapabilities? capabilities,
        string? trace,
        WorkspaceFolder []? workspaceFolders,
        CancellationToken cancellationToken = default
    )
    {
        await Task.Delay(1000, cancellationToken);

        return new InitializeResult(
            new ServerCapabilities(),
            new ServerInfo("STEP Language Server", GitVersionInformation.FullSemVer)
        );
    }

    public async Task<InitializeResult> InitializeAsync(
        int? processId,
        ClientInfo? clientInfo,
        string? rootPath,
        Uri? rootUri,
        ClientCapabilities? capabilities,
        string? trace,
        WorkspaceFolder []? workspaceFolders,
        CancellationToken cancellationToken = default
    ) => await InitializeAsync(processId, clientInfo, locale: null, rootPath, rootUri, capabilities, trace,
        workspaceFolders, cancellationToken);

    public void Initialized()
    {
        isInitialized = true;
    }

    [JsonRpcMethod("$/setTrace")]
    public void SetTrace(string value)
    {
    }

    [JsonRpcMethod("workspace/didChangeConfiguration")]
    public void WorkspaceDidChangeConfiguration(object? settings)
    {
    }

    [JsonRpcMethod("textDocument/didOpen")]
    public void TextDocumentDidOpen(TextDocumentItem textDocument)
    {
    }

    public void Shutdown()
    {
        CheckState();

        isShuttingDown = true;
    }

    private void OnInternalShutdown()
    {
        isShuttingDown = true;

        // TODO: notify client

        Exit();
    }

    public void Exit()
    {
        if (!isShuttingDown)
            throw new InvalidOperationException("Language server not shutting down");

        OnExit?.Invoke(this, EventArgs.Empty);
    }

    public async ValueTask DisposeAsync()
    {
        await stoppingTokenRegistration.DisposeAsync();
    }
}