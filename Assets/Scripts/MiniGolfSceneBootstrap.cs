using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class MiniGolfSceneBootstrap : MonoBehaviour
{
    private void Awake()
    {
        MiniGolfGameManager manager = GetOrAdd<MiniGolfGameManager>(gameObject);
        MiniGolfHUD hud = GetOrAdd<MiniGolfHUD>(gameObject);

        manager.numberOfPlayers = 1;
        manager.autoStart = true;
        manager.requireTurnConfirmation = true;
        manager.lastPlayerTimeLimit = 60f;
        manager.minimumShotImpulse = 0.8f;
        manager.maximumShotImpulse = 4.5f;
        manager.hud = hud;

        GameObject ballsObject = GameObject.Find("Pelotas");
        if (ballsObject == null)
            ballsObject = new GameObject("Pelotas");
        manager.ballsContainer = ballsObject.transform;

        GameObject ballTemplate = GameObject.Find("Pelota_Plantilla");
        if (ballTemplate == null)
        {
            Debug.LogError("Minigolf VR: falta Pelota_Plantilla en la escena.");
            return;
        }

        SphereCollider ballCollider = GetOrAdd<SphereCollider>(ballTemplate);
        ballCollider.radius = 0.5f;

        Rigidbody ballBody = GetOrAdd<Rigidbody>(ballTemplate);
        ballBody.mass = 0.045f;
        ballBody.linearDamping = 0.35f;
        ballBody.angularDamping = 0.25f;
        ballBody.interpolation = RigidbodyInterpolation.Interpolate;
        ballBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        ballBody.maxAngularVelocity = 20f;

        BallController ballController = GetOrAdd<BallController>(ballTemplate);
        ballTemplate.SetActive(false);
        manager.ballPrefab = ballController;

        GameObject holeRoot = GameObject.Find("Hoyo_01");
        GameObject spawnObject = GameObject.Find("BallSpawnPoint");
        GameObject cupObject = GameObject.Find("HoleTrigger");

        if (holeRoot == null || spawnObject == null || cupObject == null)
        {
            Debug.LogError("Minigolf VR: faltan objetos del hoyo en Assets/Scenes/MinigolfVR.");
            return;
        }

        SphereCollider cupCollider = GetOrAdd<SphereCollider>(cupObject);
        cupCollider.isTrigger = true;
        cupCollider.radius = 0.28f;

        HoleCup cup = GetOrAdd<HoleCup>(cupObject);
        cup.gameManager = manager;

        manager.holes = new List<MiniGolfHoleDefinition>
        {
            new MiniGolfHoleDefinition
            {
                holeName = "Hoyo 1",
                root = holeRoot,
                ballSpawnPoint = spawnObject.transform,
                cup = cup
            }
        };

        DesktopShotController desktop = GetOrAdd<DesktopShotController>(gameObject);
        desktop.gameManager = manager;

        LineRenderer aimLine = GetOrAdd<LineRenderer>(gameObject);
        aimLine.positionCount = 2;
        aimLine.useWorldSpace = true;
        aimLine.startWidth = 0.035f;
        aimLine.endWidth = 0.015f;
        aimLine.startColor = new Color(1f, 0.9f, 0.1f);
        aimLine.endColor = new Color(1f, 0.45f, 0.05f);
        aimLine.enabled = false;

        Shader aimShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (aimShader == null)
            aimShader = Shader.Find("Sprites/Default");
        if (aimShader != null)
        {
            aimLine.material = new Material(aimShader);
            aimLine.material.color = Color.white;
        }

        desktop.aimLine = aimLine;

        GetOrAdd<VrTurnReadyInput>(gameObject);

        GameObject vrObject = GameObject.Find("VR_Player");
        if (vrObject == null)
            vrObject = new GameObject("VR_Player");

        VRPlayerRig vrRig = GetOrAdd<VRPlayerRig>(vrObject);
        vrRig.gameManager = manager;

        GameObject cameraObject = GameObject.Find("DesktopCamera");
        if (cameraObject != null)
            vrRig.desktopCamera = cameraObject.GetComponent<Camera>();
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
}
