using UnityEngine;

namespace OneStep.Core.Configuration
{
    [CreateAssetMenu(menuName = "OneStep/Configuration/Viewport", fileName = "ViewportConfiguration")]
    public sealed class ViewportConfiguration : ScriptableObject
    {
        [field: SerializeField, Min(1)] public int ReferenceWidth { get; private set; } = 144;
        [field: SerializeField, Min(1)] public int ReferenceHeight { get; private set; } = 256;
        [field: SerializeField, Min(1)] public int AssetsPixelsPerUnit { get; private set; } = 16;
        [field: SerializeField] public Color LetterboxColor { get; private set; } = new(0.015f, 0.02f, 0.03f, 1f);

        public float TargetAspect => (float)ReferenceWidth / ReferenceHeight;
        public float OrthographicSize => ReferenceHeight / (AssetsPixelsPerUnit * 2f);
    }
}
