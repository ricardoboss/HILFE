namespace StepLang.LSP.Messages;

public record InitializeResult(
    ServerCapabilities? capabilities,
    ServerInfo? serverInfo
);
