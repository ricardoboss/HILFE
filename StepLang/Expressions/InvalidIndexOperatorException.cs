using StepLang.Expressions.Results;
using StepLang.Parsing;

namespace StepLang.Expressions;

public class InvalidIndexOperatorException(
	string index,
	ResultType resultType,
	string operation)
	: ParserException(4, null, // TODO: somehow get a token from the index access expression
		$"Invalid index expression: Cannot {operation} index {index} of a value of type {resultType.ToTypeName()}",
		"Make sure you're accessing an index of a value that supports indexing (like lists or maps).");
