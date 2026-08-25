using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MiniGolfPrototypeBuilder
{
    private const string ScenePath = "Assets/Scenes/MinigolfVR.unity";
    private const string RootPath = "Assets/MiniGolf";
    private const string GeneratedPath = RootPath + "/Generated";
    private const string MaterialsPath = GeneratedPath + "/Materials";
    private const string PrefabsPath = GeneratedPath + "/Prefabs";
    private const string BallPrefabPath = PrefabsPath + "/PlayerBall.prefab";

    [MenuItem("MiniGolf VR/Crear prototipo jugable")]
    public static void CreatePrototype()
    {
        bool continueBuild = EditorUtility.DisplayDialog(
            "Crear prototipo de Minigolf VR",
            "Se va a reemplazar la escena MinigolfVR con el mapa de prueba. Los modelos de Blender se pueden agregar despues.",
            "Crear",
            "Cancelar");

        if (!continueBuild)
            return;

        EnsureFolders();

        Material grassMaterial = CreateMaterial(MaterialsPath + "/Grass.mat", new Color(0.12f, 0.58f, 0.2f));
        Material wallMaterial = CreateMaterial(MaterialsPath + "/Walls.mat", new Color(0.88f, 0.88f, 0.9f));
        Material obstacleMaterial = CreateMaterial(MaterialsPath + "/Obstacle.mat", new Color(0.18f, 0.34f, 0.62f));
        Material holeMaterial = CreateMaterial(MaterialsPath + "/Hole.mat", new Color(0.015f, 0.015f, 0.015f));
        Material ballMaterial = CreateMaterial(MaterialsPath + "/Ball.mat", Color.white);
        Material aimMaterial = CreateUnlitMaterial(MaterialsPath + "/Aim.mat", new Color(1f, 0.88f, 0.1f));

        BallController ballPrefab = CreateBallPrefab(ballMaterial);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject holeRoot = new GameObject("Hole_01");
        CreateCube("Floor", holeRoot.transform, new Vector3(0f, -0.1f, 0f), new Vector3(4f, 0.2f, 8f), grassMaterial);
        CreateCube("Wall_Left", holeRoot.transform, new Vector3(-2.1f, 0.25f, 0f), new Vector3(0.2f, 0.5f, 8.4f), wallMaterial);
        CreateCube("Wall_Right", holeRoot.transform, new Vector3(2.1f, 0.25f, 0f), new Vector3(0.2f, 0.5f, 8.4f), wallMaterial);
        CreateCube("Wall_Back", holeRoot.transform, new Vector3(0f, 0.25f, 4.1f), new Vector3(4.4f, 0.5f, 0.2f), wallMaterial);
        CreateCube("Wall_Front", holeRoot.transform, new Vector3(0f, 0.25f, -4.1f), new Vector3(4.4f, 0.5f, 0.2f), wallMaterial);
        CreateCube("Obstacle", holeRoot.transform, new Vector3(0f, 0.3f, 0.4f), new Vector3(0.35f, 0.6f, 1.8f), obstacleMaterial);

        GameObject ramp = CreateCube("Ramp", holeRoot.transform, new Vector3(-1.15f, 0.12f, 1.5f), new Vector3(1.2f, 0.18f, 1.8f), obstacleMaterial);
        ramp.transform.rotation = Quaternion.Euler(-8f, 0f, 0f);

        GameObject spawn = new GameObject("BallSpawnPoint");
        spawn.transform.SetParent(holeRoot.transform);
        spawn.transform.position = new Vector3(0f, 0.07f, -3.25f);

        GameObject holeVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        holeVisual.name = "HoleVisual";
        holeVisual.transform.SetParent(holeRoot.transform);
        holeVisual.transform.position = new Vector3(0f, 0.006f, 3.25f);
        holeVisual.transform.localScale = new Vector3(0.38f, 0.006f, 0.38f);
        Object.DestroyImmediate(holeVisual.GetComponent<Collider>());
        ApplyMaterial(holeVisual, holeMaterial);

        GameObject cupObject = new GameObject("HoleTrigger");
        cupObject.transform.SetParent(holeRoot.transform);
        cupObject.transform.position = new Vector3(0f, 0.07f, 3.25f);
        SphereCollider cupCollider = cupObject.AddComponent<SphereCollider>();
        cupCollider.isTrigger = true;
        cupCollider.radius = 0.18f;
        HoleCup cup = cupObject.AddComponent<HoleCup>();

        GameObject ballsContainer = new GameObject("Balls");

        GameObject managerObject = new GameObject("GameManager");
        MiniGolfGameManager manager = managerObject.AddComponent<MiniGolfGameManager>();
        manager.numberOfPlayers = 1;
        manager.autoStart = true;
        manager.ballPrefab = ballPrefab;
        manager.ballsContainer = ballsContainer.transform;
        manager.holes = new List<MiniGolfHoleDefinition>
        {
            new MiniGolfHoleDefinition
            {
                holeName = "Hoyo 1",
                root = holeRoot,
                ballSpawnPoint = spawn.transform,
                cup = cup
            }
        };
        cup.gameManager = manager;

        MiniGolfHUD hud = CreateHud();
        manager.hud = hud;

        GameObject desktopControllerObject = new GameObject("DesktopShotController");
        DesktopShotController desktopController = desktopControllerObject.AddComponent<DesktopShotController>();
        desktopController.gameManager = manager;

        LineRenderer aimLine = desktopControllerObject.AddComponent<LineRenderer>();
        aimLine.positionCount = 2;
        aimLine.startWidth = 0.025f;
        aimLine.endWidth = 0.012f;
        aimLine.material = aimMaterial;
        aimLine.useWorldSpace = true;
        desktopController.aimLine = aimLine;

        CreateDesktopCamera();
        CreateDirectionalLight();
        CreateVrPutterPlaceholder(manager);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = managerObject;
        EditorGUIUtility.PingObject(managerObject);

        EditorUtility.DisplayDialog(
            "Prototipo creado",
            "Presiona Play. Usa A/D para apuntar, manten ESPACIO para cargar fuerza y suelta para golpear. Las teclas 1 a 4 cambian la cantidad de jugadores.",
            "Listo");
    }

    private static void EnsureFolders()
    {
        CreateFolderIfMissing("Assets", "MiniGolf");
        CreateFolderIfMissing(RootPath, "Generated");
        CreateFolderIfMissing(GeneratedPath, "Materials");
        CreateFolderIfMissing(GeneratedPath, "Prefabs");
        CreateFolderIfMissing("Assets", "Scenes");
    }

    private static void CreateFolderIfMissing(string parent, string folderName)
    {
        string path = parent + "/" + folderName;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, folderName);
    }

    private static Material CreateMaterial(string path, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateUnlitMaterial(string path, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static BallController CreateBallPrefab(Material material)
    {
        AssetDatabase.DeleteAsset(BallPrefabPath);

        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "PlayerBall";
        ball.transform.localScale = Vector3.one * 0.12f;
        ApplyMaterial(ball, material);

        Rigidbody body = ball.AddComponent<Rigidbody>();
        body.mass = 0.045f;
        body.linearDamping = 0.32f;
        body.angularDamping = 0.08f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        ball.AddComponent<BallController>();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(ball, BallPrefabPath);
        Object.DestroyImmediate(ball);
        return prefab.GetComponent<BallController>();
    }

    private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent);
        cube.transform.position = position;
        cube.transform.localScale = scale;
        ApplyMaterial(cube, material);
        return cube;
    }

    private static void ApplyMaterial(GameObject target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
    }

    private static void CreateDesktopCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraObject.transform.position = new Vector3(0f, 6.2f, -7.2f);
        cameraObject.transform.LookAt(new Vector3(0f, 0f, 0.5f));
        camera.clearFlags = CameraClearFlags.Skybox;
    }

    private static void CreateDirectionalLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static MiniGolfHUD CreateHud()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("HUD");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject topPanel = CreatePanel("TopPanel", canvasObject.transform, new Color(0.02f, 0.04f, 0.08f, 0.82f));
        RectTransform topRect = topPanel.GetComponent<RectTransform>();
        SetRect(topRect, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 150f));

        Text playerText = CreateText("PlayerText", topPanel.transform, font, 30, TextAnchor.MiddleLeft);
        SetRect(playerText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(35f, -20f), new Vector2(520f, 55f));

        Text holeText = CreateText("HoleText", topPanel.transform, font, 30, TextAnchor.MiddleCenter);
        SetRect(holeText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-180f, -20f), new Vector2(360f, 55f));

        Text scoreText = CreateText("ScoreText", topPanel.transform, font, 30, TextAnchor.MiddleRight);
        SetRect(scoreText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-635f, -20f), new Vector2(600f, 55f));

        Text timerText = CreateText("TimerText", canvasObject.transform, font, 38, TextAnchor.MiddleCenter);
        timerText.color = new Color(1f, 0.25f, 0.15f);
        SetRect(timerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-180f, -205f), new Vector2(360f, 60f));
        timerText.gameObject.SetActive(false);

        Text messageText = CreateText("MessageText", canvasObject.transform, font, 34, TextAnchor.UpperCenter);
        SetRect(messageText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-450f, -165f), new Vector2(900f, 250f));

        GameObject powerBackground = CreatePanel("PowerBackground", canvasObject.transform, new Color(0f, 0f, 0f, 0.78f));
        RectTransform powerBackgroundRect = powerBackground.GetComponent<RectTransform>();
        SetRect(powerBackgroundRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-250f, 105f), new Vector2(500f, 52f));

        GameObject powerFillObject = CreatePanel("PowerFill", powerBackground.transform, new Color(0.1f, 0.9f, 0.25f));
        Image powerFill = powerFillObject.GetComponent<Image>();
        powerFill.type = Image.Type.Filled;
        powerFill.fillMethod = Image.FillMethod.Horizontal;
        powerFill.fillOrigin = 0;
        powerFill.fillAmount = 0f;
        RectTransform fillRect = powerFillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(6f, 6f);
        fillRect.offsetMax = new Vector2(-6f, -6f);

        Text powerLabel = CreateText("PowerLabel", powerBackground.transform, font, 25, TextAnchor.MiddleCenter);
        powerLabel.text = "Fuerza 0%";
        powerLabel.color = Color.white;
        powerLabel.rectTransform.anchorMin = Vector2.zero;
        powerLabel.rectTransform.anchorMax = Vector2.one;
        powerLabel.rectTransform.offsetMin = Vector2.zero;
        powerLabel.rectTransform.offsetMax = Vector2.zero;

        Text instructionsText = CreateText("InstructionsText", canvasObject.transform, font, 23, TextAnchor.MiddleCenter);
        SetRect(instructionsText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-700f, 15f), new Vector2(1400f, 80f));

        MiniGolfHUD hud = canvasObject.AddComponent<MiniGolfHUD>();
        hud.playerText = playerText;
        hud.holeText = holeText;
        hud.scoreText = scoreText;
        hud.timerText = timerText;
        hud.messageText = messageText;
        hud.instructionsText = instructionsText;
        hud.powerLabel = powerLabel;
        hud.powerFill = powerFill;
        return hud;
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static Text CreateText(string name, Transform parent, Font font, int size, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static void CreateVrPutterPlaceholder(MiniGolfGameManager manager)
    {
        GameObject putter = new GameObject("Putter_VR_Placeholder");
        putter.transform.position = new Vector3(0.55f, 0.08f, -3.1f);

        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shaft.name = "Shaft";
        shaft.transform.SetParent(putter.transform);
        shaft.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        shaft.transform.localScale = new Vector3(0.035f, 1f, 0.035f);
        Object.DestroyImmediate(shaft.GetComponent<Collider>());

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "ClubHead";
        head.transform.SetParent(putter.transform);
        head.transform.localPosition = new Vector3(0f, 0.03f, 0.12f);
        head.transform.localScale = new Vector3(0.34f, 0.1f, 0.12f);
        BoxCollider collider = head.GetComponent<BoxCollider>();
        collider.isTrigger = true;

        ClubHead clubHead = head.AddComponent<ClubHead>();
        clubHead.gameManager = manager;
        clubHead.velocitySource = putter.transform;

        putter.SetActive(false);
    }
}
