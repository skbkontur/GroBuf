# Changelog

## v1.9.2 - 2023.04.24
- Add net7 support (PR [#32](https://github.com/skbkontur/GroBuf/pull/32))
- Drop tests for net5
- Switch to GitHub Actions

## v1.8.1 - 2022.02.18
- Sign GroBuf assembly with a strong name

## v1.7.8 - 2022.01.13
- Fix bug with negative array indices in GroBufHelpers static constructor on some Mono platforms (issue #28).

## v1.7.6 - 2021.11.30
- Drop netcoreapp2.1 support.
- Update GrEmit dependency to v3.4.10.

## v1.7.3 - 2021.03.11
- Fix GroBuf to work on .NET 5.0 (PR [#25](https://github.com/skbkontur/GroBuf/pull/25)).

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
