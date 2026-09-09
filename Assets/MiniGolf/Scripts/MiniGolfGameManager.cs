using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class MiniGolfHoleDefinition
{
    public string holeName = "Hoyo 1";
    public GameObject root;
    public Transform ballSpawnPoint;
    public HoleCup cup;
}

[Serializable]
public sealed class MiniGolfPlayerState
{
    public string playerName;
    public Color ballColor = Color.white;
    public int totalStrokes;
    [NonSerialized] public int holeStrokes;
    [NonSerialized] public bool finishedHole;
    [NonSerialized] public BallController ball;
}

public sealed class MiniGolfGameManager : MonoBehaviour
{
    public static MiniGolfGameManager Instance { get; private set; }

    [Header("Partida")]
    [Range(1, 4)] public int numberOfPlayers = 1;
    public bool autoStart = true;
    public bool requireTurnConfirmation = true;
    public float lastPlayerTimeLimit = 60f;

    [Header("Golpe")]
    public float minimumShotImpulse = 0.8f;
    public float maximumShotImpulse = 4.5f;

    [Header("Referencias")]
    public BallController ballPrefab;
    public Transform ballsContainer;
    public MiniGolfHUD hud;
    public List<MiniGolfHoleDefinition> holes = new List<MiniGolfHoleDefinition>();

    private readonly List<MiniGolfPlayerState> players = new List<MiniGolfPlayerState>();
    private int currentPlayerIndex;
    private int currentHoleIndex;
    private bool canShoot;
    private bool gameFinished;
    private bool waitingForTurnConfirmation;
    private bool lastPlayerTimerRunning;
    private bool transitioningHole;
    private float lastPlayerTimeRemaining;

    private static readonly Color[] PlayerColors =
    {
        new Color(0.95f, 0.2f, 0.2f),
        new Color(0.15f, 0.45f, 1f),
        new Color(1f, 0.82f, 0.1f),
        new Color(0.55f, 0.2f, 0.85f)
    };

    public IReadOnlyList<MiniGolfPlayerState> Players => players;
    public int CurrentPlayerIndex => currentPlayerIndex;
    public int CurrentHoleNumber => currentHoleIndex + 1;
    public int HoleCount => holes.Count;
    public bool CanShoot => canShoot && !waitingForTurnConfirmation && !gameFinished && !transitioningHole;
    public bool GameFinished => gameFinished;
    public bool WaitingForTurnConfirmation => waitingForTurnConfirmation;
    public bool LastPlayerTimerRunning => lastPlayerTimerRunning;
    public float LastPlayerTimeRemaining => lastPlayerTimeRemaining;
    public MiniGolfHUD Hud => hud;

    public MiniGolfPlayerState ActivePlayer
    {
        get
        {
            if (currentPlayerIndex < 0 || currentPlayerIndex >= players.Count)
                return null;
            return players[currentPlayerIndex];
        }
    }

    public BallController ActiveBall => ActivePlayer != null ? ActivePlayer.ball : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (autoStart)
            StartNewGame(numberOfPlayers);
    }

    private void Update()
    {
        if (!lastPlayerTimerRunning || waitingForTurnConfirmation || gameFinished || transitioningHole)
            return;

        lastPlayerTimeRemaining -= Time.deltaTime;
        if (hud != null)
            hud.SetTimer(true, lastPlayerTimeRemaining);

        if (lastPlayerTimeRemaining <= 0f)
            HandleLastPlayerTimeout();
    }

    public void StartNewGame(int playerCount)
    {
        CancelInvoke();
        transitioningHole = false;
        lastPlayerTimerRunning = false;
        gameFinished = false;
        waitingForTurnConfirmation = false;
        canShoot = false;
        numberOfPlayers = Mathf.Clamp(playerCount, 1, 4);

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ball != null)
                Destroy(players[i].ball.gameObject);
        }

        players.Clear();

        if (ballPrefab == null || holes == null || holes.Count == 0)
        {
            Debug.LogError("MiniGolf: faltan el prefab de la pelota o los hoyos.");
            if (hud != null)
                hud.SetMessage("Faltan referencias en MiniGolfGameManager.");
            return;
        }

        for (int i = 0; i < numberOfPlayers; i++)
        {
            MiniGolfPlayerState player = new MiniGolfPlayerState
            {
                playerName = "Jugador " + (i + 1),
                ballColor = PlayerColors[i],
                totalStrokes = 0
            };

            BallController ball = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity, ballsContainer);
            ball.name = "Pelota_" + (i + 1);
            ball.Initialize(this, i, player.ballColor);
            player.ball = ball;
            players.Add(player);
        }

        StartHole(0);
    }

    public bool TryHitBall(BallController ball, Vector3 direction, float normalizedPower)
    {
        if (!CanShoot || ball == null || ball != ActiveBall)
            return false;

        normalizedPower = Mathf.Clamp01(normalizedPower);
        if (normalizedPower < 0.03f || direction.sqrMagnitude < 0.0001f)
            return false;

        direction.y = Mathf.Clamp(direction.y, -0.05f, 0.25f);
        direction.Normalize();

        MiniGolfPlayerState player = ActivePlayer;
        player.holeStrokes++;
        canShoot = false;

        float impulse = Mathf.Lerp(minimumShotImpulse, maximumShotImpulse, normalizedPower);
        ball.Strike(direction, impulse, normalizedPower);

        if (hud != null)
        {
            hud.SetPower(0f);
            hud.SetMessage(player.playerName + " golpeo la pelota");
            hud.Refresh(this);
        }

        return true;
    }

    public void OnBallStopped(BallController ball)
    {
        if (gameFinished || transitioningHole || ball == null || ball != ActiveBall)
            return;

        AdvanceTurn();
    }

    public void OnBallHoled(BallController ball)
    {
        if (gameFinished || transitioningHole || ball == null)
            return;

        int playerIndex = ball.PlayerIndex;
        if (playerIndex < 0 || playerIndex >= players.Count)
            return;

        MiniGolfPlayerState player = players[playerIndex];
        if (player.finishedHole)
            return;

        player.finishedHole = true;
        player.totalStrokes += player.holeStrokes;
        ball.SetHoled();
        canShoot = false;

        if (hud != null)
        {
            hud.SetMessage(player.playerName + " completo el hoyo en " + player.holeStrokes + " golpes");
            hud.Refresh(this);
        }

        if (CountUnfinishedPlayers() == 0)
        {
            transitioningHole = true;
            Invoke(nameof(FinishCurrentHole), 1f);
            return;
        }

        AdvanceTurn();
    }

    private void StartHole(int holeIndex)
    {
        currentHoleIndex = holeIndex;
        transitioningHole = false;
        lastPlayerTimerRunning = false;
        lastPlayerTimeRemaining = lastPlayerTimeLimit;

        for (int i = 0; i < holes.Count; i++)
        {
            if (holes[i] != null && holes[i].root != null)
                holes[i].root.SetActive(i == currentHoleIndex);
        }

        MiniGolfHoleDefinition hole = holes[currentHoleIndex];
        if (hole == null || hole.ballSpawnPoint == null)
        {
            Debug.LogError("MiniGolf: el hoyo no tiene BallSpawnPoint.");
            return;
        }

        for (int i = 0; i < players.Count; i++)
        {
            MiniGolfPlayerState player = players[i];
            player.holeStrokes = 0;
            player.finishedHole = false;
            player.ball.gameObject.SetActive(true);
            player.ball.ResetForHole(GetSpawnPosition(hole.ballSpawnPoint.position, i));
        }

        currentPlayerIndex = 0;

        if (hud != null)
        {
            hud.SetTimer(false, 0f);
            hud.SetPower(0f);
        }

        SetActivePlayer(currentPlayerIndex, requireTurnConfirmation && players.Count > 1);
    }

    private Vector3 GetSpawnPosition(Vector3 basePosition, int playerIndex)
    {
        float centeredIndex = playerIndex - (players.Count - 1) * 0.5f;
        return basePosition + Vector3.right * centeredIndex * 0.16f;
    }

    private void AdvanceTurn()
    {
        int unfinishedCount = CountUnfinishedPlayers();
        if (unfinishedCount <= 0)
            return;

        if (unfinishedCount == 1)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (!players[i].finishedHole)
                {
                    bool changedPlayer = i != currentPlayerIndex;
                    SetActivePlayer(i, requireTurnConfirmation && changedPlayer);
                    UpdateLastPlayerTimerState();
                    return;
                }
            }
        }

        for (int step = 1; step <= players.Count; step++)
        {
            int next = (currentPlayerIndex + step) % players.Count;
            if (!players[next].finishedHole)
            {
                bool changedPlayer = next != currentPlayerIndex;
                SetActivePlayer(next, requireTurnConfirmation && changedPlayer);
                UpdateLastPlayerTimerState();
                return;
            }
        }
    }

    private void SetActivePlayer(int playerIndex, bool waitForConfirmation)
    {
        currentPlayerIndex = playerIndex;
        waitingForTurnConfirmation = waitForConfirmation;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ball != null && !players[i].finishedHole)
            {
                bool activeAndReady = i == currentPlayerIndex && !waitingForTurnConfirmation;
                players[i].ball.SetTurnActive(activeAndReady);
            }
        }

        canShoot = !players[currentPlayerIndex].finishedHole && !waitingForTurnConfirmation;

        if (hud != null)
        {
            string message = waitingForTurnConfirmation
                ? "Pasale las gafas a " + players[currentPlayerIndex].playerName + ". Presiona ENTER o el boton A cuando este listo"
                : "Turno de " + players[currentPlayerIndex].playerName;
            hud.SetMessage(message);
            hud.Refresh(this);
        }
    }

    public bool ConfirmTurnReady()
    {
        if (!waitingForTurnConfirmation || gameFinished || transitioningHole || ActivePlayer == null)
            return false;

        waitingForTurnConfirmation = false;
        canShoot = !ActivePlayer.finishedHole;

        if (ActiveBall != null)
            ActiveBall.SetTurnActive(true);

        UpdateLastPlayerTimerState();

        if (hud != null)
        {
            hud.SetMessage(ActivePlayer.playerName + " listo para jugar");
            hud.Refresh(this);
        }

        return true;
    }

    private void UpdateLastPlayerTimerState()
    {
        if (players.Count <= 1 || CountUnfinishedPlayers() != 1 || waitingForTurnConfirmation)
            return;

        if (!lastPlayerTimerRunning)
        {
            lastPlayerTimerRunning = true;
            lastPlayerTimeRemaining = lastPlayerTimeLimit;
            if (hud != null)
                hud.SetMessage("Ultimo jugador: quedan 60 segundos");
        }
    }

    private int CountUnfinishedPlayers()
    {
        int count = 0;
        for (int i = 0; i < players.Count; i++)
        {
            if (!players[i].finishedHole)
                count++;
        }

        return count;
    }

    private void HandleLastPlayerTimeout()
    {
        lastPlayerTimerRunning = false;
        waitingForTurnConfirmation = false;
        MiniGolfPlayerState player = ActivePlayer;
        if (player == null || player.finishedHole)
            return;

        player.finishedHole = true;
        player.holeStrokes += 5;
        player.totalStrokes += player.holeStrokes;
        player.ball.SetHoled();
        canShoot = false;
        transitioningHole = true;

        if (hud != null)
        {
            hud.SetTimer(false, 0f);
            hud.SetMessage("Se termino el tiempo. " + player.playerName + " recibe 5 golpes de penalizacion");
            hud.Refresh(this);
        }

        Invoke(nameof(FinishCurrentHole), 1.5f);
    }

    private void FinishCurrentHole()
    {
        transitioningHole = false;
        lastPlayerTimerRunning = false;

        if (currentHoleIndex + 1 < holes.Count)
        {
            StartHole(currentHoleIndex + 1);
            return;
        }

        FinishGame();
    }

    private void FinishGame()
    {
        gameFinished = true;
        waitingForTurnConfirmation = false;
        canShoot = false;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ball != null)
                players[i].ball.SetTurnActive(false);
        }

        int bestScore = int.MaxValue;
        for (int i = 0; i < players.Count; i++)
            bestScore = Mathf.Min(bestScore, players[i].totalStrokes);

        List<string> winners = new List<string>();
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].totalStrokes == bestScore)
                winners.Add(players[i].playerName);
        }

        string winnerText = winners.Count == 1
            ? "Ganador: " + winners[0]
            : "Empate: " + string.Join(", ", winners);

        if (hud != null)
        {
            hud.SetTimer(false, 0f);
            hud.ShowResults(BuildScoreboard() + "\n" + winnerText + "\nPresiona R para volver a jugar");
            hud.Refresh(this);
        }
    }

    public string BuildHudScoreboard()
    {
        if (players.Count == 0)
            return "Sin jugadores";

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < players.Count; i++)
        {
            if (i > 0)
                builder.Append("  |  ");

            MiniGolfPlayerState player = players[i];
            builder.Append("J")
                .Append(i + 1)
                .Append(" H:")
                .Append(player.holeStrokes)
                .Append(" T:")
                .Append(player.totalStrokes);

            if (player.finishedHole)
                builder.Append(" OK");
        }

        return builder.ToString();
    }

    private string BuildScoreboard()
    {
        StringBuilder builder = new StringBuilder("Partida terminada");
        for (int i = 0; i < players.Count; i++)
        {
            builder.Append("\n")
                .Append(players[i].playerName)
                .Append(": ")
                .Append(players[i].totalStrokes)
                .Append(" golpes");
        }

        return builder.ToString();
    }
}
