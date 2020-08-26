# Changelog

## v1.6.1 - 2020.08.26
- Use original [Mono.Reflection](https://www.nuget.org/packages/Mono.Reflection/2.0.0) package instead of forked one.
- Use strongly-named [GrEmit](https://github.com/skbkontur/gremit) assembly.

## v1.5.3 - 2020.04.03
- Fix GroBuf to work on .NET Core 3 (issue [#18](https://github.com/skbkontur/GroBuf/issues/18)).
- Use [SourceLink](https://github.com/dotnet/sourcelink) to help ReSharper decompiler show actual code.

## v1.4 - 2018.09.14
- Use [Nerdbank.GitVersioning](https://github.com/AArnott/Nerdbank.GitVersioning) to automate generation of assembly 
  and nuget package versions.
- Fix GroBuf to work on .NET Core 2.1 runtime on Linux.
- Update [Gremit](https://github.com/skbkontur/gremit) dependency to v2.3.

## v1.3 - 2018.01.06
- Support .NET Standard 2.0.
- Switch to SDK-style project format and dotnet core build tooling.
