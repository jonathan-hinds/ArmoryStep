using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OneStep.Input
{
    public sealed class GameInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;

        public event Action<Vector2> MoveChanged;
        public event Action Confirmed;
        public event Action Cancelled;
        public event Action PrimaryAction;
        public event Action WaitRequested;

        public Vector2 Move { get; private set; }
        public Vector2 PointerPosition { get; private set; }
        public string LastControlPath { get; private set; } = "Waiting for input";

        private InputAction _move;
        private InputAction _confirm;
        private InputAction _cancel;
        private InputAction _pointer;
        private InputAction _primary;
        private InputAction _wait;

        public void Configure(InputActionAsset inputActions) => actions = inputActions;

        private void OnEnable()
        {
            if (actions == null)
            {
                Debug.LogError("GameInputReader requires an InputActionAsset.", this);
                return;
            }

            _move = actions.FindAction("Gameplay/Move", true);
            _confirm = actions.FindAction("Gameplay/Confirm", true);
            _cancel = actions.FindAction("Gameplay/Cancel", true);
            _pointer = actions.FindAction("Gameplay/PointerPosition", true);
            _primary = actions.FindAction("Gameplay/PrimaryAction", true);
            _wait = actions.FindAction("Gameplay/Wait", true);

            _move.performed += OnMove;
            _move.canceled += OnMove;
            _confirm.performed += OnConfirm;
            _cancel.performed += OnCancel;
            _pointer.performed += OnPointer;
            _primary.performed += OnPrimary;
            _wait.performed += OnWait;
            actions.Enable();
        }

        private void OnDisable()
        {
            if (_move == null)
            {
                return;
            }

            _move.performed -= OnMove;
            _move.canceled -= OnMove;
            _confirm.performed -= OnConfirm;
            _cancel.performed -= OnCancel;
            _pointer.performed -= OnPointer;
            _primary.performed -= OnPrimary;
            _wait.performed -= OnWait;
            actions.Disable();
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            Move = context.ReadValue<Vector2>();
            Record(context);
            MoveChanged?.Invoke(Move);
        }

        private void OnConfirm(InputAction.CallbackContext context)
        {
            Record(context);
            Confirmed?.Invoke();
        }

        private void OnCancel(InputAction.CallbackContext context)
        {
            Record(context);
            Cancelled?.Invoke();
        }

        private void OnPointer(InputAction.CallbackContext context)
        {
            PointerPosition = context.ReadValue<Vector2>();
            Record(context);
        }

        private void OnPrimary(InputAction.CallbackContext context)
        {
            Record(context);
            PrimaryAction?.Invoke();
        }

        private void OnWait(InputAction.CallbackContext context)
        {
            Record(context);
            WaitRequested?.Invoke();
        }

        private void Record(InputAction.CallbackContext context)
        {
            LastControlPath = context.control?.path ?? context.action.name;
        }
    }
}
