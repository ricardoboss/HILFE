using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using StepLang.LSP.Diagnostics;
using StepLang.Parsing;
using StepLang.Parsing.Nodes;
using StepLang.Tokenizing;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace StepLang.LSP;

internal sealed class SessionState(
	ILogger<SessionState> logger,
	DiagnosticsRunner diagnosticsRunner,
	ILanguageServerFacade server)
{
	private readonly ConcurrentDictionary<Uri, DocumentState> documents = new();
	public IReadOnlyDictionary<Uri, DocumentState> Documents => documents;

	private readonly ConcurrentDictionary<string, List<DeclaredVariableInfo>> symbols = new();
	public IReadOnlyDictionary<string, IReadOnlyList<DeclaredVariableInfo>> Symbols => symbols.ToImmutableDictionary(
		kvp => kvp.Key, IReadOnlyList<DeclaredVariableInfo> (kvp) => kvp.Value
	);

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

	public void UpdateDocument(Uri documentUri, int version, Func<string, string> textUpdater)
	{
		logger.LogDebug("Updating document {DocumentUri}", documentUri);

		if (!documents.Remove(documentUri, out var oldDocument))
		{
			logger.LogWarning("Document {DocumentUri} was not found in session; nothing to update", documentUri);

			return;
		}

		var collection = Diagnostics.AddOrUpdate(documentUri, [], (_, _) => []);
		collection.CollectionChanged += (_, _) => SendDiagnostics(documentUri);

		var text = textUpdater(oldDocument.Text);

		var newDocumentState = new DocumentState { DocumentUri = documentUri, Version = version, Text = text, };

		documents[documentUri] = newDocumentState;

		RecalculateDocument(newDocumentState);

		RecalculateSymbols();

		QueueAnalysis(newDocumentState);
	}

	private void SendDiagnostics(Uri documentUri)
	{
		logger.LogTrace("Sending diagnostics for document {DocumentUri}", documentUri);

		var diagnosticParams = new PublishDiagnosticsParams
		{
			Uri = documentUri, Diagnostics = Diagnostics[documentUri], Version = Documents[documentUri].Version,
		};

		server.TextDocument.PublishDiagnostics(diagnosticParams);
	}

	private void RecalculateDocument(DocumentState documentState)
	{
		logger.LogTrace("Recalculating document {Document}", documentState);

		try
		{
			var tokens = Tokenize(documentState);
			documentState.Tokens = tokens;

			var ast = ParseAst(documentState, tokens);
			documentState.Ast = ast;

			var declarations = ScanDeclarations(documentState, ast);
			documentState.Symbols = declarations;
		}
		catch (StepLangException e)
		{
			ReportStepLangException(documentState, e);
		}
	}

	private void ReportStepLangException(DocumentState documentState, StepLangException e)
	{
		logger.LogTrace("Reporting exception {ExceptionType} for document {Document}", e.GetType().Name, documentState);

		var diagnostic = new Diagnostic
		{
			Code = e.ErrorCode,
			Message = e.Message,
			Severity = DiagnosticSeverity.Error,
			Range = e.Location?.ToRange() ?? new Range(),
			CodeDescription = new CodeDescription
			{
				Href = new Uri(e.HelpLink ?? "https://github.com/ricardoboss/STEP/wiki"),
			},
		};

		Diagnostics[documentState.DocumentUri].Add(diagnostic);
	}

	private void QueueAnalysis(DocumentState documentState)
	{
		logger.LogTrace("Queuing analysis for document {Document}", documentState);

		diagnosticsRunner.Dispatch(this, documentState);
	}

	private TokenCollection Tokenize(DocumentState documentState)
	{
		logger.LogTrace("Tokenizing document {Document}", documentState);

		var tokens = new Tokenizer(documentState.Text, strict: false).Tokenize().ToArray();

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

		var collector = new VariableDeclarationCollector(documentState.DocumentUri);

		collector.Visit(ast);

		return new FileSymbols
		{
			DocumentUri = documentState.DocumentUri,
			Declarations = collector.Declarations,
			Scopes = collector.Scopes,
		};
	}

	private void RecalculateSymbols()
	{
		logger.LogTrace("Recalculating symbols");

		HashSet<(string Name, Uri DocumentUri)> seen = [];
		foreach (var document in documents.Values)
		{
			var declarations = document.Symbols?.Declarations ?? [];
			foreach (var declaration in declarations)
			{
				_ = symbols.AddOrUpdate(declaration.Name, [declaration], (key, others) =>
				{
					// remove all declarations with the same name from the same document
					_ = others.RemoveAll(d => d.DocumentUri == document.DocumentUri);

					others.Add(declaration);

					return others;
				});

				_ = seen.Add((declaration.Name, document.DocumentUri));
			}
		}

		// delete all symbols that were not seen in the current documents
		foreach (var name in symbols.Keys.Except(seen.Select(t => t.Name)))
		{
			_ = symbols.TryRemove(name, out _);
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
