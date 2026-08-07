using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OneStep.Gameplay.Overworld
{
    public sealed class DiscreteInputDriver : MonoBehaviour
    {
        private AdventureConfiguration _configuration;
        private Vector2Int _heldDirection;
        private float _nextRepeatTime;

        public event Action<Vector2Int> ActionRequested;

        public bool InputEnabled { get; set; }

        public void Configure(AdventureConfiguration configuration) => _configuration = configuration;

        private void Update()
        {
            if (!InputEnabled || _configuration == null)
            {
                _heldDirection = Vector2Int.zero;
                return;
            }

            if (Keyboard.current?.spaceKey.wasPressedThisFrame == true || Gamepad.current?.startButton.wasPressedThisFrame == true)
            {
                ActionRequested?.Invoke(Vector2Int.zero);
            }

            var direction = ReadDirection();
            if (direction == Vector2Int.zero)
            {
                _heldDirection = Vector2Int.zero;
                return;
            }

            if (direction != _heldDirection)
            {
                _heldDirection = direction;
                ActionRequested?.Invoke(direction);
                _nextRepeatTime = Time.unscaledTime + _configuration.JoystickInitialRepeatDelay;
                return;
            }

            if (Time.unscaledTime >= _nextRepeatTime)
            {
                ActionRequested?.Invoke(direction);
                _nextRepeatTime = Time.unscaledTime + _configuration.KeyboardRepeatRate;
            }
        }

        private static Vector2Int ReadDirection()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    return Vector2Int.up;
                }
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    return Vector2Int.down;
                }
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    return Vector2Int.left;
                }
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    return Vector2Int.right;
                }
            }

            var gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return Vector2Int.zero;
            }

            var value = gamepad.dpad.ReadValue();
            if (value.sqrMagnitude < 0.25f)
            {
                value = gamepad.leftStick.ReadValue();
            }

            if (value.sqrMagnitude < 0.25f)
            {
                return Vector2Int.zero;
            }

            return Mathf.Abs(value.x) > Mathf.Abs(value.y)
                ? new Vector2Int(value.x > 0f ? 1 : -1, 0)
                : new Vector2Int(0, value.y > 0f ? 1 : -1);
        }
    }
}
