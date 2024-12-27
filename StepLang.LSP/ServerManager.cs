using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using JetBrains.Annotations;
using Lunet.Extensions.Logging.SpectreConsole;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nerdbank.Streams;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using StepLang.LSP.Handlers;

namespace StepLang.LSP;

public class ServerManager
{
	public async Task<int> RunAsync(ServerOptions options, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(options);

		if (options.UseStandardIO)
		{
			return await HandleStandardIoAsync(cancellationToken);
		}

		await HandleSocketAsync(options, cancellationToken);

		return 0;
	}

	private async Task HandleSocketAsync(ServerOptions options, CancellationToken cancellationToken)
	{
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
			await Console.Error.WriteLineAsync("Client task failed");
			await Console.Error.WriteLineAsync(e.ToString());
		}
		finally
		{
			_ = clientTasks.TryRemove(clientSocket, out _);

			clientSocket.Dispose();
		}
	}

	private static async Task<int> HandleStandardIoAsync(CancellationToken cancellationToken)
	{
		await using var input = Console.OpenStandardInput();
		await using var output = Console.OpenStandardOutput();

		using var server = CreateServer(input, output);

		int? code = null;
		using var _ = server.Exit.Subscribe(c => code = c);

		await HandleServerShutdownAsync(server, cancellationToken);

		return code ?? 0;
	}

	private static async Task HandleServerShutdownAsync(LanguageServer server, CancellationToken cancellationToken)
	{
		await server.Initialize(cancellationToken);

		await using var shutdownRegistration = cancellationToken.Register(server.ForcefulShutdown);

		await server.WaitForExit;
	}

	[MustDisposeResource]
	private static LanguageServer CreateServer(Stream input, Stream output)
	{
		var server = LanguageServer.Create(o =>
		{
			o.ServerInfo = new ServerInfo { Name = "STEP", Version = GitVersionInformation.FullSemVer, };

			o.Input = input.UsePipeReader();
			o.Output = output.UsePipeWriter();

			o.WithServices(s =>
				s.AddLogging(b =>
					b
						.ClearProviders()
						.AddSpectreConsole(new SpectreConsoleLoggerOptions { SingleLine = true, })
						.AddLanguageProtocolLogging()
						.SetMinimumLevel(LogLevel.Trace)
				)
			);

			o.WithHandler<SemanticTokensHandler>();
			o.WithHandler<DefinitionHandler>();
			o.WithHandler<DidOpenTextDocumentHandler>();
		});

		return server;
	}
}
