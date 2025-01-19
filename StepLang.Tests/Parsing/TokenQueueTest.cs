using StepLang.Parsing;
using StepLang.Tokenizing;
using StepLang.Utils;

namespace StepLang.Tests.Parsing;

public class TokenQueueTest
{
	[Fact]
	public void TestDequeueReturnsErrorIfEmpty()
	{
		var queue = new TokenQueue();

		var noParamsResult = queue.Dequeue();
		var noParamsErr = Assert.IsType<Err<Token>>(noParamsResult);
		_ = Assert.IsType<UnexpectedEndOfTokensException>(noParamsErr.Exception);

		var countParamResult = queue.Dequeue(1);
		var countParamErr = Assert.IsType<Err<Token[]>>(countParamResult);
		_ = Assert.IsType<UnexpectedEndOfTokensException>(countParamErr.Exception);

		var allowedTypesParamResult = queue.Dequeue(TokenType.Whitespace);
		var allowedTypesParamErr = Assert.IsType<Err<Token>>(allowedTypesParamResult);
		_ = Assert.IsType<UnexpectedEndOfTokensException>(allowedTypesParamErr.Exception);
	}
}
