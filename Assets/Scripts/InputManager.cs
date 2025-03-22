using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour {
    public static InputManager Instance { get; private set; }
    public static PlayerInput playerInput;

    public event EventHandler OnJumpPressed;
    public event EventHandler OnJumpReleased;
    public event EventHandler OnDashPressed;
    public event EventHandler OnAttacked;

    private GameInput gameInput;
    private Gamepad gamepad;

    private void Awake() {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;

        gameInput = new GameInput();
        gameInput.Player.Enable();
    }

    private void Start() {
        gameInput.Player.Jump.performed += Jump_performed;
        gameInput.Player.Jump.canceled += Jump_canceled;
        gameInput.Player.Dash.performed += Dash_performed;
    }

    private void Dash_performed(InputAction.CallbackContext obj) {
        OnDashPressed?.Invoke(this, EventArgs.Empty);
    }
    private void Jump_canceled(InputAction.CallbackContext obj) {
        OnJumpReleased?.Invoke(this, EventArgs.Empty);
    }
    private void Jump_performed(InputAction.CallbackContext obj) {
        OnJumpPressed?.Invoke(this, EventArgs.Empty);
    }
    public Vector2 GetMoveDirection() {
        return gameInput.Player.Move.ReadValue<Vector2>();
    }
    private void OnDisable() {
        gameInput.Player.Jump.performed -= Jump_performed;
        gameInput.Player.Jump.canceled -= Jump_canceled;
        gameInput.Player.Dash.performed -= Dash_performed;

        gameInput.Player.Disable();
    }

}