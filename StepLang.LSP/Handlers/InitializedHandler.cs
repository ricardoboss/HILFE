using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.General;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace StepLang.LSP.Handlers;

public class InitializedHandler(ILogger<InitializedHandler> logger) : LanguageProtocolInitializedHandlerBase
{
	public override Task<Unit> Handle(InitializedParams request, CancellationToken cancellationToken)
	{
		logger.LogTrace("Handling Initialized");

		return Task.FromResult(Unit.Value);
	}
}
