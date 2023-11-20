using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods")]
[assembly: SuppressMessage("Design", "CA1032:Implement standard exception constructors")]
[assembly: SuppressMessage("Usage", "CA2225:Operator overloads have named alternates")]
[assembly: InternalsVisibleTo("StepLang.Tests")]
