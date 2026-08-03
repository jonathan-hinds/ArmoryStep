using UnityEngine;

namespace OneStep.Editor.Build
{
    public sealed class WebBuildConfiguration : ScriptableObject
    {
        [field: SerializeField] public string OutputPath { get; private set; } = "Builds/Web/Production";
        [field: SerializeField] public bool DevelopmentBuild { get; private set; }
        [field: SerializeField] public bool AutoRun { get; private set; }

        public void Configure(string outputPath, bool developmentBuild, bool autoRun = false)
        {
            OutputPath = outputPath;
            DevelopmentBuild = developmentBuild;
            AutoRun = autoRun;
        }
    }
}
