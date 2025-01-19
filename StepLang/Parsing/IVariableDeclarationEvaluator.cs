using StepLang.Interpreting;
using StepLang.Parsing.Nodes.VariableDeclarations;

namespace StepLang.Parsing;

public interface IVariableDeclarationEvaluator
{
	Variable Evaluate(VariableDeclarationNode variableDeclarationNode);
	Variable Evaluate(NullableVariableDeclarationNode variableDeclarationNode);
	Variable Evaluate(VariableInitializationNode variableDeclarationNode);
	Variable Evaluate(NullableVariableInitializationNode variableDeclarationNode);
	Variable Evaluate(ErrorVariableDeclarationNode variableDeclarationNode);
}
