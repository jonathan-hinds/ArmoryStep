using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OneStep.Editor.Build
{
    public static class WebBuildPipeline
    {
        private const string DevelopmentConfigPath = "Assets/_Project/Settings/Build/Development.asset";
        private const string ProductionConfigPath = "Assets/_Project/Settings/Build/Production.asset";

        [MenuItem("Tools/OneStep/Build Web/Development")]
        public static void BuildDevelopment() => Build(DevelopmentConfigPath);

        [MenuItem("Tools/OneStep/Build Web/Production")]
        public static void BuildProduction() => Build(ProductionConfigPath);

        private static void Build(string configurationPath)
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

            var outputPath = Path.GetFullPath(configuration.OutputPath);
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
        }
    }
}
