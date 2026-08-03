using UnityEngine;

namespace OneStep.Core.Configuration
{
    [CreateAssetMenu(menuName = "OneStep/Configuration/Services", fileName = "ServicesConfiguration")]
    public sealed class ServicesConfiguration : ScriptableObject
    {
        [field: SerializeField] public string EnvironmentName { get; private set; } = "production";
        [field: SerializeField] public bool SignInAnonymously { get; private set; } = true;
        [field: SerializeField, Min(2)] public int DuelCapacity { get; private set; } = 2;
    }
}
