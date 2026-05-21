using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MilestoneTwoSceneBuilder : MonoBehaviour
{
    private static bool builtForScene;
    private static bool subscribedToSceneChanges;

    private Material concrete;
    private Material darkMetal;
    private Material hazardYellow;
    private Material redTeam;
    private Material blueTeam;
    private Material glass;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BuildCurrentScene()
    {
        if (!subscribedToSceneChanges)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            subscribedToSceneChanges = true;
        }

        BuildScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        builtForScene = false;
        BuildScene(scene);
    }

    private static void BuildScene(Scene scene)
    {
        if (builtForScene) return;

        builtForScene = true;

        string sceneName = scene.name.ToLowerInvariant();

        // Mode-selection menu: inject CTF/LMS buttons at runtime
        if (sceneName.Contains("wybortrybu"))
        {
            MilestoneTwoSceneBuilder extender = new GameObject("Mode Menu Extender").AddComponent<MilestoneTwoSceneBuilder>();
            extender.InjectModeMenuButtons();
            return;
        }

        if (!sceneName.Contains("strzelnica") && !sceneName.Contains("tor")
            && !sceneName.Contains("multiplayer") && !sceneName.Contains("ctf")
            && !sceneName.Contains("lms"))
        {
            return;
        }

        MilestoneTwoSceneBuilder builder = new GameObject("Milestone Two Scene Builder").AddComponent<MilestoneTwoSceneBuilder>();
        builder.CreateMaterials();
        builder.SetupWorldMood();
        builder.EnsurePlayer(sceneName);
        builder.EnsureLighting();

        if (sceneName.Contains("strzelnica"))
        {
            builder.BuildShootingRange();
        }
        else if (sceneName.Contains("tor"))
        {
            builder.BuildObstacleCourse();
        }
        else if (sceneName.Contains("multiplayer"))
        {
            builder.BuildMultiplayerPrototype();
        }
        else if (sceneName.Contains("ctf"))
        {
            builder.BuildCTFMap();
        }
        else if (sceneName.Contains("lms"))
        {
            builder.BuildLMSMap();
        }
    }

    private void CreateMaterials()
    {
        concrete = CreateMaterial("Cold Concrete", new Color(0.24f, 0.26f, 0.27f), 0.05f, 0.15f);
        darkMetal = CreateMaterial("Gunmetal", new Color(0.055f, 0.06f, 0.065f), 0.25f, 0.45f);
        hazardYellow = CreateMaterial("Hazard Yellow", new Color(1f, 0.72f, 0.08f), 0.05f, 0.2f);
        redTeam = CreateMaterial("Red Team", new Color(0.9f, 0.08f, 0.08f), 0.1f, 0.25f);
        blueTeam = CreateMaterial("Blue Team", new Color(0.08f, 0.35f, 1f), 0.1f, 0.25f);
        glass = CreateMaterial("Target Glass", new Color(0.2f, 0.9f, 1f, 0.45f), 0.0f, 0.8f);
        SetupUrpTransparency(glass);
    }

    private Material CreateMaterial(string name, Color color, float metallic, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.name = name;
        material.color = color;
        material.SetFloat("_Metallic", metallic);
        bool isUrp = shader.name.Contains("Universal");
        material.SetFloat(isUrp ? "_Smoothness" : "_Glossiness", smoothness);
        return material;
    }

    private void SetupUrpTransparency(Material material)
    {
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
    }

    private void SetupWorldMood()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.32f, 0.38f, 0.48f);
        RenderSettings.ambientEquatorColor = new Color(0.18f, 0.19f, 0.2f);
        RenderSettings.ambientGroundColor = new Color(0.08f, 0.075f, 0.07f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.12f, 0.14f, 0.16f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.018f;

        Material skybox = new Material(Shader.Find("Skybox/Procedural"));
        skybox.SetFloat("_AtmosphereThickness", 0.65f);
        skybox.SetColor("_SkyTint", new Color(0.42f, 0.48f, 0.56f));
        skybox.SetColor("_GroundColor", new Color(0.12f, 0.12f, 0.13f));
        RenderSettings.skybox = skybox;
    }

    private void EnsurePlayer(string sceneName)
    {
        GameObject player = GameObject.Find("Gracz");
        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Gracz";
            Destroy(player.GetComponent<Collider>());
        }

        // Floor top = Y 0. CharacterController center=(0,0,0) height=1.8 → bottom at playerY-0.9.
        // Spawn at Y=0.95 so feet land 0.05 above floor — minimal drop, no jitter.
        if (sceneName.Contains("multiplayer"))
            player.transform.position = new Vector3(0f, 0.95f, -10f);
        else if (sceneName.Contains("tor"))
            player.transform.position = new Vector3(0f, 0.95f, -6f);
        else if (sceneName.Contains("ctf"))
            player.transform.position = new Vector3(0f, 0.95f, -13f);
        else if (sceneName.Contains("lms"))
            player.transform.position = new Vector3(0f, 0.95f, -2f);
        else
            player.transform.position = new Vector3(0f, 0.95f, -4f);

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = player.AddComponent<CharacterController>();
        }

        controller.height = 1.8f;
        controller.radius = 0.35f;

        if (player.GetComponent<gracz_ruch>() == null)
        {
            player.AddComponent<gracz_ruch>();
        }

        Camera camera = player.GetComponentInChildren<Camera>();
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Camera");
            cameraObject.transform.SetParent(player.transform);
            cameraObject.transform.localPosition = new Vector3(0f, 0.68f, 0.08f);
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.fieldOfView = 72f;
        camera.nearClipPlane = 0.02f;

        WeaponSystem weaponSystem = player.GetComponent<WeaponSystem>();
        if (weaponSystem == null)
        {
            weaponSystem = player.AddComponent<WeaponSystem>();
        }

        weaponSystem.playerCamera = camera;
        weaponSystem.hudText = EnsureHud();
    }

    private Text EnsureHud()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("HUD");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        Transform existing = canvas.transform.Find("MilestoneHUD");
        if (existing != null)
        {
            return existing.GetComponent<Text>();
        }

        // Main panel — tall enough for score + weapon (top) + controls help (bottom)
        GameObject panel = new GameObject("HUD Panel");
        panel.transform.SetParent(canvas.transform, false);
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.02f, 0.025f, 0.03f, 0.65f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(14f, -14f);
        panelRect.sizeDelta = new Vector2(292f, 236f);

        // Dynamic: score + active weapon name (top ~72px)
        GameObject textObject = new GameObject("MilestoneHUD");
        textObject.transform.SetParent(panel.transform, false);
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 18;
        text.alignment = TextAnchor.UpperLeft;
        text.raycastTarget = false;
        text.color = new Color(0.92f, 0.95f, 1f);

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(14f, 158f);   // leaves bottom 158px for controls
        rect.offsetMax = new Vector2(-10f, -10f);

        // Divider line
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(panel.transform, false);
        Image divImg = divider.AddComponent<Image>();
        divImg.color = new Color(0.4f, 0.45f, 0.5f, 0.35f);
        RectTransform divRect = divider.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0f, 1f);
        divRect.anchorMax = new Vector2(1f, 1f);
        divRect.pivot = new Vector2(0.5f, 1f);
        divRect.anchoredPosition = new Vector2(0f, -72f);
        divRect.sizeDelta = new Vector2(-20f, 1f);

        // Static: controls help (bottom ~158px)
        GameObject controlsObject = new GameObject("ControlsHelp");
        controlsObject.transform.SetParent(panel.transform, false);
        Text controlsText = controlsObject.AddComponent<Text>();
        controlsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        controlsText.fontSize = 14;
        controlsText.alignment = TextAnchor.UpperLeft;
        controlsText.raycastTarget = false;
        controlsText.color = new Color(0.60f, 0.66f, 0.74f);
        controlsText.text =
            "CONTROLS\n" +
            "WASD  - move       Space - jump\n" +
            "Mouse - look   P/LMB - shoot\n" +
            "1 / 2 / Wheel - switch weapon\n" +
            "V   - first / third person\n" +
            "Esc - unlock cursor";

        RectTransform controlsRect = controlsText.GetComponent<RectTransform>();
        controlsRect.anchorMin = Vector2.zero;
        controlsRect.anchorMax = Vector2.one;
        controlsRect.offsetMin = new Vector2(14f, 10f);
        controlsRect.offsetMax = new Vector2(-10f, -78f);  // starts 78px below panel top

        return text;
    }

    private void EnsureLighting()
    {
        if (FindAnyObjectByType<Light>() != null) return;

        GameObject lightObject = new GameObject("Sun Key Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
    }

    private void BuildShootingRange()
    {
        // ── FLOOR & CEILING (enclosed indoor range) ─────────────────────────────
        // Floor spans X:-11..+11, Z:-6..+30  (player spawns at Z=-4, well inside)
        CreateFloor("Range Floor",   new Vector3(0f,  -0.05f, 12f), new Vector3(22f, 0.1f,  36f), concrete);
        CreateFloor("Range Ceiling", new Vector3(0f,   4.15f, 12f), new Vector3(22f, 0.3f,  36f), darkMetal);

        // ── OUTER PERIMETER ──────────────────────────────────────────────────────
        CreateWall("Wall South", new Vector3( 0f,    2f, -6.2f), new Vector3(22f,   4f,  0.4f));
        CreateWall("Wall North", new Vector3( 0f,    2f, 30.2f), new Vector3(22f,   4f,  0.4f));
        CreateWall("Wall West",  new Vector3(-11.2f, 2f, 12f),   new Vector3(0.4f,  4f, 36.8f));
        CreateWall("Wall East",  new Vector3( 11.2f, 2f, 12f),   new Vector3(0.4f,  4f, 36.8f));

        // ── INTERIOR DIVIDERS (create 3 corridors: W / C / E) ───────────────────
        // Left divider  X=-4, solid Z= 2..20, open at south Z<2 and north Z>20
        CreateWall("Div Left",  new Vector3(-4f, 2f, 11f), new Vector3(0.4f, 4f, 18f));
        // Right divider X=+4, solid Z= 4..18, open at south Z<4 and north Z>18
        CreateWall("Div Right", new Vector3( 4f, 2f, 11f), new Vector3(0.4f, 4f, 14f));

        // ── CHOKE WALLS (perpendicular stubs creating corners) ───────────────────
        // West choke at Z=8: blocks X=-11..-5 (forces player toward centre briefly)
        CreateWall("Choke West", new Vector3(-8f, 2f,  8f), new Vector3(6f, 4f, 0.4f));
        // East choke at Z=22: blocks X=+5..+11
        CreateWall("Choke East", new Vector3( 8f, 2f, 22f), new Vector3(6f, 4f, 0.4f));

        // ── LIGHTING ─────────────────────────────────────────────────────────────
        float[] lx = { -7.5f, 0f, 7.5f };
        float[] lz = { -2f, 5f, 11f, 17f, 23f, 28.5f };
        foreach (float z in lz)
            foreach (float x in lx)
                CreateLightRig(new Vector3(x, 3.8f, z));

        // ── WEAPON TABLE near spawn ───────────────────────────────────────────────
        CreateWeaponTable(new Vector3(0f, 0.55f, -2.5f));

        // ── COVER STACKS ─────────────────────────────────────────────────────────
        CreateCoverStack("Cover WS", new Vector3(-7.5f, 0.55f,  3.5f), -1f);
        CreateCoverStack("Cover WN", new Vector3(-7.5f, 0.55f, 24f),    1f);
        CreateCoverStack("Cover CM", new Vector3( 0f,   0.55f, 14f),    1f);
        CreateCoverStack("Cover EN", new Vector3( 7.5f, 0.55f, 19f),   -1f);
        CreateCoverStack("Cover BK", new Vector3(-2.5f, 0.55f, 27f),   -1f);

        // ── TARGETS ──────────────────────────────────────────────────────────────
        // West corridor (enter from south gap Z<2, or north gap Z>20)
        CreateTarget("Target W1", new Vector3(-7.5f, 1.6f,  5.5f), true,  15);
        CreateTarget("Target W2", new Vector3(-6.5f, 1.6f,  9.5f), false, 10);
        CreateTarget("Target W3", new Vector3(-8f,   1.6f, 24.5f), true,  20);

        // Centre corridor
        CreateTarget("Target C1", new Vector3( 0f,   1.6f,  9f),   false, 10);
        CreateTarget("Target C2", new Vector3(-1.5f, 1.6f, 15f),   true,  15);
        CreateTarget("Target C3", new Vector3( 1f,   1.6f, 21f),   false, 20);

        // East corridor (accessible from gap at Z>18)
        CreateTarget("Target E1", new Vector3( 7.5f, 1.6f, 20f),   true,  20);
        CreateTarget("Target E2", new Vector3( 8.5f, 1.6f, 25f),   false, 15);
        CreateTarget("Target E3", new Vector3( 6.5f, 1.6f, 28.5f), true,  25);

        // North back room (all corridors open at Z>20)
        CreateTarget("Target N1", new Vector3(-4.5f, 1.6f, 29f), true,  30);
        CreateTarget("Target N2", new Vector3( 4.5f, 1.6f, 29f), false, 25);

        // ── HAZARD STRIPES at corridor junctions ─────────────────────────────────
        CreateStrip("Stripe W-Gap",  new Vector3(-7.5f, 0.01f,  2f),  new Vector3(7f,  0.02f, 0.3f), hazardYellow);
        CreateStrip("Stripe E-Gap",  new Vector3( 7.5f, 0.01f, 18f),  new Vector3(7f,  0.02f, 0.3f), hazardYellow);
        CreateStrip("Stripe North",  new Vector3( 0f,   0.01f, 29f),  new Vector3(22f, 0.02f, 0.3f), hazardYellow);
    }

    private void BuildObstacleCourse()
    {
        CreateFloor("Obstacle Course Floor", new Vector3(0f, -0.05f, 16f), new Vector3(16f, 0.1f, 48f), concrete);
        CreateGate("Start Gate", new Vector3(0f, 1.4f, -4f), blueTeam);
        CreateGate("Finish Gate", new Vector3(0f, 1.4f, 39f), hazardYellow);

        for (int i = 0; i < 10; i++)
        {
            float z = i * 4.1f;
            float side = i % 2 == 0 ? -1f : 1f;
            CreateCoverStack("Concrete Cover " + i, new Vector3(side * 3.3f, 0.55f, z), side);
            CreateFenceSegment("Fence " + i, new Vector3(-side * 5.7f, 1.15f, z + 1.8f), Quaternion.Euler(0f, side > 0f ? 90f : -90f, 0f));
            CreateTarget("Course Threat " + i, new Vector3(-side * 2.6f, 1.6f, z + 2.1f), i % 2 == 0, 20);
        }

        CreateStrip("Finish Hazard Stripe", new Vector3(0f, 0.04f, 38.6f), new Vector3(6.4f, 0.04f, 0.5f), hazardYellow);
    }

    private void BuildMultiplayerPrototype()
    {
        CreateFloor("CTF LMS Arena Floor", new Vector3(0f, -0.05f, 5f), new Vector3(34f, 0.1f, 34f), concrete);
        CreateFenceRun(-16.8f, 1.1f, -10f, 8, false);
        CreateFenceRun(16.8f, 1.1f, -10f, 8, false);
        CreateFenceRun(-12f, 1.1f, -11.8f, 8, true);
        CreateFenceRun(-12f, 1.1f, 21.8f, 8, true);

        CreateFlagBase("Blue Flag Base", new Vector3(-12f, 0.1f, 5f), blueTeam);
        CreateFlagBase("Red Flag Base", new Vector3(12f, 0.1f, 5f), redTeam);
        CreateGate("Mid Control Gate", new Vector3(0f, 1.4f, 5f), hazardYellow);

        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2f / 8f;
            Vector3 position = new Vector3(Mathf.Cos(angle) * 8.5f, 1f, 5f + Mathf.Sin(angle) * 8.5f);
            CreateCoverStack("Arena Cover " + i, position, Mathf.Sign(Mathf.Cos(angle)));
            CreateSpawnPad("LMS Spawn " + (i + 1), position + Vector3.up * 0.08f, i % 2 == 0 ? blueTeam : redTeam);
        }

        CreateAgentDummy("Blue Agent Dummy", new Vector3(-7.5f, 0.9f, 2f), blueTeam);
        CreateAgentDummy("Red Agent Dummy", new Vector3(7.5f, 0.9f, 8f), redTeam);
    }

    private GameObject CreateFloor(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = name;
        floor.transform.position = position;
        floor.transform.localScale = scale;
        floor.GetComponent<Renderer>().material = material;
        return floor;
    }

    private void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = CreateFloor(name, position, scale, concrete);
        AddTrim(name + " Top Trim", position + new Vector3(0f, scale.y * 0.5f + 0.08f, 0f), new Vector3(scale.x + 0.08f, 0.16f, scale.z + 0.08f));
    }

    private void AddTrim(string name, Vector3 position, Vector3 scale)
    {
        GameObject trim = CreateFloor(name, position, scale, darkMetal);
        trim.GetComponent<Renderer>().material = darkMetal;
    }

    private void CreateStrip(string name, Vector3 position, Vector3 scale, Material material)
    {
        CreateFloor(name, position, scale, material);
    }

    private void CreateLightRig(Vector3 position)
    {
        GameObject casing = CreateFloor("Ceiling Light Casing", position, new Vector3(1.4f, 0.08f, 0.22f), darkMetal);
        GameObject bulb = CreateFloor("Cool White Light Panel", position + Vector3.down * 0.08f, new Vector3(1.1f, 0.04f, 0.16f), glass);
        Light light = bulb.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 8f;
        light.intensity = 1.7f;
        light.color = new Color(0.75f, 0.9f, 1f);
        casing.isStatic = true;
    }

    private void CreateWeaponTable(Vector3 position)
    {
        CreateFloor("Weapon Bench", position, new Vector3(3.6f, 0.18f, 1f), darkMetal);
        CreateFloor("Bench Left Leg", position + new Vector3(-1.45f, -0.45f, -0.35f), new Vector3(0.16f, 0.85f, 0.16f), darkMetal);
        CreateFloor("Bench Right Leg", position + new Vector3(1.45f, -0.45f, 0.35f), new Vector3(0.16f, 0.85f, 0.16f), darkMetal);
        CreateFloor("Ammo Crate", position + new Vector3(-0.9f, 0.2f, 0f), new Vector3(0.8f, 0.35f, 0.55f), hazardYellow);
        CreateFloor("Weapon Prop", position + new Vector3(0.65f, 0.22f, 0.02f), new Vector3(1.2f, 0.12f, 0.18f), darkMetal);
    }

    private void CreateGate(string name, Vector3 position, Material accent)
    {
        CreateFloor(name + " Left Pillar", position + new Vector3(-3f, 0f, 0f), new Vector3(0.28f, 2.8f, 0.28f), darkMetal);
        CreateFloor(name + " Right Pillar", position + new Vector3(3f, 0f, 0f), new Vector3(0.28f, 2.8f, 0.28f), darkMetal);
        CreateFloor(name + " Header", position + new Vector3(0f, 1.45f, 0f), new Vector3(6.3f, 0.22f, 0.3f), accent);
    }

    private void CreateCoverStack(string name, Vector3 position, float side)
    {
        CreateFloor(name + " Low Block", position, new Vector3(2.2f, 1.1f, 0.8f), concrete);
        CreateFloor(name + " Angled Cap", position + new Vector3(0.35f * side, 0.72f, 0f), new Vector3(1.3f, 0.35f, 0.85f), darkMetal).transform.rotation = Quaternion.Euler(0f, 0f, side * 8f);
    }

    private void CreateFenceRun(float x, float y, float startZ, int count, bool alongZ)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 position = alongZ ? new Vector3(x, y, startZ + i * 3.7f) : new Vector3(x + i * 3.7f, y, startZ);
            Quaternion rotation = alongZ ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
            CreateFenceSegment("PSX Fence Segment", position, rotation);
        }
    }

    private void CreateFenceSegment(string name, Vector3 position, Quaternion rotation)
    {
        GameObject root = new GameObject(name);
        root.transform.position = position;
        root.transform.rotation = rotation;

        CreateFencePart(root.transform, "Post A", new Vector3(-1.75f, 0f, 0f), new Vector3(0.12f, 2.1f, 0.12f));
        CreateFencePart(root.transform, "Post B", new Vector3(1.75f, 0f, 0f), new Vector3(0.12f, 2.1f, 0.12f));
        CreateFencePart(root.transform, "Top Rail", new Vector3(0f, 0.8f, 0f), new Vector3(3.6f, 0.1f, 0.1f));
        CreateFencePart(root.transform, "Mid Rail", new Vector3(0f, 0.15f, 0f), new Vector3(3.6f, 0.08f, 0.08f));

        for (int i = 0; i < 7; i++)
        {
            CreateFencePart(root.transform, "Vertical Wire", new Vector3(-1.2f + i * 0.4f, 0.25f, 0f), new Vector3(0.035f, 1.45f, 0.035f));
        }
    }

    private void CreateFencePart(Transform parent, string name, Vector3 localPosition, Vector3 scale)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().material = darkMetal;
    }

    private GameObject CreateTarget(string name, Vector3 position, bool moving, int points)
    {
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        target.name = name;
        target.transform.position = position;
        target.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        target.transform.localScale = new Vector3(0.9f, 0.14f, 0.9f);

        ShootableTarget shootableTarget = target.AddComponent<ShootableTarget>();
        shootableTarget.moving = moving;
        shootableTarget.points = points;
        shootableTarget.moveDistance = moving ? 2.5f : 0f;
        shootableTarget.moveSpeed = moving ? 1.2f + points * 0.02f : 0f;

        CreateTargetRing(target.transform, 0.74f, Color.white, 0.012f);
        CreateTargetRing(target.transform, 0.52f, new Color(0.1f, 0.12f, 0.15f), 0.018f);
        CreateTargetRing(target.transform, 0.28f, moving ? new Color(0.1f, 0.85f, 1f) : new Color(1f, 0.12f, 0.08f), 0.024f);
        return target;
    }

    private void CreateTargetRing(Transform parent, float scale, Color color, float forwardOffset)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Target Ring";
        ring.transform.SetParent(parent, false);
        ring.transform.localPosition = new Vector3(0f, forwardOffset, 0f);
        ring.transform.localRotation = Quaternion.identity;
        ring.transform.localScale = new Vector3(scale, 0.035f, scale);
        ring.GetComponent<Renderer>().material = CreateMaterial("Ring", color, 0.1f, 0.3f);
        Destroy(ring.GetComponent<Collider>());
    }

    private void CreateFlagBase(string name, Vector3 position, Material material)
    {
        CreateSpawnPad(name, position, material);
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = name + " Pole";
        pole.transform.position = position + new Vector3(0f, 1.5f, 0f);
        pole.transform.localScale = new Vector3(0.08f, 1.5f, 0.08f);
        pole.GetComponent<Renderer>().material = darkMetal;

        GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flag.name = name + " Flag";
        flag.transform.position = position + new Vector3(0.72f, 2.45f, 0f);
        flag.transform.localScale = new Vector3(1.45f, 0.72f, 0.08f);
        flag.GetComponent<Renderer>().material = material;

        Light light = flag.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 5f;
        light.intensity = 1.3f;
        light.color = material.color;
    }

    private void CreateSpawnPad(string name, Vector3 position, Material material)
    {
        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = name;
        pad.transform.position = position;
        pad.transform.localScale = new Vector3(1.4f, 0.08f, 1.4f);
        pad.GetComponent<Renderer>().material = material;
    }

    private void CreateAgentDummy(string name, Vector3 position, Material accent)
    {
        GameObject root = new GameObject(name);
        root.transform.position = position;

        CreateDummyPart(root.transform, "Body Armor", PrimitiveType.Capsule, new Vector3(0f, 0.9f, 0f), new Vector3(0.42f, 0.72f, 0.32f), concrete);
        CreateDummyPart(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.75f, 0f), new Vector3(0.32f, 0.32f, 0.32f), accent);
        CreateDummyPart(root.transform, "Rifle", PrimitiveType.Cube, new Vector3(0.35f, 1.1f, 0.2f), new Vector3(0.85f, 0.1f, 0.14f), darkMetal);
        CreateDummyPart(root.transform, "Left Leg", PrimitiveType.Capsule, new Vector3(-0.16f, 0.25f, 0f), new Vector3(0.16f, 0.42f, 0.16f), darkMetal);
        CreateDummyPart(root.transform, "Right Leg", PrimitiveType.Capsule, new Vector3(0.16f, 0.25f, 0f), new Vector3(0.16f, 0.42f, 0.16f), darkMetal);
    }

    private void CreateDummyPart(Transform parent, string name, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().material = material;
    }

    // ── CTF MAP ─────────────────────────────────────────────────────────────────

    private void BuildCTFMap()
    {
        // Floor & ceiling (enclosed arena)
        CreateFloor("CTF Floor",   new Vector3(0f, -0.05f, 0f), new Vector3(32f, 0.1f,  36f), concrete);
        CreateFloor("CTF Ceiling", new Vector3(0f,  4.15f, 0f), new Vector3(32f, 0.3f,  36f), darkMetal);

        // Perimeter walls
        CreateWall("CTF South Wall", new Vector3( 0f,    2f, -18.2f), new Vector3(32f,  4f,  0.4f));
        CreateWall("CTF North Wall", new Vector3( 0f,    2f,  18.2f), new Vector3(32f,  4f,  0.4f));
        CreateWall("CTF West Wall",  new Vector3(-16.2f, 2f,   0f),   new Vector3(0.4f, 4f, 36.8f));
        CreateWall("CTF East Wall",  new Vector3( 16.2f, 2f,   0f),   new Vector3(0.4f, 4f, 36.8f));

        // ── BLUE BASE (south – player's team) ────────────────────────────────────
        CreateFlagBase("CTF Blue Base", new Vector3(0f, 0.1f, -14f), blueTeam);
        CreateSpawnPad("Blue Spawn L", new Vector3(-3.5f, 0.05f, -16f), blueTeam);
        CreateSpawnPad("Blue Spawn R", new Vector3( 3.5f, 0.05f, -16f), blueTeam);
        CreateStrip("CTF Blue Stripe", new Vector3(0f, 0.01f, -11f), new Vector3(32f, 0.02f, 0.35f), blueTeam);

        // ── RED BASE (north – enemy) ──────────────────────────────────────────────
        CreateFlagBase("CTF Red Base", new Vector3(0f, 0.1f, 14f), redTeam);
        CreateSpawnPad("Red Spawn L", new Vector3(-3.5f, 0.05f, 16f), redTeam);
        CreateSpawnPad("Red Spawn R", new Vector3( 3.5f, 0.05f, 16f), redTeam);
        CreateStrip("CTF Red Stripe", new Vector3(0f, 0.01f, 11f), new Vector3(32f, 0.02f, 0.35f), redTeam);

        // ── CAPTURABLE FLAG (glowing sphere at red base) ─────────────────────────
        GameObject ctfFlag = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ctfFlag.name = "CTF Red Flag";
        ctfFlag.transform.position = new Vector3(0f, 1.55f, 14f);
        ctfFlag.transform.localScale = Vector3.one * 0.5f;
        ctfFlag.GetComponent<Renderer>().material = CreateMaterial("CTF Flag", new Color(1f, 0.12f, 0.08f), 0.2f, 0.9f);
        Destroy(ctfFlag.GetComponent<Collider>());
        Light flagLight = ctfFlag.AddComponent<Light>();
        flagLight.type = LightType.Point;
        flagLight.range = 7f;
        flagLight.intensity = 1.6f;
        flagLight.color = new Color(1f, 0.15f, 0.08f);

        // ── COVER (symmetric, 4 rows) ─────────────────────────────────────────────
        // Row south  (Z = -9)
        CreateCoverStack("CTF Cov SL",  new Vector3(-7f, 0.55f,  -9f),  -1f);
        CreateCoverStack("CTF Cov SR",  new Vector3( 7f, 0.55f,  -9f),   1f);
        // Row south-mid  (Z = -4)
        CreateCoverStack("CTF Cov SML", new Vector3(-10f, 0.55f, -4f),   1f);
        CreateCoverStack("CTF Cov SMC", new Vector3(  0f, 0.55f, -4f),   1f);
        CreateCoverStack("CTF Cov SMR", new Vector3( 10f, 0.55f, -4f),  -1f);
        // Centre row  (Z = 0)
        CreateCoverStack("CTF Cov CL",  new Vector3(-7f, 0.55f,   0f),  -1f);
        CreateCoverStack("CTF Cov CC",  new Vector3( 0f, 0.55f,   0f),   1f);
        CreateCoverStack("CTF Cov CR",  new Vector3( 7f, 0.55f,   0f),   1f);
        // Row north-mid  (Z = 4)
        CreateCoverStack("CTF Cov NML", new Vector3(-10f, 0.55f,  4f),  -1f);
        CreateCoverStack("CTF Cov NMC", new Vector3(  0f, 0.55f,  4f),  -1f);
        CreateCoverStack("CTF Cov NMR", new Vector3( 10f, 0.55f,  4f),   1f);
        // Row north  (Z = 9)
        CreateCoverStack("CTF Cov NL",  new Vector3(-7f, 0.55f,   9f),   1f);
        CreateCoverStack("CTF Cov NR",  new Vector3( 7f, 0.55f,   9f),  -1f);

        // ── SIDE BARRIERS (mid-zone, low) ─────────────────────────────────────────
        CreateFloor("CTF Barrier W", new Vector3(-13f, 0.65f, 0f), new Vector3(0.4f, 1.3f, 10f), darkMetal);
        CreateFloor("CTF Barrier E", new Vector3( 13f, 0.65f, 0f), new Vector3(0.4f, 1.3f, 10f), darkMetal);

        // ── LIGHTING ─────────────────────────────────────────────────────────────
        float[] lx = { -10f, 0f, 10f };
        float[] lz = { -14f, -7f, 0f, 7f, 14f };
        foreach (float z in lz)
            foreach (float x in lx)
                CreateLightRig(new Vector3(x, 3.8f, z));

        // ── GAME MANAGER ─────────────────────────────────────────────────────────
        new GameObject("CTF Manager").AddComponent<CTFGameManager>();
    }

    // ── LMS MAP ──────────────────────────────────────────────────────────────────

    private void BuildLMSMap()
    {
        // Floor & ceiling
        CreateFloor("LMS Floor",   new Vector3(0f, -0.05f, 0f), new Vector3(30f, 0.1f,  30f), concrete);
        CreateFloor("LMS Ceiling", new Vector3(0f,  4.15f, 0f), new Vector3(30f, 0.3f,  30f), darkMetal);

        // Perimeter walls
        CreateWall("LMS South Wall", new Vector3( 0f,    2f, -15.2f), new Vector3(30f,  4f,  0.4f));
        CreateWall("LMS North Wall", new Vector3( 0f,    2f,  15.2f), new Vector3(30f,  4f,  0.4f));
        CreateWall("LMS West Wall",  new Vector3(-15.2f, 2f,   0f),   new Vector3(0.4f, 4f, 30.8f));
        CreateWall("LMS East Wall",  new Vector3( 15.2f, 2f,   0f),   new Vector3(0.4f, 4f, 30.8f));

        // Spawn pad + weapon table at centre-south
        CreateSpawnPad("LMS Player Spawn", new Vector3(0f, 0.05f, -2f), blueTeam);
        CreateWeaponTable(new Vector3(0f, 0.55f, -1f));

        // ── COVER (8 outer + 2 inner) ─────────────────────────────────────────────
        CreateCoverStack("LMS Cov NW",  new Vector3(-8f,  0.55f,  8f),   1f);
        CreateCoverStack("LMS Cov N",   new Vector3( 0f,  0.55f, 10f),   1f);
        CreateCoverStack("LMS Cov NE",  new Vector3( 8f,  0.55f,  8f),  -1f);
        CreateCoverStack("LMS Cov E",   new Vector3(10f,  0.55f,  0f),  -1f);
        CreateCoverStack("LMS Cov SE",  new Vector3( 8f,  0.55f, -8f),   1f);
        CreateCoverStack("LMS Cov S",   new Vector3( 0f,  0.55f,-10f),  -1f);
        CreateCoverStack("LMS Cov SW",  new Vector3(-8f,  0.55f, -8f),  -1f);
        CreateCoverStack("LMS Cov W",   new Vector3(-10f, 0.55f,  0f),   1f);
        CreateCoverStack("LMS Cov CL",  new Vector3(-4f,  0.55f,  3f),   1f);
        CreateCoverStack("LMS Cov CR",  new Vector3( 4f,  0.55f, -3f),  -1f);

        // ── PICKUP PROPS (visual only) ────────────────────────────────────────────
        CreatePickupProp("Health Pickup 1", new Vector3(-2.5f, 0.3f,  2.5f), new Color(0.1f,  0.9f,  0.2f));
        CreatePickupProp("Health Pickup 2", new Vector3( 2.5f, 0.3f, -2.5f), new Color(0.1f,  0.9f,  0.2f));
        CreatePickupProp("Ammo Pickup 1",   new Vector3( 2.5f, 0.3f,  2.5f), new Color(1f,    0.85f, 0.1f));
        CreatePickupProp("Ammo Pickup 2",   new Vector3(-2.5f, 0.3f, -2.5f), new Color(1f,    0.85f, 0.1f));

        // ── ENEMY DUMMIES (6) ──────────────────────────────────────────────────────
        CreateLMSDummy("Enemy 1", new Vector3(-9f,  0.95f, -9f));
        CreateLMSDummy("Enemy 2", new Vector3( 9f,  0.95f, -9f));
        CreateLMSDummy("Enemy 3", new Vector3(-12f, 0.95f,  0f));
        CreateLMSDummy("Enemy 4", new Vector3( 12f, 0.95f,  0f));
        CreateLMSDummy("Enemy 5", new Vector3(-9f,  0.95f,  9f));
        CreateLMSDummy("Enemy 6", new Vector3( 9f,  0.95f,  9f));

        // ── HAZARD STRIPE at spawn ────────────────────────────────────────────────
        CreateStrip("LMS Spawn Stripe", new Vector3(0f, 0.01f, 0f), new Vector3(3f, 0.02f, 3f), hazardYellow);

        // ── LIGHTING ─────────────────────────────────────────────────────────────
        float[] lx = { -8f, 0f, 8f };
        float[] lz = { -8f, 0f, 8f };
        foreach (float z in lz)
            foreach (float x in lx)
                CreateLightRig(new Vector3(x, 3.8f, z));

        // ── GAME MANAGER ─────────────────────────────────────────────────────────
        new GameObject("LMS Manager").AddComponent<LMSGameManager>();
    }

    // ── PICKUP PROP (visual glow cylinder, no collider) ──────────────────────────

    private void CreatePickupProp(string name, Vector3 position, Color glowColor)
    {
        GameObject prop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        prop.name = name;
        prop.transform.position = position;
        prop.transform.localScale = new Vector3(0.4f, 0.25f, 0.4f);
        prop.GetComponent<Renderer>().material = CreateMaterial(name + " Mat", glowColor, 0f, 0.88f);
        Destroy(prop.GetComponent<Collider>());

        Light glow = prop.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.range = 3.5f;
        glow.intensity = 0.85f;
        glow.color = glowColor;
    }

    // ── LMS ENEMY DUMMY (capsule + ShootableTarget, no respawn/randomise) ────────

    private void CreateLMSDummy(string name, Vector3 position)
    {
        // Body capsule — WeaponSystem hits the collider, ShootableTarget handles kill
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = name;
        body.transform.position = position;
        body.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);   // ~1.8 m tall

        ShootableTarget target = body.AddComponent<ShootableTarget>();
        target.randomizeOnHit = false;   // deactivate on kill, don't randomise
        target.moving = false;
        target.points = 100;

        // Head sphere (decorative, no collider)
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = name + " Head";
        head.transform.position = position + new Vector3(0f, 1.1f, 0f);
        head.transform.localScale = Vector3.one * 0.38f;
        head.GetComponent<Renderer>().material = CreateMaterial(name + " Head", new Color(0.85f, 0.3f, 0.18f), 0f, 0.2f);
        Destroy(head.GetComponent<Collider>());

        // Rifle prop
        GameObject rifle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rifle.name = name + " Rifle";
        rifle.transform.position = position + new Vector3(0.35f, 0.75f, 0.2f);
        rifle.transform.localScale = new Vector3(0.8f, 0.09f, 0.12f);
        rifle.GetComponent<Renderer>().material = darkMetal;
        Destroy(rifle.GetComponent<Collider>());
    }

    // ── MODE MENU BUTTON INJECTION (WyborTrybu scene) ────────────────────────────

    private void InjectModeMenuButtons()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        SceneLoader loader = FindAnyObjectByType<SceneLoader>();
        if (loader == null) return;

        // Guard against double-injection
        if (canvas.transform.Find("CTF Mode Button") != null) return;

        // Left column: mirrors the right-column buttons at same Y heights → visible at any res
        // where the existing Strzelnica/Tor buttons are visible.
        AddMenuButton(canvas.transform, "CTF  Capture the Flag", "CTF",  loader, new Vector2(-700f, 400f));
        AddMenuButton(canvas.transform, "LMS  Last Man Standing", "LMS", loader, new Vector2(-700f,  50f));
    }

    private static void AddMenuButton(Transform parent, string label, string sceneName,
                                      SceneLoader loader, Vector2 anchoredPos)
    {
        string btnName = sceneName + " Mode Button";
        if (parent.Find(btnName) != null) return;

        GameObject btnGo = new GameObject(btnName);
        btnGo.transform.SetParent(parent, false);

        Image img = btnGo.AddComponent<Image>();
        img.color = new Color(0.2f, 0.22f, 0.28f, 0.92f);

        Button btn = btnGo.AddComponent<Button>();
        string captured = sceneName;
        SceneLoader capturedLoader = loader;
        btn.onClick.AddListener(() => capturedLoader.LoadSceneByName(captured));

        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.3f, 0.35f, 0.5f);
        cb.pressedColor = new Color(0.12f, 0.14f, 0.2f);
        btn.colors = cb;

        RectTransform rect = btnGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(800f, 175f);

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        Text labelText = labelGo.AddComponent<Text>();
        labelText.text = label;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 52;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = new Color(0.95f, 0.95f, 1f);
        labelText.raycastTarget = false;

        RectTransform lr = labelText.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;
    }
}
