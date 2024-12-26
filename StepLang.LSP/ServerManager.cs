using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nerdbank.Streams;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.JsonRpc.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using StepLang.LSP.Diagnostics;
using StepLang.LSP.Diagnostics.Analyzers;
using StepLang.LSP.Handlers;
using StepLang.LSP.Handlers.TextDocument;

namespace StepLang.LSP;

public class ServerManager(ServerOptions options)
{
	private ILogger<ServerManager> logger => lazyLogger.Value;
	private readonly Lazy<ILogger<ServerManager>> lazyLogger = new(() =>
		options.LoggerFactory?.CreateLogger<ServerManager>() ?? NullLogger<ServerManager>.Instance);

	public async Task<int> RunAsync(CancellationToken cancellationToken = default)
	{
		if (options.UseStandardIO)
		{
			return await HandleStandardIoAsync(cancellationToken);
		}

		await HandleSocketAsync(cancellationToken);

		return 0;
	}

	private async Task HandleSocketAsync(CancellationToken cancellationToken)
	{
		logger.LogInformation("Starting server on {Host}:{Port}", options.Host, options.Port);

		using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

		var endpoint = new IPEndPoint(IPAddress.Parse(options.Host), options.Port);

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

			using var server = CreateServer(ioStream, ioStream);

			await HandleServerShutdownAsync(server, cancellationToken);
		}
		catch (Exception e)
		{
			logger.LogError(e, "Client handling failed");
		}
		finally
		{
			_ = clientTasks.TryRemove(clientSocket, out _);

			clientSocket.Dispose();
		}
	}

	private async Task<int> HandleStandardIoAsync(CancellationToken cancellationToken)
	{
		logger.LogInformation("Starting server on standard input/output");

		await using var input = Console.OpenStandardInput();
		await using var output = Console.OpenStandardOutput();

		using var server = CreateServer(input, output);

		int? code = null;
		using var _ = server.Exit.Subscribe(c => code = c);

		await HandleServerShutdownAsync(server, cancellationToken);

		return code ?? 0;
	}

	private async Task HandleServerShutdownAsync(LanguageServer server, CancellationToken cancellationToken)
	{
		await server.Initialize(cancellationToken);

		await using var shutdownRegistration = cancellationToken.Register(server.ForcefulShutdown);

		logger.LogDebug("Waiting for server exit...");

		await server.WaitForExit;

		logger.LogInformation("Server is shutting down");
	}

	[MustDisposeResource]
	private LanguageServer CreateServer(Stream input, Stream output)
	{
		var info = new ServerInfo
		{
			Name = "STEP", Version = GitVersionInformation.FullSemVer,
		};

		var server = LanguageServer.Create(o => o
			.WithInput(input)
			.WithOutput(output)
			.WithServerInfo(info)
			.WithServices(ConfigureServices)
			.WithUnhandledExceptionHandler(OnUnhandledException)
			.WithResponseExceptionFactory(ResponseExceptionFactory)
			.WithHandler<InitializedHandler>()
			.WithHandler<DidOpenTextDocumentHandler>()
		);

		return server;
	}

	private Exception ResponseExceptionFactory(ServerError servererror, string message)
	{
		return new NotImplementedException();
	}

	private void OnUnhandledException(Exception exception)
	{
		logger.LogError(exception, "Unhandled exception");
	}

	private void ConfigureServices(IServiceCollection services)
	{
		if (options.LoggerFactory is { } loggerFactory)
		{
			_ = services
				.AddLogging()
				.RemoveAll<ILoggerFactory>()
				.AddSingleton(loggerFactory);
		}

		_ = services
			.AddSingleton<SessionState>()
			.AddSingleton<DiagnosticsRunner>()
			.AddTransient<IAnalyzer, UnusedDeclarationsAnalyzer>();
	}
}
