using UnityEngine;

public sealed class MiniGolfHUD : MonoBehaviour
{
    public bool drawRuntimeHud = true;

    private string playerLine = "Turno: -";
    private string holeLine = "Hoyo 1 / 1";
    private string scoreLine = "Sin jugadores";
    private string timerLine = "";
    private string messageLine = "Minigolf VR";
    private string instructionsLine =
        "A/D o flechas: apuntar | Mantener ESPACIO: fuerza | Soltar: golpear\n" +
        "Teclas 1-4: jugadores | ENTER: confirmar turno | R: reiniciar";
    private float power;
    private bool timerVisible;

    private GUIStyle titleStyle;
    private GUIStyle textStyle;
    private GUIStyle smallStyle;
    private GUIStyle centerStyle;

    public void Refresh(MiniGolfGameManager manager)
    {
        if (manager == null)
            return;

        MiniGolfPlayerState player = manager.ActivePlayer;
        playerLine = player != null
            ? "Turno: " + player.playerName + "   Jugadores: " + manager.Players.Count
            : "Turno: -";
        holeLine = "Hoyo " + manager.CurrentHoleNumber + " / " + Mathf.Max(1, manager.HoleCount);
        scoreLine = manager.BuildHudScoreboard();

        instructionsLine = manager.WaitingForTurnConfirmation
            ? "Pasale las gafas al jugador indicado | ENTER o boton A: jugador listo"
            : "A/D o flechas: apuntar | Mantener ESPACIO: fuerza | Soltar: golpear\n" +
              "Teclas 1-4: jugadores | R: reiniciar";
    }

    public void SetPower(float normalizedPower)
    {
        power = Mathf.Clamp01(normalizedPower);
    }

    public void SetTimer(bool visible, float seconds)
    {
        timerVisible = visible;
        timerLine = visible ? "Tiempo: " + Mathf.CeilToInt(Mathf.Max(0f, seconds)) : "";
    }

    public void SetMessage(string message)
    {
        messageLine = message;
    }

    public void ShowResults(string results)
    {
        messageLine = results;
    }

    private void OnGUI()
    {
        if (!drawRuntimeHud)
            return;

        EnsureStyles();

        float panelWidth = Mathf.Min(780f, Screen.width - 30f);
        float panelHeight = timerVisible ? 235f : 210f;
        Rect panel = new Rect(15f, 15f, panelWidth, panelHeight);

        Color previousColor = GUI.color;
        GUI.color = new Color(0.025f, 0.04f, 0.06f, 0.9f);
        GUI.Box(panel, GUIContent.none);
        GUI.color = previousColor;

        GUI.Label(new Rect(30f, 25f, panelWidth - 30f, 28f), "MINIGOLF VR", titleStyle);
        GUI.Label(new Rect(30f, 58f, panelWidth - 30f, 24f), playerLine + "   |   " + holeLine, textStyle);
        GUI.Label(new Rect(30f, 84f, panelWidth - 30f, 24f), scoreLine, smallStyle);

        GUI.Label(new Rect(30f, 112f, 75f, 24f), "Fuerza", textStyle);
        Rect powerBackground = new Rect(105f, 115f, panelWidth - 135f, 18f);
        GUI.color = new Color(0.15f, 0.17f, 0.2f, 1f);
        GUI.DrawTexture(powerBackground, Texture2D.whiteTexture);
        GUI.color = Color.Lerp(new Color(0.1f, 0.9f, 0.25f), new Color(1f, 0.12f, 0.05f), power);
        GUI.DrawTexture(new Rect(powerBackground.x, powerBackground.y, powerBackground.width * power, powerBackground.height), Texture2D.whiteTexture);
        GUI.color = previousColor;
        GUI.Label(powerBackground, Mathf.RoundToInt(power * 100f) + "%", centerStyle);

        GUI.Label(new Rect(30f, 143f, panelWidth - 30f, timerVisible ? 48f : 30f), messageLine, textStyle);

        if (timerVisible)
            GUI.Label(new Rect(30f, 176f, panelWidth - 30f, 24f), timerLine, textStyle);

        GUI.Label(new Rect(30f, timerVisible ? 202f : 170f, panelWidth - 30f, 42f), instructionsLine, smallStyle);
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.35f, 1f, 0.45f) }
        };

        textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            normal = { textColor = Color.white },
            wordWrap = true
        };

        smallStyle = new GUIStyle(textStyle)
        {
            fontSize = 13
        };

        centerStyle = new GUIStyle(smallStyle)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
    }
}
