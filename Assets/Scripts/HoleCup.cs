using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class HoleCup : MonoBehaviour
{
    public MiniGolfGameManager gameManager;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        BallController ball = other.GetComponentInParent<BallController>();
        if (ball == null)
            return;

        MiniGolfGameManager manager = gameManager != null
            ? gameManager
            : MiniGolfGameManager.Instance;

        if (manager != null)
            manager.OnBallHoled(ball);
    }
}
