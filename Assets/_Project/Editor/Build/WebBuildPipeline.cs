using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using ZipCompressionLevel = System.IO.Compression.CompressionLevel;

namespace OneStep.Editor.Build
{
    public static class WebBuildPipeline
    {
        private const string DevelopmentConfigPath = "Assets/_Project/Settings/Build/Development.asset";
        private const string ProductionConfigPath = "Assets/_Project/Settings/Build/Production.asset";
        private const string ItchArchiveFileName = "OneStep-itch.zip";

        [MenuItem("Tools/OneStep/Build Web/Development")]
        public static void BuildDevelopment() => Build(DevelopmentConfigPath);

        [MenuItem("Tools/OneStep/Build Web/Production")]
        public static void BuildProduction()
        {
            var outputPath = Build(ProductionConfigPath);
            var archivePath = PackageForItch(outputPath);
            Debug.Log($"itch.io package created at {archivePath}. Upload this ZIP as an HTML5 browser build.");
        }

        private static string Build(string configurationPath)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                throw new InvalidOperationException("Install WebGL Build Support for this Unity editor version in Unity Hub before building.");
            }

            var configuration = AssetDatabase.LoadAssetAtPath<WebBuildConfiguration>(configurationPath);
            if (configuration == null)
            {
                throw new InvalidOperationException("Run Tools > OneStep > Build Foundation before creating a Web build.");
            }

            var outputPath = GetValidatedOutputPath(configuration.OutputPath);
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }
            Directory.CreateDirectory(outputPath);
            PlayerSettings.WebGL.exceptionSupport = configuration.DevelopmentBuild
                ? WebGLExceptionSupport.FullWithStacktrace
                : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.debugSymbolMode = configuration.DevelopmentBuild
                ? WebGLDebugSymbolMode.Embedded
                : WebGLDebugSymbolMode.Off;

            var options = BuildOptions.None;
            if (configuration.DevelopmentBuild)
            {
                options |= BuildOptions.Development;
            }

            if (configuration.AutoRun)
            {
                options |= BuildOptions.AutoRunPlayer;
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = options
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Web build failed: {report.summary.result} ({report.summary.totalErrors} errors).");
            }

            Debug.Log($"Web build completed at {outputPath} ({report.summary.totalSize / 1048576f:F1} MiB). ");
            return outputPath;
        }

        private static string GetValidatedOutputPath(string configuredPath)
        {
            var outputPath = Path.GetFullPath(configuredPath);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var webBuildRoot = Path.GetFullPath(Path.Combine(projectRoot, "Builds", "Web"))
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (!outputPath.StartsWith(webBuildRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Web build output must be inside {webBuildRoot}.");
            }

            return outputPath;
        }

        private static string PackageForItch(string outputPath)
        {
            ValidateBuildOutput(outputPath);

            var archivePath = Path.Combine(Path.GetDirectoryName(outputPath), ItchArchiveFileName);
            var temporaryArchivePath = archivePath + ".tmp";
            if (File.Exists(temporaryArchivePath))
            {
                File.Delete(temporaryArchivePath);
            }

            try
            {
                using (var archiveStream = File.Create(temporaryArchivePath))
                using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create))
                {
                    foreach (var filePath in Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories)
                                 .OrderBy(path => path, StringComparer.Ordinal))
                    {
                        var entryName = filePath.Substring(outputPath.Length)
                            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                            .Replace('\\', '/');
                        var entry = archive.CreateEntry(entryName, ZipCompressionLevel.Optimal);
                        using (var input = File.OpenRead(filePath))
                        using (var output = entry.Open())
                        {
                            input.CopyTo(output);
                        }
                    }
                }

                ValidateArchive(temporaryArchivePath);
                if (File.Exists(archivePath))
                {
                    File.Delete(archivePath);
                }
                File.Move(temporaryArchivePath, archivePath);
                return archivePath;
            }
            finally
            {
                if (File.Exists(temporaryArchivePath))
                {
                    File.Delete(temporaryArchivePath);
                }
            }
        }

        private static void ValidateBuildOutput(string outputPath)
        {
            var indexPath = Path.Combine(outputPath, "index.html");
            var stylePath = Path.Combine(outputPath, "TemplateData", "style.css");
            var buildPath = Path.Combine(outputPath, "Build");
            if (!File.Exists(indexPath) || !File.Exists(stylePath) || !Directory.Exists(buildPath))
            {
                throw new InvalidOperationException("Web build is incomplete: index.html, TemplateData/style.css, or Build is missing.");
            }

            var buildFiles = Directory.GetFiles(buildPath).Select(Path.GetFileName).ToArray();
            var requiredSuffixes = new[] { ".loader.js", ".data.unityweb", ".framework.js.unityweb", ".wasm.unityweb" };
            var missingSuffix = requiredSuffixes.FirstOrDefault(suffix =>
                !buildFiles.Any(fileName => fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)));
            if (missingSuffix != null)
            {
                throw new InvalidOperationException($"Web build is incomplete: Build/*{missingSuffix} is missing.");
            }
        }

        private static void ValidateArchive(string archivePath)
        {
            using (var archive = ZipFile.OpenRead(archivePath))
            {
                var entryNames = archive.Entries.Select(entry => entry.FullName).ToArray();
                if (entryNames.Any(name => name.Contains("\\")))
                {
                    throw new InvalidOperationException("itch.io package contains Windows-style ZIP paths.");
                }

                if (!entryNames.Contains("index.html", StringComparer.Ordinal) ||
                    !entryNames.Contains("TemplateData/style.css", StringComparer.Ordinal) ||
                    !entryNames.Any(name => name.StartsWith("Build/", StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException("itch.io package must contain index.html, Build, and TemplateData at its root.");
                }
            }
        }
    }
}
