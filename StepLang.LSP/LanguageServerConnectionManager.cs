using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using StreamJsonRpc;

namespace StepLang.LSP;

public class LanguageServerConnectionManager : IAsyncDisposable
{
    private CancellationTokenSource stoppingSource = new();

    private CancellationToken StoppingToken => stoppingSource.Token;

    private readonly ConcurrentDictionary<Socket, Task> clientTasks = new();

    public async Task RunAsync(LanguageServerOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (options.UseStandardIO)
        {
            await HandleStandardIoAsync(StoppingToken);

            return;
        }

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        var endpoint = new IPEndPoint(IPAddress.Parse(options.Host), options.Port);
        socket.Bind(endpoint);

        await ListenAsync(socket, StoppingToken);
    }

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

    private async Task HandleClientAsync(Socket clientSocket, CancellationToken stoppingToken)
    {
        await using var server = new StepLangLanguageServer(stoppingToken);

        server.OnExit += (_, _) =>
        {
            _ = clientTasks.TryRemove(clientSocket, out _);

            clientSocket.Dispose();
        };

        await using var stream = new NetworkStream(clientSocket);

        await HandleStreamAsync(server, stream, stream);
    }

    private static async Task HandleStandardIoAsync(CancellationToken stoppingToken)
    {
        var server = new StepLangLanguageServer(stoppingToken);

        server.OnExit += (_, _) => Environment.Exit(0);

        await HandleStreamAsync(server, Console.OpenStandardOutput(), Console.OpenStandardInput());
    }

    private static async Task HandleStreamAsync(StepLangLanguageServer server, Stream output, Stream input)
    {
        using var rpc = new JsonRpc(output, input);

        rpc.AddLocalRpcTarget(server, new JsonRpcTargetOptions
        {
            MethodNameTransform = CommonMethodNameTransforms.CamelCase,
        });

        // rpc.TraceSource.Switch.Level = SourceLevels.All;
        // _ = rpc.TraceSource.Listeners.Add(new ConsoleTraceListener());

        rpc.StartListening();

        await rpc.Completion;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await stoppingSource.CancelAsync();

        foreach (var clientTask in clientTasks.Values)
        {
            await clientTask;
        }
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        stoppingSource.Dispose();
        stoppingSource = null!;
    }
}