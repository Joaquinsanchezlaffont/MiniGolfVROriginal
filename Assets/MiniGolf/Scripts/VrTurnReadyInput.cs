using UnityEngine;
using UnityEngine.InputSystem;

public sealed class VrTurnReadyInput : MonoBehaviour
{
    private InputAction readyAction;

    private void OnEnable()
    {
        readyAction = new InputAction("Jugador listo", InputActionType.Button);
        readyAction.AddBinding("<XRController>{RightHand}/primaryButton");
        readyAction.performed += OnReadyPressed;
        readyAction.Enable();
    }

    private void OnDisable()
    {
        if (readyAction == null)
            return;

        readyAction.performed -= OnReadyPressed;
        readyAction.Disable();
        readyAction.Dispose();
        readyAction = null;
    }

    private void OnReadyPressed(InputAction.CallbackContext context)
    {
        if (MiniGolfGameManager.Instance != null)
            MiniGolfGameManager.Instance.ConfirmTurnReady();
    }
}
