# Agent Guide

## Start Here

- This repository has no root solution. Work in the owning solution under `src/`, `samples/`, or `resources/` and keep changes scoped to that area.
- Read the nearest README before editing. Use the root [README](README.md) for the repository map, [sample prerequisites](samples/README.md), [utility notes](src/utilities/README.md), and [wizard build notes](src/wizards/README.md).
- Treat `src/` as utility and wizard source, `samples/` as independent reference applications, and `bin/` as prebuilt distribution artifacts. Do not hand-edit compiled binaries.
- Projects use legacy, non-SDK-style .NET Framework 4.8 project files. Preserve their format and existing package/reference conventions unless modernization is explicitly requested.

## Build And Test

- Build the narrowest owning solution from a Visual Studio Developer Command Prompt:

  ```bat
  msbuild.exe path\to\OwningSolution.sln /t:Rebuild /p:Configuration=Debug
  ```

- Do not assume `dotnet build` supports these projects. They depend on full .NET Framework and, in several areas, Visual Studio extension tooling or an installed Sage 300 environment.
- The repository-wide [rebuild script](src/NoWebSync_RebuildMergeISVProjectAndWizards.bat) is a reference workflow, not a portable command: it hardcodes `C:\Projects\Sage300-SDK\Develop` and Visual Studio 2019 Enterprise. Adapt its root and Visual Studio paths before running it.
- Wizard compilation uses PowerShell to package templates; see [wizard prerequisites](src/wizards/README.md). Building a VSIX does not install it.
- Sample compilation requires `Sage300WebDir` to point to the installed Sage 300 Online Web directory and `Sage300WebSdkDir` to point to this repository. Release builds may deploy to the local Worker folder; stop or restart IIS as described in the [utility notes](src/utilities/README.md).
- The focused automated tests are in `src/utilities/MergeISVProject/MergeISVProjectTests`. Build `src/utilities/MergeISVProject/MergeISVProject.sln`, then run the MSTest assembly with Visual Studio Test Explorer or `vstest.console.exe`.
- Run the full focused suite with:

  ```bat
  vstest.console.exe src\utilities\MergeISVProject\MergeISVProjectTests\bin\Debug\MergeISVProjectTests.dll
  ```

- Run one test with:

  ```bat
  vstest.console.exe src\utilities\MergeISVProject\MergeISVProjectTests\bin\Debug\MergeISVProjectTests.dll /Tests:CommandLineOptions_Test_NoParameters
  ```

- There is no repository-wide lint command. Compilation uses warning level 4, and some sample projects treat warnings as errors.
- Report environment-dependent validation as not run when Sage 300, IIS, Visual Studio workloads, or proprietary assemblies are unavailable; do not replace it with unrelated repository-wide checks.

## Architecture

- Visual Studio wizards under `src/wizards` scaffold Sage 300 MVC and Web API solutions from the unpacked sources in `src/wizards/Templates`.
- Utilities under `src/utilities` support generation and deployment. Generated Web projects invoke `MergeISVProject.exe` after build to stage compiled assets, minify JavaScript, and optionally deploy Release output.
- Samples are independent runnable references for generated solutions. Their standard dependency flow is `Web` -> `Services` -> `BusinessRepository`, with shared `Interfaces`, `Models`, and localized `Resources`; Web API templates add `WebApi.Models` and `WebApi.WebApi`.
- `resources/Sage300Resources` contains localization assemblies used by screens and the Language Resource Wizard. `maps` documents framework and module relationships whose implementation source is not included here.

## Change Boundaries

- Use [settings/AccpacDotNetVersion.props](settings/AccpacDotNetVersion.props) as the shared source of Sage assembly versions for samples. Generated wizard solutions intentionally receive a local copy.
- Preserve legacy MSBuild project format. Files are explicitly listed in `.csproj` files, so update the matching `Compile`, `Content`, `None`, or resource entries when adding, deleting, or moving files.
- Preserve `packages.config`, existing `HintPath` references, and project-reference conventions unless modernization is explicitly requested.
- Preserve template placeholders such as `TPA`, `TU`, and `ValuedPartner`; they are replaced by wizard generation workflows.
- Treat unpacked folders under `src/wizards/Templates` as template source. Regenerate `.vstemplate` files and ZIP archives with the local scripts and copy them to the wizard package; do not edit generated archives directly.
- Shared web assets are duplicated across UI, customization, and upgrade template trees. Search those trees and keep intentional copies synchronized when changing template framework assets.
- Never edit `obj/`, NuGet-generated files, package caches, generated template archives, or build outputs as source. Update their owning project or template and regenerate them when the task requires refreshed artifacts.
- When a task explicitly requires refreshed distribution binaries, update only copies that already exist in source control unless the task explicitly asks to add a new distribution location. Build or post-build workflows may create untracked copies; remove those generated files rather than adding them. Verify the retained tracked copies and keep restore/build intermediates out of the final change.
- `samples/UpdateSamplesFromColumbusWeb.bat` and resource synchronization scripts depend on Sage-internal source trees. Do not run them unless the required internal environment is explicitly available.
- Keep sample changes within the selected sample solution. Use `samples/ClearStatistics` and the corresponding tutorial as the primary reference for a generated web screen; use module maps under [maps](maps/README.md) when tracing controller or repository relationships.

## Design References

- Follow the existing generated-layer structure and naming in the nearest sample rather than inventing a parallel architecture.
- Consult [coding patterns](docs/patterns/Sage300SDK_CodingPatterns.docx), [coding standards](docs/standards/Sage300SDK_CodingStandards.docx), and [software architecture](docs/development/Sage300SDK_SoftwareArchitecture.docx) for broad changes.
- For Web API work, use the [developer reference](docs/webapi/Sage300SDK_DeveloperReference.docx) and the runnable examples under `samples/WebAPI_*`.
- For screen customization, use the specifications and tutorial under `docs/customization/` and the examples under `samples/customization/`.