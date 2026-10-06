# MergeISVProject Agent Guide

## Scope

- This solution contains the .NET Framework 4.8 `MergeISVProject` console application and its legacy MSTest project.
- The utility is called by generated Sage 300 Web project post-build events. It stages deployment assets, compiles Razor views, optionally minifies JavaScript, and can copy Release output into a local Sage 300 Web and Worker installation.
- Follow the repository-level `AGENTS.md` in addition to this project-specific guide.

## Build And Test

Run commands from a Visual Studio Developer Command Prompt:

```bat
msbuild.exe src\utilities\MergeISVProject\MergeISVProject.sln /t:Restore /p:RestorePackagesConfig=true
msbuild.exe src\utilities\MergeISVProject\MergeISVProject.sln /t:Rebuild /p:Configuration=Debug
vstest.console.exe src\utilities\MergeISVProject\MergeISVProjectTests\bin\Debug\MergeISVProjectTests.dll
```

Run one test by method name:

```bat
vstest.console.exe src\utilities\MergeISVProject\MergeISVProjectTests\bin\Debug\MergeISVProjectTests.dll /Tests:CommandLineOptions_Test_NoParameters
```

- The projects use legacy `packages.config` restore and non-SDK-style `.csproj` files. Restore against the solution with `/p:RestorePackagesConfig=true`; restoring the test or application project directly may fail because no `SolutionDir` is available.
- There is no separate lint task; compile with warning level 4.
- `build.bat` is a legacy Visual Studio 2013 Release-build wrapper. Prefer the explicit `msbuild.exe` command unless reproducing that workflow.
- The compiler and exit-code regressions are in `ApplicationExitCodeTests`. Run them with `/TestCaseFilter:FullyQualifiedName~ApplicationExitCodeTests`.
- `CommandLineOptions_Test_AllValidArguments` validates that its hardcoded folders exist, and `CommandLineOptions_Test_CleanupArguments` has a count assertion tied to its argument fixture. Check those fixtures before treating either failure as a product regression.
- A successful application build copies `MergeISVProject.exe` into `bin\utilities`, `src\wizards\Templates\UIWizards\Web`, and `src\wizards\Templates\UpgradeWizard\Items`. ZIP and VSIX artifacts do not currently embed another copy.
- When refreshing distributed executables, rebuild `Release`, compare assembly versions and SHA-256 hashes across all three destinations, and remove `bin` and restored `packages` intermediates afterward. Only `bin\utilities\MergeISVProject.exe` is currently tracked; review the two generated template copies intentionally.

## Architecture

- `Program` creates `CommandLineOptions`, `Logger`, and `MergeISVProjectDriver`, then displays usage/errors or runs the deployment pipeline.
- `CommandLineOptions` is the attribute-driven command-line schema and validator. Required, optional, and existing-folder rules come from `CustomAttributes`; usage text and validation are derived from those option properties.
- `MergeISVProjectDriver` owns the workflow. Mode `0` stages a full generated solution; mode `1` stages a single project. It requires the `release` build profile, creates deployment folders, compiles views with the .NET Framework tools, filters binaries, copies resources/menu/bootstrap files, and deploys unless `--nodeploy` is set.
- Razor compilation uses the framework selected by `--dotnetframeworkpath`; do not hardcode `Framework` or `Framework64`. A nonzero `aspnet_compiler.exe` result must terminate packaging, and `Program.Main` must return a nonzero application exit code.
- Roslyn Razor compilation is opt-in. When the source `Web.config` names `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`, the driver temporarily stages that config, the provider DLL, and `bin\roslyn`, then removes the compiler payload before packaging. Projects without that configuration continue using the .NET Framework CodeDOM provider.
- Resolve `Microsoft.Web.Infrastructure.dll` from the Web project's compiled `bin` first, then fall back to `Sage300WebSdkDir\bin\utilities`. If neither copy exists, fail with a logged `MergeISVProjectException` rather than passing an unset environment variable to `Path.Combine`.
- `FolderManager` is the source of truth for original, staging, compiled, final Web/Worker, and live Sage 300 paths. Keep path construction there rather than scattering new path rules through the driver.
- `SageISVMinifier` handles JavaScript minification. `FileUtilities` and `Utilities` contain shared file/process/application helpers. `Messages` and `Resources.resx` contain user-facing text.
- `MergeISVProjectTests` covers command-line parsing plus compiler exit propagation, Roslyn staging/fallback, missing compiler assets, and temporary payload cleanup. It uses MSTest `PrivateObject` for private parser helpers and `InternalsVisibleTo` for focused driver tests.

## Conventions

- Preserve C# 6 compatibility and the existing legacy project structure. Add every new source or resource file to the appropriate `.csproj`.
- When adding or changing a command-line option, update `ICommandLineOptions`, the attributed property in `CommandLineOptions`, parsing/validation behavior, usage output, and focused tests together.
- Preserve option names used by generated project post-build events: `--mode`, `--solutionpath`, `--webprojectpath`, `--menufilename`, `--buildprofile`, `--dotnetframeworkpath`, `--minify`, `--nodeploy`, `--testdeploy`, `--log`, and `--extraresourcelanguages`.
- Keep deployment mutations behind `--nodeploy` and `--testdeploy` semantics. Tests and local verification should avoid writing to a live Sage 300 installation.
- Log operational steps and failures through `ILogger`. Use `MergeISVProjectException` for failures that have already been logged and should terminate the run.
- Keep the Roslyn provider version project-owned: stage the provider selected and restored by the generated Web project rather than adding a provider package dependency to this utility.
- Stop only a `VBCSCompiler.exe` whose full executable path belongs to the staged `bin\roslyn` payload; never terminate unrelated compiler servers by process name alone.
- Stop that staged compiler server and remove its payload immediately after `aspnet_compiler.exe` exits, before copying `Compiled\bin` over `Staging\bin`; otherwise the bulk copy can fail while overwriting locked Roslyn files.
- Loading the built executable with `Assembly.LoadFrom` in a persistent PowerShell session locks the build output. Use an isolated smoke-test process or release that host before rebuilding.
- Edit `Resources.resx`, not the auto-generated `Resources.Designer.cs`, for resource text changes.
- Do not run `postbuild.bat` or `psCopyArtifactToPublish.ps1` unless the task explicitly requires refreshing all distributed copies; both scripts copy binaries and dependencies across multiple repository locations.
