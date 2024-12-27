namespace StepLang.LSP;

public class ServerOptions
{
	public string Host { get; set; } = "127.0.0.1";

	public int Port { get; set; } = 50051;

	public bool UseStandardIO { get; set; }
}
