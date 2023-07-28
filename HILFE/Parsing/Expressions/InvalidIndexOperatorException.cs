﻿using System.Diagnostics.CodeAnalysis;
using HILFE.Interpreting;

namespace HILFE.Parsing.Expressions;

[SuppressMessage("Design", "CA1032:Implement standard exception constructors")]
public class InvalidIndexOperatorException : InterpreterException
{
    public InvalidIndexOperatorException(string valueType) : base($"Invalid index expression: Cannot index into a value of type {valueType}")
    {
    }
}