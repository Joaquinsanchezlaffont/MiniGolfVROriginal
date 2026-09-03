using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

public static class MiniGolfVrRigBuilder
{
    private const string MenuPath = "MiniGolf VR/Agregar jugador VR (tutorial)";

    [MenuItem(MenuPath)]
    public static void AddVrPlayer()
    {
        XROrigin existingOrigin = Object.FindFirstObjectByType<XROrigin>();
        if (existingOrigin != null)
        {
            Selection.activeGameObject = existingOrigin.gameObject;
            EditorGUIUtility.PingObject(existingOrigin.gameObject);
            EditorUtility.DisplayDialog("Jugador VR", "La escena ya tiene un XR Origin.", "Listo");
            return;
        }

        DisableDesktopCamera();

        GameObject interactionManagerObject = new GameObject("XR Interaction Manager");
        interactionManagerObject.AddComponent<XRInteractionManager>();
        Undo.RegisterCreatedObjectUndo(interactionManagerObject, "Crear XR Interaction Manager");

        GameObject originObject = new GameObject("XR Origin (VR)");
        originObject.transform.position = new Vector3(0f, 0f, -3.3f);
        XROrigin origin = originObject.AddComponent<XROrigin>();
        Undo.RegisterCreatedObjectUndo(originObject, "Crear jugador VR");

        GameObject cameraOffset = new GameObject("Camera Offset");
        cameraOffset.transform.SetParent(originObject.transform, false);

        Camera vrCamera = CreateVrCamera(cameraOffset.transform);
        CreateTrackedController(cameraOffset.transform, true);
        CreateTrackedController(cameraOffset.transform, false);

        origin.CameraFloorOffsetObject = cameraOffset;
        origin.Camera = vrCamera;
        origin.CameraYOffset = 0f;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Selection.activeGameObject = originObject;
        EditorGUIUtility.PingObject(originObject);

        EditorUtility.DisplayDialog(
            "Jugador VR agregado",
            "Se creo el XR Origin con la camara y los controles izquierdo y derecho. Activa OpenXR para Windows antes de probar con las gafas.",
            "Listo");
    }

    [MenuItem(MenuPath, true)]
    private static bool CanAddVrPlayer()
    {
        return !EditorApplication.isPlaying;
    }

    private static void DisableDesktopCamera()
    {
        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i].CompareTag("MainCamera") && cameras[i].gameObject.activeSelf)
            {
                Undo.RecordObject(cameras[i].gameObject, "Desactivar camara de escritorio");
                cameras[i].gameObject.SetActive(false);
            }
        }
    }

    private static Camera CreateVrCamera(Transform parent)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(parent, false);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 100f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<UniversalAdditionalCameraData>();

        AddTrackedPose(
            cameraObject,
            "Head Position",
            "<XRHMD>/centerEyePosition",
            "Head Rotation",
            "<XRHMD>/centerEyeRotation",
            "Head Tracking",
            "<XRHMD>/trackingState");

        return camera;
    }

    private static void CreateTrackedController(Transform parent, bool leftHand)
    {
        string handName = leftHand ? "Left Controller" : "Right Controller";
        string handUsage = leftHand ? "LeftHand" : "RightHand";

        GameObject controller = new GameObject(handName);
        controller.transform.SetParent(parent, false);
        controller.transform.localPosition = leftHand
            ? new Vector3(-0.2f, 1.2f, 0.4f)
            : new Vector3(0.2f, 1.2f, 0.4f);

        AddTrackedPose(
            controller,
            handName + " Position",
            "<XRController>{" + handUsage + "}/devicePosition",
            handName + " Rotation",
            "<XRController>{" + handUsage + "}/deviceRotation",
            handName + " Tracking",
            "<XRController>{" + handUsage + "}/trackingState");

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Controller Visual";
        visual.transform.SetParent(controller.transform, false);
        visual.transform.localScale = new Vector3(0.08f, 0.12f, 0.16f);

        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
            Object.DestroyImmediate(visualCollider);
    }

    private static void AddTrackedPose(
        GameObject target,
        string positionName,
        string positionBinding,
        string rotationName,
        string rotationBinding,
        string trackingName,
        string trackingBinding)
    {
        TrackedPoseDriver driver = target.AddComponent<TrackedPoseDriver>();
        driver.positionInput = CreateAction(positionName, "Vector3", positionBinding);
        driver.rotationInput = CreateAction(rotationName, "Quaternion", rotationBinding);
        driver.trackingStateInput = CreateAction(trackingName, "Integer", trackingBinding);
        driver.ignoreTrackingState = false;
    }

    private static InputActionProperty CreateAction(string name, string controlType, string binding)
    {
        InputAction action = new InputAction(name, InputActionType.Value);
        action.expectedControlType = controlType;
        action.AddBinding(binding);
        return new InputActionProperty(action);
    }
}
