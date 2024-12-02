namespace StepLang.LSP.Messages;

public record TextDocumentItem(
    Uri Uri,
    string LanguageId,
    int Version,
    string Text
);