using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using StepLang.LSP.Diagnostics;
using StepLang.LSP.Diagnostics.Analyzers;
using StepLang.LSP.Handlers;
using StepLang.LSP.Handlers.TextDocument;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace StepLang.LSP;

internal sealed class ServerManager(
	ILogger<ServerManager> logger,
	IOptions<ServerOptions> delegateOptions,
	IServiceProvider
		services)
{
	private ServerOptions Options => delegateOptions.Value;

	public async Task<int> RunAsync(CancellationToken cancellationToken = default)
	{
		if (Options.UseStandardIO)
		{
			return await HandleStandardIoAsync(cancellationToken);
		}

		await HandleSocketAsync(cancellationToken);

		return 0;
	}

	private async Task HandleSocketAsync(CancellationToken cancellationToken)
	{
		logger.LogInformation("Starting server on {Host}:{Port}", Options.Host, Options.Port);

		using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

		var endpoint = new IPEndPoint(IPAddress.Parse(Options.Host), Options.Port);

		socket.Bind(endpoint);

		await ListenAsync(socket, cancellationToken);
	}

	private readonly ConcurrentDictionary<Socket, Task> clientTasks = new();

	private async Task ListenAsync(Socket serverSocket, CancellationToken cancellationToken)
	{
		serverSocket.Listen();

		while (!cancellationToken.IsCancellationRequested)
		{
			var clientSocket = await serverSocket.AcceptAsync(cancellationToken);

			logger.LogDebug("Client connected from {RemoteAddress}", clientSocket.RemoteEndPoint);

			var clientTask = HandleClientAsync(clientSocket, cancellationToken);

			clientTasks[clientSocket] = clientTask;
		}
	}

	private async Task HandleClientAsync(Socket clientSocket, CancellationToken cancellationToken)
	{
		try
		{
			await using var ioStream = new NetworkStream(clientSocket);

			using var server = await CreateServerAsync(ioStream, ioStream);

			await HandleServerShutdownAsync(server, cancellationToken);
		}
		catch (Exception e)
		{
			logger.LogError(e, "Client handling failed");
		}
		finally
		{
			_ = clientTasks.TryRemove(clientSocket, out _);

#pragma warning disable IDISP007
			clientSocket.Dispose();
#pragma warning restore IDISP007
		}
	}

	private async Task<int> HandleStandardIoAsync(CancellationToken cancellationToken)
	{
		logger.LogInformation("Starting server on standard input/output");

		await using var input = Console.OpenStandardInput();
		await using var output = Console.OpenStandardOutput();

		using var server = await CreateServerAsync(input, output);

		int? code = null;
		using var _ = server.Exit.Subscribe(c => code = c);

		await HandleServerShutdownAsync(server, cancellationToken);

		return code ?? 0;
	}

	private async Task HandleServerShutdownAsync(LanguageServer server, CancellationToken cancellationToken)
	{
		await using var shutdownRegistration = cancellationToken.Register(server.ForcefulShutdown);

		logger.LogDebug("Waiting for server exit...");

		await server.WaitForExit;

		logger.LogInformation("Server is shutting down");
	}

	[MustDisposeResource]
	private async Task<LanguageServer> CreateServerAsync(Stream input, Stream output)
	{
		var info = new ServerInfo { Name = "STEP", Version = GitVersionInformation.FullSemVer };

		var server = await LanguageServer.From(o =>
			{
				o
					.WithInput(input)
					.WithOutput(output)
					.WithServerInfo(info)
					.WithServices(ConfigureServices)
					.WithUnhandledExceptionHandler(OnUnhandledException)
					.WithHandler<DidOpenTextDocumentHandler>()
					.WithHandler<DidChangeTextDocumentHandler>()
					.WithHandler<CodeActionTextDocumentHandler>()
					.WithHandler<DidCloseTextDocumentHandler>()
					.WithHandler<DefinitionHandler>()
					.WithHandler<SemanticTokensHandler>()
					;
			},
			services
		);

		return server;
	}

	private void OnUnhandledException(Exception exception)
	{
		logger.LogError(exception, "Unhandled exception");
	}

	private void ConfigureServices(IServiceCollection s)
	{
		_ = s
			.AddLogging(b =>
			{
				b
					.SetMinimumLevel(LogLevel.Trace)
					.AddFilter("StepLang", LogLevel.Trace)
					.ClearProviders()
					.AddLanguageProtocolLogging();

				if (Options.UseStandardIO)
					return;

				// only when NOT using stdio, we add a logger that can use stdio for logging
				b.AddSimpleSpectreConsole();
			})
			.AddSingleton<SessionState>()
			.AddSingleton<DiagnosticsRunner>()
			.AddTransient<IAnalyzer, UnusedDeclarationsAnalyzer>();
	}
}
