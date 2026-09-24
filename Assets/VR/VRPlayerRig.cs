using UnityEngine;
using UnityEngine.XR;

public sealed class VRPlayerRig : MonoBehaviour
{
    public MiniGolfGameManager gameManager;
    public Camera desktopCamera;
    public Vector3 fallbackHeadPosition = new Vector3(0f, 1.65f, 0f);
    public Vector3 fallbackLeftHandPosition = new Vector3(-0.25f, 1.25f, 0.35f);
    public Vector3 fallbackRightHandPosition = new Vector3(0.25f, 1.25f, 0.35f);

    private Transform head;
    private Transform leftHand;
    private Transform rightHand;
    private Camera vrCamera;
    private bool rigReady;
    private float nextDeviceCheck;

    private void Update()
    {
        if (!rigReady)
        {
            if (Time.unscaledTime >= nextDeviceCheck)
            {
                nextDeviceCheck = Time.unscaledTime + 1f;
                TryCreateRig();
            }

            return;
        }

        UpdateNodePose(XRNode.Head, head, fallbackHeadPosition);
        UpdateNodePose(XRNode.LeftHand, leftHand, fallbackLeftHandPosition);
        UpdateNodePose(XRNode.RightHand, rightHand, fallbackRightHandPosition);
    }

    private void TryCreateRig()
    {
        InputDevice headset = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (!headset.isValid)
            return;

        GameObject headObject = new GameObject("Cabeza VR");
        headObject.transform.SetParent(transform, false);
        head = headObject.transform;
        head.localPosition = fallbackHeadPosition;

        vrCamera = headObject.AddComponent<Camera>();
        vrCamera.nearClipPlane = 0.05f;
        vrCamera.farClipPlane = 500f;
        vrCamera.stereoTargetEye = StereoTargetEyeMask.Both;
        headObject.AddComponent<AudioListener>();

        leftHand = CreateTrackedObject("Control Izquierdo", fallbackLeftHandPosition);
        rightHand = CreateTrackedObject("Control Derecho", fallbackRightHandPosition);
        CreatePutter(rightHand);

        if (desktopCamera != null)
        {
            desktopCamera.enabled = false;
            AudioListener desktopListener = desktopCamera.GetComponent<AudioListener>();
            if (desktopListener != null)
                desktopListener.enabled = false;
        }

        rigReady = true;
    }

    private Transform CreateTrackedObject(string objectName, Vector3 fallbackPosition)
    {
        GameObject trackedObject = new GameObject(objectName);
        trackedObject.transform.SetParent(transform, false);
        trackedObject.transform.localPosition = fallbackPosition;
        return trackedObject.transform;
    }

    private void CreatePutter(Transform hand)
    {
        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "Palo VR";
        shaft.transform.SetParent(hand, false);
        shaft.transform.localPosition = new Vector3(0f, -0.28f, 0.13f);
        shaft.transform.localScale = new Vector3(0.018f, 0.32f, 0.018f);
        Collider shaftCollider = shaft.GetComponent<Collider>();
        if (shaftCollider != null)
            shaftCollider.enabled = false;

        GameObject headObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headObject.name = "Cabeza del palo";
        headObject.transform.SetParent(hand, false);
        headObject.transform.localPosition = new Vector3(0f, -0.62f, 0.15f);
        headObject.transform.localScale = new Vector3(0.18f, 0.07f, 0.08f);

        BoxCollider hitCollider = headObject.GetComponent<BoxCollider>();
        hitCollider.isTrigger = true;

        ClubHead clubHead = headObject.AddComponent<ClubHead>();
        clubHead.gameManager = gameManager;
        clubHead.velocitySource = hand;
        clubHead.minimumSwingSpeed = 0.25f;
        clubHead.maximumSwingSpeed = 4f;
    }

    private static void UpdateNodePose(XRNode node, Transform target, Vector3 fallbackPosition)
    {
        if (target == null)
            return;

        InputDevice device = InputDevices.GetDeviceAtXRNode(node);

        if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
            target.localPosition = position;
        else
            target.localPosition = fallbackPosition;

        if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            target.localRotation = rotation;
    }

    private void OnDestroy()
    {
        if (desktopCamera == null)
            return;

        desktopCamera.enabled = true;
        AudioListener desktopListener = desktopCamera.GetComponent<AudioListener>();
        if (desktopListener != null)
            desktopListener.enabled = true;
    }
}
