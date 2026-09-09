using UnityEngine;
using UnityEngine.InputSystem;

public sealed class DesktopShotController : MonoBehaviour
{
    public MiniGolfGameManager gameManager;
    public LineRenderer aimLine;
    public float chargeSpeed = 0.75f;
    public float aimRotationSpeed = 75f;

    private float charge;
    private float chargeDirection = 1f;
    private float aimAngle;
    private bool charging;

    private void Update()
    {
        if (gameManager == null || Keyboard.current == null)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard.digit1Key.wasPressedThisFrame) gameManager.StartNewGame(1);
        if (keyboard.digit2Key.wasPressedThisFrame) gameManager.StartNewGame(2);
        if (keyboard.digit3Key.wasPressedThisFrame) gameManager.StartNewGame(3);
        if (keyboard.digit4Key.wasPressedThisFrame) gameManager.StartNewGame(4);

        if (keyboard.rKey.wasPressedThisFrame)
        {
            gameManager.StartNewGame(gameManager.numberOfPlayers);
            return;
        }

        if (keyboard.enterKey.wasPressedThisFrame && gameManager.ConfirmTurnReady())
            return;

        float aimInput = 0f;
        if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) aimInput -= 1f;
        if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) aimInput += 1f;
        aimAngle += aimInput * aimRotationSpeed * Time.deltaTime;

        UpdateAimLine();

        if (!gameManager.CanShoot || gameManager.GameFinished)
        {
            charging = false;
            charge = 0f;
            if (gameManager.Hud != null)
                gameManager.Hud.SetPower(0f);
            return;
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            charging = true;
            charge = 0.05f;
            chargeDirection = 1f;
        }

        if (charging && keyboard.spaceKey.isPressed)
        {
            charge += chargeDirection * chargeSpeed * Time.deltaTime;

            if (charge >= 1f)
            {
                charge = 1f;
                chargeDirection = -1f;
            }
            else if (charge <= 0.05f)
            {
                charge = 0.05f;
                chargeDirection = 1f;
            }

            if (gameManager.Hud != null)
                gameManager.Hud.SetPower(charge);
        }

        if (charging && keyboard.spaceKey.wasReleasedThisFrame)
        {
            Vector3 direction = Quaternion.Euler(0f, aimAngle, 0f) * Vector3.forward;
            gameManager.TryHitBall(gameManager.ActiveBall, direction, charge);
            charging = false;
            charge = 0f;
        }
    }

    private void UpdateAimLine()
    {
        if (aimLine == null)
            return;

        BallController activeBall = gameManager.ActiveBall;
        bool visible = activeBall != null && gameManager.CanShoot && !gameManager.GameFinished;
        aimLine.enabled = visible;

        if (!visible)
            return;

        Vector3 start = activeBall.transform.position + Vector3.up * 0.08f;
        Vector3 direction = Quaternion.Euler(0f, aimAngle, 0f) * Vector3.forward;
        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, start + direction * 1.4f);
    }
}
