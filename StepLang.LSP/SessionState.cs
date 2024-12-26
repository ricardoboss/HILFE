using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using StepLang.LSP.Diagnostics;
using StepLang.Parsing;
using StepLang.Tokenizing;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace StepLang.LSP;

public class SessionState(ILogger<SessionState> logger, DiagnosticsRunner diagnosticsRunner, ILanguageServerFacade server)
{
	private readonly ConcurrentDictionary<Uri, DocumentState> documents = new();
	public IReadOnlyDictionary<Uri, DocumentState> Documents => documents;

	private readonly ConcurrentDictionary<string, DeclaredVariableInfo> symbols = new();
	public IReadOnlyDictionary<string, DeclaredVariableInfo> Symbols => symbols;

	public ConcurrentDictionary<Uri, ObservableCollection<Diagnostic>> Diagnostics { get; } = new();

	public void AddDocument(DocumentState documentState)
	{
		logger.LogDebug("Adding document {DocumentUri} to session", documentState.DocumentUri);

		if (!documents.TryAdd(documentState.DocumentUri, documentState))
		{
			throw new InvalidOperationException("Document already exists");
		}

		var collection = Diagnostics.AddOrUpdate(documentState.DocumentUri, [], (_, _) => []);
		collection.CollectionChanged += (_, _) => SendDiagnostics(documentState.DocumentUri);

		RecalculateDocument(documentState);

		RecalculateSymbols();

		QueueAnalysis(documentState);
	}

	public void UpdateDocument(Uri documentUri, int version, string text)
	{
		logger.LogDebug("Updating document {DocumentUri}", documentUri);

		if (!documents.Remove(documentUri, out _))
		{
			logger.LogWarning("Document {DocumentUri} was not found in session; nothing to update", documentUri);

			return;
		}

		var collection = Diagnostics.AddOrUpdate(documentUri, [], (_, _) => []);
		collection.CollectionChanged += (_, _) => SendDiagnostics(documentUri);

		var newDocumentState = new DocumentState
		{
			DocumentUri = documentUri, Version = version, Text = text,
		};

		documents[documentUri] = newDocumentState;

		RecalculateDocument(newDocumentState);

		RecalculateSymbols();

		QueueAnalysis(newDocumentState);
	}

	private void SendDiagnostics(Uri documentUri)
	{
		logger.LogTrace("Sending diagnostics for document {DocumentUri}", documentUri);

		server.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
		{
			Uri = documentUri,
			Diagnostics = Diagnostics[documentUri],
			Version = Documents[documentUri].Version,
		});
	}

	private void RecalculateDocument(DocumentState documentState)
	{
		logger.LogTrace("Recalculating document {Document}", documentState);

		var tokens = Tokenize(documentState);
		var ast = ParseAst(documentState, tokens);
		var declarations = ScanDeclarations(documentState, ast);

		documentState.Tokens = tokens;
		documentState.Ast = ast;
		documentState.Symbols = declarations;
	}

	private void QueueAnalysis(DocumentState documentState)
	{
		logger.LogTrace("Queuing analysis for document {Document}", documentState);

		diagnosticsRunner.Dispatch(this, documentState);
	}

	private TokenCollection Tokenize(DocumentState documentState)
	{
		logger.LogTrace("Tokenizing document {Document}", documentState);

		var tokens = new Tokenizer(documentState.Text).Tokenize().ToArray();

		return new TokenCollection(tokens);
	}

	private RootNode ParseAst(DocumentState documentState, TokenCollection tokens)
	{
		logger.LogTrace("Parsing AST for document {Document}", documentState);

		var parser = new Parser(tokens);

		return parser.ParseRoot();
	}

	private FileSymbols ScanDeclarations(DocumentState documentState, RootNode ast)
	{
		logger.LogTrace("Scanning declarations for document {Document}", documentState);

		var collector = new VariableDeclarationCollector();

		collector.Visit(ast);

		return new FileSymbols
		{
			DocumentUri = documentState.DocumentUri, Declarations = collector.Declarations, Scopes = collector.Scopes,
		};
	}

	private void RecalculateSymbols()
	{
		logger.LogTrace("Recalculating symbols");

		foreach (var document in documents.Values)
		{
			var declarations = document.Symbols?.Declarations ?? [];
			foreach (var declaration in declarations)
			{
				_ = symbols.AddOrUpdate(declaration.Name, declaration, (_, old) =>
				{
					logger.LogWarning(
						"Replacing declaration for symbol {SymbolName} (previously declared at {OldDeclaration})",
						declaration.Name, old);

					return declaration;
				});
			}
		}
	}

	public void RemoveDocument(Uri documentUri)
	{
		logger.LogTrace("Removing document {DocumentUri} from session", documentUri);

		if (!documents.Remove(documentUri, out _))
		{
			logger.LogWarning("Document {DocumentUri} was not found in session; nothing to remove", documentUri);

			return;
		}

		_ = Diagnostics.Remove(documentUri, out _);

		RecalculateSymbols();
	}
}
