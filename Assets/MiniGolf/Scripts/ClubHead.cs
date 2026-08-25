using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class ClubHead : MonoBehaviour
{
    public MiniGolfGameManager gameManager;
    public Transform velocitySource;
    public float minimumSwingSpeed = 0.25f;
    public float maximumSwingSpeed = 4f;
    [Range(0f, 1f)] public float velocitySmoothing = 0.45f;

    private Vector3 previousPosition;
    private Vector3 measuredVelocity;

    private void OnEnable()
    {
        Transform source = velocitySource != null ? velocitySource : transform;
        previousPosition = source.position;
        measuredVelocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        Transform source = velocitySource != null ? velocitySource : transform;
        Vector3 instantVelocity = (source.position - previousPosition) / Time.fixedDeltaTime;
        measuredVelocity = Vector3.Lerp(measuredVelocity, instantVelocity, 1f - velocitySmoothing);
        previousPosition = source.position;

        MiniGolfGameManager manager = gameManager != null
            ? gameManager
            : MiniGolfGameManager.Instance;

        if (manager != null && manager.Hud != null && manager.CanShoot)
        {
            float power = Mathf.InverseLerp(minimumSwingSpeed, maximumSwingSpeed, measuredVelocity.magnitude);
            manager.Hud.SetPower(power);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        BallController ball = other.GetComponentInParent<BallController>();
        if (ball == null)
            return;

        MiniGolfGameManager manager = gameManager != null
            ? gameManager
            : MiniGolfGameManager.Instance;

        if (manager == null)
            return;

        float swingSpeed = measuredVelocity.magnitude;
        float power = Mathf.InverseLerp(minimumSwingSpeed, maximumSwingSpeed, swingSpeed);
        if (power <= 0f)
            return;

        manager.TryHitBall(ball, measuredVelocity.normalized, power);
    }
}
