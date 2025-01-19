# Unreleased

* (internal) Workflow now checks the built binaries before publishing
* Track and report core and CLI build metadata separately by @ricardoboss in https://github.com/ricardoboss/STEP/pull/131
* (internal) Replace custom coverage summary reporting with coveralls by @ricardoboss in https://github.com/ricardoboss/STEP/pull/133
* Improved error reporting for when identifier usage ends early by @ricardoboss in https://github.com/ricardoboss/STEP/pull/137
* (internal) Replaced `cmdwtf.BuildTimestampGenerator` with `Baugen` by @ricardoboss in https://github.com/ricardoboss/STEP/pull/139
* (internal) Upgraded to xunit v3 core by @ricardoboss in https://github.com/ricardoboss/STEP/pull/140
* Added `env` function by @ricardoboss in https://github.com/ricardoboss/STEP/pull/144
* Added new error handling for collecting and reporting diagnostics by @ricardoboss in https://github.com/ricardoboss/STEP/pull/145

# v2.1.0

* Changed "power" operator from `^` to `**` by @ricardoboss in https://github.com/ricardoboss/STEP/pull/107
* Added support for nullable type declarations by @ricardoboss in https://github.com/ricardoboss/STEP/pull/102
* Major rewrite of the parser and interpreter to achieve more abstraction and better error reporting by @ricardoboss
  in https://github.com/ricardoboss/STEP/pull/109
    * Added "parse" command to show the parsed abstract syntax tree
    * All variable declarations now support the nullability indicator (`?`)
    * Function definitions can now specify default parameter values
    * `break` and `continue` no longer allow expressions to break/continue for multiple loops
    * Exceptions that were previously printed with the affected code can now also show the causing token
    * Native functions now support union types to accept more than one value type of parameter
* Added `httpServer` and `fileResponse` functions by @ricardoboss in https://github.com/ricardoboss/STEP/pull/112
    * Also classify `fetch` as a web function together with `httpServer` and `fileResponse`
* New functions:
    * `indexOf`
    * `containsKey` by @ricardoboss
      in https://github.com/ricardoboss/STEP/commit/d64468b6ce9e8b2efb3dffddc73bc12ddb9c3dec
    * `split` by @ricardoboss in https://github.com/ricardoboss/STEP/pull/115
* `print` and `println` now flush to the output directly after being called
* loop constructs allow truthy values to continue looping (as opposed to only boolean values)
* Many bug fixes, including:
    * Loops new properly handle `break` and `continue` statements
    * Loop bodies now better keep track of their own scope
    * Fix conversion of map result to expression node by wrapping keys in quotes before passing them as values to
      LiteralString tokens by @ricardoboss
      in https://github.com/ricardoboss/STEP/commit/1ec33101df2f5b26cd402e5bc3e0e06f87c9a7cf
* Logical operators now short-circuit
    * This also enables "bash-style" expressions like `true && "it's true"` evaluating to `"it's true"`
* Added support for variable length separators in split function and made length parameter of substring optional by
  @ricardoboss in https://github.com/ricardoboss/STEP/commit/d56458eee4919d65ff4ed410d0a3edfe4d3b509d
* Fixed issue with paths relative to call location instead of relative to execution location by @ricardoboss
  in https://github.com/ricardoboss/STEP/commit/027bbf04b494501280015d6dba881071ee309cfa
* Updated to .NET 8 by @ricardoboss in https://github.com/ricardoboss/STEP/pull/116
* Now using Spectre.Console for CLI
* Added new `highlight` command

For more info, see https://github.com/ricardoboss/STEP/milestone/4?closed=1

# v2.0.0

* Use strong types for expression results by @ricardoboss in https://github.com/ricardoboss/STEP/pull/12
* Use JSON source generators to enable trimming by @ricardoboss in https://github.com/ricardoboss/STEP/pull/14
* Keep wiki sources in main repository by @ricardoboss in https://github.com/ricardoboss/STEP/pull/21
* Move license to its own file by @ricardoboss in https://github.com/ricardoboss/STEP/issues/27
* Added many functions to the standard library by @ricardoboss & @chucker
  in https://github.com/ricardoboss/STEP/issues/5
* Exceptions now include help links to helpful Docs by @ricardoboss in https://github.com/ricardoboss/STEP/issues/17
* Added a `format` command to the CLI by @ricardoboss in https://github.com/ricardoboss/STEP/issues/19
* Added contribution documentation by @ricardoboss in https://github.com/ricardoboss/STEP/issues/26

For more info, see https://github.com/ricardoboss/STEP/milestone/2?closed=1

# v1.0.0 - Initial release 🥳

* Added .NET library for parsing and interpreting STEP programs
* Added CLI for running STEP programs from files
* fix TestAdditiveMultiplicativePrecedencesWithParentheses by @chucker in https://github.com/ricardoboss/STEP/pull/1
* move examples to subdir and simplify `.csproj` by @chucker in https://github.com/ricardoboss/STEP/pull/2
* run all example `.hil` files as an integration test of sorts by @chucker in https://github.com/ricardoboss/STEP/pull/3
* Keep track of token locations and improve error reporting by @ricardoboss
  in https://github.com/ricardoboss/STEP/pull/8
* Add `import` statements by @ricardoboss in https://github.com/ricardoboss/STEP/pull/9
* Added string index operator by @ricardoboss in https://github.com/ricardoboss/STEP/pull/10

For more info, see https://github.com/ricardoboss/STEP/milestone/1?closed=1
