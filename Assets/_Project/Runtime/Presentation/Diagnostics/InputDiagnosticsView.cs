using OneStep.Input;
using UnityEngine;
using UnityEngine.UI;

namespace OneStep.Presentation.Diagnostics
{
    public sealed class InputDiagnosticsView : MonoBehaviour
    {
        [SerializeField] private GameInputReader input;
        [SerializeField] private Text output;

        public void Configure(GameInputReader reader, Text outputText)
        {
            input = reader;
            output = outputText;
        }

        private void Update()
        {
            if (input == null || output == null)
            {
                return;
            }

            output.text = $"INPUT  move {input.Move:F1}  pointer {input.PointerPosition:F0}\n{input.LastControlPath}";
        }
    }
}
