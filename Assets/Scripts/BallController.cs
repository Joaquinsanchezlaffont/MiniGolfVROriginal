using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public sealed class BallController : MonoBehaviour
{
    [Header("Deteccion de pelota quieta")]
    public float stopSpeed = 0.06f;
    public float stopAngularSpeed = 0.6f;
    public float timeToConfirmStop = 0.7f;
    public float outOfBoundsHeight = -2f;

    private Rigidbody body;
    private MiniGolfGameManager gameManager;
    private float stoppedTime;
    private bool waitingToStop;
    private bool holed;
    private Vector3 lastSafePosition;

    public int PlayerIndex { get; private set; } = -1;
    public float LastShotPower { get; private set; }
    public Rigidbody Body => body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    public void Initialize(MiniGolfGameManager manager, int playerIndex, Color color)
    {
        gameManager = manager;
        PlayerIndex = playerIndex;

        Renderer ballRenderer = GetComponentInChildren<Renderer>();
        if (ballRenderer != null)
            ballRenderer.material.color = color;
    }

    public void ResetForHole(Vector3 position)
    {
        if (body == null)
            body = GetComponent<Rigidbody>();

        holed = false;
        waitingToStop = false;
        stoppedTime = 0f;
        LastShotPower = 0f;
        transform.position = position;
        transform.rotation = Quaternion.identity;
        body.isKinematic = true;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        lastSafePosition = position;
    }

    public void SetTurnActive(bool isActive)
    {
        if (body == null || holed)
            return;

        body.isKinematic = !isActive;
        if (!isActive)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    public void Strike(Vector3 direction, float impulse, float normalizedPower)
    {
        if (holed)
            return;

        body.isKinematic = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        LastShotPower = normalizedPower;
        waitingToStop = true;
        stoppedTime = 0f;
        body.AddForce(direction * impulse, ForceMode.Impulse);
    }

    public void SetHoled()
    {
        holed = true;
        waitingToStop = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
        gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (holed || body == null || body.isKinematic)
            return;

        if (transform.position.y < outOfBoundsHeight)
        {
            ResetAfterOutOfBounds();
            return;
        }

        if (!waitingToStop)
            return;

        bool isNearlyStopped =
            body.linearVelocity.sqrMagnitude <= stopSpeed * stopSpeed &&
            body.angularVelocity.sqrMagnitude <= stopAngularSpeed * stopAngularSpeed;

        if (isNearlyStopped)
        {
            stoppedTime += Time.fixedDeltaTime;
            if (stoppedTime >= timeToConfirmStop)
            {
                waitingToStop = false;
                stoppedTime = 0f;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                lastSafePosition = transform.position;
                gameManager.OnBallStopped(this);
            }
        }
        else
        {
            stoppedTime = 0f;
        }
    }

    private void ResetAfterOutOfBounds()
    {
        transform.position = lastSafePosition;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        waitingToStop = false;
        stoppedTime = 0f;
        gameManager.OnBallStopped(this);
    }
}
