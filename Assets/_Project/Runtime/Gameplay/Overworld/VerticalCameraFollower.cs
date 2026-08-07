using UnityEngine;

namespace OneStep.Gameplay.Overworld
{
    [RequireComponent(typeof(Camera))]
    public sealed class VerticalCameraFollower : MonoBehaviour
    {
        private AdventureSession _session;
        private float _fixedX;
        private float _minimumY;
        private float _highestCameraY;

        public void Configure(AdventureSession session, int worldWidth)
        {
            _session = session;
            _fixedX = CalculateHorizontalCenter(worldWidth);
            _minimumY = GetComponent<Camera>().orthographicSize - 0.5f;
            _highestCameraY = Mathf.Max(_minimumY, session.PlayerPosition.y + 3f);
            transform.position = new Vector3(_fixedX, _highestCameraY, transform.position.z);
        }

        public static float CalculateHorizontalCenter(int worldWidth)
        {
            // Tilemap cell x occupies [x, x + 1], so cells 0..width-1 span [0, width].
            return Mathf.Max(1, worldWidth) * 0.5f;
        }

        private void LateUpdate()
        {
            if (_session == null)
            {
                return;
            }

            _highestCameraY = Mathf.Max(_highestCameraY, _session.PlayerPosition.y + 3f, _minimumY);
            var current = transform.position;
            var targetY = Mathf.Lerp(current.y, _highestCameraY, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            transform.position = new Vector3(_fixedX, targetY, current.z);
        }
    }
}
