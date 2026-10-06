using System;
using System.Diagnostics;
using System.IO;
using MergeISVProject;
using MergeISVProject.CustomExceptions;
using MergeISVProject.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MergeISVProjectTests
{
    [TestClass]
    public class ApplicationExitCodeTests
    {
        [TestMethod]
        public void Program_Main_InvalidArguments_ReturnsFailureExitCode()
        {
            var exitCode = Program.Main(new string[0]);

            Assert.AreEqual(1, exitCode);
        }

        [TestMethod]
        public void RunAspNetCompiler_NonzeroExit_ThrowsWithExitCode()
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = Environment.GetEnvironmentVariable("ComSpec"),
                Arguments = "/d /c exit 42"
            };
            var logger = new Logger("MergeISVProjectTests.log", Path.GetTempPath(), false);

            try
            {
                MergeISVProjectDriver.RunAspNetCompiler(startInfo, logger);
                Assert.Fail("Expected a compiler failure for a nonzero process exit code.");
            }
            catch (MergeISVProjectException exception)
            {
                StringAssert.Contains(exception.Message, "aspnet_compiler.exe exit code: 42");
            }
        }

        [TestMethod]
        public void ResolveWebInfrastructurePath_MissingSdkEnvironment_ThrowsHelpfulError()
        {
            const string variableName = "Sage300WebSdkDir";
            var originalValue = Environment.GetEnvironmentVariable(variableName);
            var webBin = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var logger = new Logger("MergeISVProjectTests.log", Path.GetTempPath(), false);

            try
            {
                Directory.CreateDirectory(webBin);
                Environment.SetEnvironmentVariable(variableName, null);

                try
                {
                    MergeISVProjectDriver.ResolveWebInfrastructurePath(webBin, logger);
                    Assert.Fail("Expected a failure when Sage300WebSdkDir is not set.");
                }
                catch (MergeISVProjectException exception)
                {
                    StringAssert.Contains(exception.Message, variableName);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable(variableName, originalValue);
                Directory.Delete(webBin, recursive: true);
            }
        }

        [TestMethod]
        public void ResolveWebInfrastructurePath_ProjectBinContainsFile_UsesProjectFile()
        {
            const string variableName = "Sage300WebSdkDir";
            var originalValue = Environment.GetEnvironmentVariable(variableName);
            var webBin = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var expectedPath = Path.Combine(webBin, "Microsoft.Web.Infrastructure.dll");
            var logger = new Logger("MergeISVProjectTests.log", Path.GetTempPath(), false);

            try
            {
                Directory.CreateDirectory(webBin);
                File.WriteAllText(expectedPath, "dependency");
                Environment.SetEnvironmentVariable(variableName, null);

                var actualPath = MergeISVProjectDriver.ResolveWebInfrastructurePath(webBin, logger);

                Assert.AreEqual(expectedPath, actualPath);
            }
            finally
            {
                Environment.SetEnvironmentVariable(variableName, originalValue);
                Directory.Delete(webBin, recursive: true);
            }
        }

        [TestMethod]
        public void StageRoslynCompilerAssets_ConfiguredProject_CopiesCompilerPayload()
        {
            var testRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var sourceRoot = Path.Combine(testRoot, "Source");
            var sourceBin = Path.Combine(sourceRoot, "bin");
            var sourceRoslyn = Path.Combine(sourceBin, "roslyn");
            var stagingRoot = Path.Combine(testRoot, "Staging");
            var stagingBin = Path.Combine(stagingRoot, "bin");
            var providerAssembly = "Microsoft.CodeDom.Providers.DotNetCompilerPlatform.dll";

            try
            {
                Directory.CreateDirectory(sourceRoslyn);
                Directory.CreateDirectory(stagingBin);
                File.WriteAllText(
                    Path.Combine(sourceRoot, "web.config"),
                    "<configuration><system.codedom><compilers><compiler language=\"c#\" type=\"Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider\" /></compilers></system.codedom></configuration>");
                File.WriteAllText(Path.Combine(sourceBin, providerAssembly), "provider");
                File.WriteAllText(Path.Combine(sourceRoslyn, "csc.exe"), "compiler");

                var logger = new Logger("MergeISVProjectTests.log", Path.GetTempPath(), false);
                var staged = MergeISVProjectDriver.StageRoslynCompilerAssets(sourceRoot, stagingRoot, logger);

                Assert.IsTrue(staged);
                Assert.IsTrue(File.Exists(Path.Combine(stagingRoot, "web.config")));
                Assert.IsTrue(File.Exists(Path.Combine(stagingBin, providerAssembly)));
                Assert.IsTrue(File.Exists(Path.Combine(stagingBin, "roslyn", "csc.exe")));
            }
            finally
            {
                if (Directory.Exists(testRoot))
                {
                    Directory.Delete(testRoot, recursive: true);
                }
            }
        }

        [TestMethod]
        public void StageRoslynCompilerAssets_UnconfiguredProject_UsesFrameworkProvider()
        {
            var testRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var sourceRoot = Path.Combine(testRoot, "Source");
            var stagingRoot = Path.Combine(testRoot, "Staging");

            try
            {
                Directory.CreateDirectory(sourceRoot);
                Directory.CreateDirectory(stagingRoot);

                var logger = new Logger("MergeISVProjectTests.log", Path.GetTempPath(), false);
                var staged = MergeISVProjectDriver.StageRoslynCompilerAssets(sourceRoot, stagingRoot, logger);

                Assert.IsFalse(staged);
            }
            finally
            {
                if (Directory.Exists(testRoot))
                {
                    Directory.Delete(testRoot, recursive: true);
                }
            }
        }

        [TestMethod]
        public void StageRoslynCompilerAssets_MissingCompilerPayload_ThrowsWithPath()
        {
            var testRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var sourceRoot = Path.Combine(testRoot, "Source");
            var sourceBin = Path.Combine(sourceRoot, "bin");
            var sourceRoslyn = Path.Combine(sourceBin, "roslyn");
            var stagingRoot = Path.Combine(testRoot, "Staging");

            try
            {
                Directory.CreateDirectory(sourceBin);
                Directory.CreateDirectory(Path.Combine(stagingRoot, "bin"));
                File.WriteAllText(
                    Path.Combine(sourceRoot, "web.config"),
                    "<configuration><system.codedom><compilers><compiler language=\"c#\" type=\"Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider\" /></compilers></system.codedom></configuration>");
                File.WriteAllText(
                    Path.Combine(sourceBin, "Microsoft.CodeDom.Providers.DotNetCompilerPlatform.dll"),
                    "provider");

                var logger = new Logger("MergeISVProjectTests.log", Path.GetTempPath(), false);

                try
                {
                    MergeISVProjectDriver.StageRoslynCompilerAssets(sourceRoot, stagingRoot, logger);
                    Assert.Fail("Expected a failure when the Roslyn compiler payload is missing.");
                }
                catch (MergeISVProjectException exception)
                {
                    StringAssert.Contains(exception.Message, sourceRoslyn);
                }
            }
            finally
            {
                if (Directory.Exists(testRoot))
                {
                    Directory.Delete(testRoot, recursive: true);
                }
            }
        }

        [TestMethod]
        public void RemoveRoslynCompilerAssets_RemovesTemporaryPayload()
        {
            var testRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var stagingBin = Path.Combine(testRoot, "bin");
            var compilerFolder = Path.Combine(stagingBin, "roslyn");
            var provider = Path.Combine(stagingBin, "Microsoft.CodeDom.Providers.DotNetCompilerPlatform.dll");

            try
            {
                Directory.CreateDirectory(compilerFolder);
                File.WriteAllText(provider, "provider");
                File.WriteAllText(Path.Combine(compilerFolder, "csc.exe"), "compiler");

                var logger = new Logger("MergeISVProjectTests.log", Path.GetTempPath(), false);
                MergeISVProjectDriver.RemoveRoslynCompilerAssets(stagingBin, logger);

                Assert.IsFalse(File.Exists(provider));
                Assert.IsFalse(Directory.Exists(compilerFolder));
            }
            finally
            {
                if (Directory.Exists(testRoot))
                {
                    Directory.Delete(testRoot, recursive: true);
                }
            }
        }
    }
}