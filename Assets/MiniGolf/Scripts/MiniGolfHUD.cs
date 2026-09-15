using UnityEngine;
using UnityEngine.UI;

public sealed class MiniGolfHUD : MonoBehaviour
{
    public Text playerText;
    public Text holeText;
    public Text scoreText;
    public Text timerText;
    public Text messageText;
    public Text instructionsText;
    public Text powerLabel;
    public Image powerFill;

    public void Refresh(MiniGolfGameManager manager)
    {
        if (manager == null)
            return;

        MiniGolfPlayerState player = manager.ActivePlayer;

        if (playerText != null)
            playerText.text = player != null
                ? "Turno: " + player.playerName + "   Jugadores: " + manager.Players.Count
                : "Turno: -";

        if (holeText != null)
            holeText.text = "Hoyo " + manager.CurrentHoleNumber + " / " + Mathf.Max(1, manager.HoleCount);

        if (scoreText != null)
            scoreText.text = manager.BuildHudScoreboard();

        if (instructionsText != null)
        {
            instructionsText.text = manager.WaitingForTurnConfirmation
                ? "Pasale las gafas al jugador indicado   |   ENTER o boton A: jugador listo"
                : "A/D o flechas: apuntar   |   Mantener ESPACIO: fuerza   |   Soltar: golpear\n" +
                  "Teclas 1-4: cantidad de jugadores   |   R: reiniciar";
        }
    }

    public void SetPower(float normalizedPower)
    {
        normalizedPower = Mathf.Clamp01(normalizedPower);

        if (powerFill != null)
        {
            powerFill.fillAmount = normalizedPower;
            powerFill.color = Color.Lerp(new Color(0.1f, 0.9f, 0.25f), new Color(1f, 0.15f, 0.08f), normalizedPower);
        }

        if (powerLabel != null)
            powerLabel.text = "Fuerza " + Mathf.RoundToInt(normalizedPower * 100f) + "%";
    }

    public void SetTimer(bool visible, float seconds)
    {
        if (timerText == null)
            return;

        timerText.gameObject.SetActive(visible);
        timerText.text = visible ? "Tiempo: " + Mathf.CeilToInt(Mathf.Max(0f, seconds)) : string.Empty;
    }

    public void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }

    public void ShowResults(string results)
    {
        if (messageText != null)
            messageText.text = results;
    }
}
