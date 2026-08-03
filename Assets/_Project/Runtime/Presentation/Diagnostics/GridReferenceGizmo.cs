using UnityEngine;

namespace OneStep.Presentation.Diagnostics
{
    public sealed class GridReferenceGizmo : MonoBehaviour
    {
        [SerializeField, Min(1)] private int columns = 10;
        [SerializeField, Min(1)] private int rows = 18;
        [SerializeField, Min(0.1f)] private float cellSize = 1f;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.75f, 0.25f);
            var origin = transform.position - new Vector3(columns * cellSize, rows * cellSize) * 0.5f;
            for (var x = 0; x <= columns; x++)
            {
                var from = origin + Vector3.right * (x * cellSize);
                Gizmos.DrawLine(from, from + Vector3.up * (rows * cellSize));
            }

            for (var y = 0; y <= rows; y++)
            {
                var from = origin + Vector3.up * (y * cellSize);
                Gizmos.DrawLine(from, from + Vector3.right * (columns * cellSize));
            }
        }
    }
}
