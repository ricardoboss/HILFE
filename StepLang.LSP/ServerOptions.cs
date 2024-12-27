namespace StepLang.LSP;

internal sealed class ServerOptions
{
	public string Host { get; set; } = "127.0.0.1";

	public int Port { get; set; } = 14246;

	public bool UseStandardIO { get; set; }
}
