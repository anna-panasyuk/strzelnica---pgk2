using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class WeaponSystem : MonoBehaviour
{
    [System.Serializable]
    public class Weapon
    {
        public string name = "Pistol";
        public float damage = 1f;
        public float fireDelay = 0.35f;
        public float range = 90f;
        public Color color = Color.yellow;
        public Vector3 modelScale = Vector3.one;
        public Vector3 modelOffset = Vector3.zero;
    }

    public Weapon[] weapons =
    {
        new Weapon
        {
            name = "Pistol",
            damage = 1f,
            fireDelay = 0.32f,
            range = 80f,
            color = new Color(1f, 0.86f, 0.25f),
            modelScale = new Vector3(0.72f, 0.72f, 0.72f),
            modelOffset = new Vector3(0.03f, -0.02f, -0.08f)
        },
        new Weapon
        {
            name = "Scorpion-style SMG",
            damage = 0.65f,
            fireDelay = 0.1f,
            range = 115f,
            color = new Color(0.15f, 0.88f, 1f),
            modelScale = new Vector3(1f, 1f, 1.22f),
            modelOffset = new Vector3(0.06f, -0.01f, 0.04f)
        }
    };

    public Camera playerCamera;
    public Text hudText;
    public AudioSource audioSource;
    public int score;

    private int selectedWeapon;
    private float nextShotTime;
    private float recoil;
    private float muzzleFlashUntil;
    private Transform weaponRoot;
    private Transform barrel;
    private GameObject muzzleFlash;
    private gracz_ruch movement;
    private Transform thirdPersonWeaponRoot;
    private GameObject thirdPersonMuzzleFlash;
    private const float aimAssistRadius = 0.38f;
    private Renderer[] weaponRenderers;
    private Text crosshairText;
    private Text hitFeedbackText;
    private float hitFeedbackUntil;
    private Vector3 weaponBasePosition;
    private Quaternion weaponBaseRotation;

    void Start()
    {
        movement = GetComponent<gracz_ruch>();

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (playerCamera != null)
        {
            playerCamera.fieldOfView = 72f;
            playerCamera.nearClipPlane = 0.02f;
            BuildViewModel();
            BuildThirdPersonWeapon();
            BuildCrosshair();
            BuildHitFeedback();
        }

        ApplyWeaponLook();
        UpdateHud();
    }

    void Update()
    {
        HandleWeaponSwitch();

        bool fireInput = (Mouse.current != null && Mouse.current.leftButton.isPressed)
                      || (Keyboard.current != null && Keyboard.current.pKey.isPressed);
        if (fireInput)
        {
            TryShoot();
        }

        UpdateWeaponVisibility();
        AnimateViewModel();
        AnimateThirdPersonWeapon();
        TickHitFeedback();
        UpdateHud();
    }

    public void AddScore(int points)
    {
        score += points;
        if (points > 0 && hitFeedbackText != null)
        {
            hitFeedbackText.text = "+" + points;
            hitFeedbackText.color = new Color(1f, 0.9f, 0.2f, 1f);
            hitFeedbackUntil = Time.time + 0.7f;
        }
        UpdateHud();
    }

    private void HandleWeaponSwitch()
    {
        if (Keyboard.current == null) return;

        int previousWeapon = selectedWeapon;
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            selectedWeapon = 0;
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame && weapons.Length > 1)
        {
            selectedWeapon = 1;
        }

        if (Mouse.current != null && Mouse.current.scroll.ReadValue().y != 0f)
        {
            selectedWeapon = (selectedWeapon + 1) % weapons.Length;
        }

        if (previousWeapon != selectedWeapon)
        {
            recoil = 0.08f;
            ApplyWeaponLook();
        }
    }

    private void TryShoot()
    {
        if (playerCamera == null || weapons.Length == 0 || Time.time < nextShotTime) return;

        Weapon weapon = weapons[Mathf.Clamp(selectedWeapon, 0, weapons.Length - 1)];
        nextShotTime = Time.time + weapon.fireDelay;
        recoil = Mathf.Min(recoil + 0.16f, 0.42f);
        muzzleFlashUntil = Time.time + 0.055f;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (TryFindShotHit(ray, weapon.range, out RaycastHit hit))
        {
            ShootableTarget target = hit.collider.GetComponentInParent<ShootableTarget>();
            if (target != null)
            {
                int gainedPoints = target.Hit(weapon.damage);
                AddScore(gainedPoints);
            }

            SpawnImpact(hit.point, hit.normal, weapon.color);
        }
    }

    private void BuildViewModel()
    {
        if (weaponRoot != null) return;

        GameObject root = new GameObject("FPS Weapon Viewmodel");
        root.transform.SetParent(playerCamera.transform, false);
        weaponRoot = root.transform;
        weaponBasePosition = new Vector3(0.36f, -0.31f, 0.58f);
        weaponBaseRotation = Quaternion.Euler(-1f, -7f, 0f);

        CreatePart("Receiver", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.34f, 0.18f, 0.55f), new Color(0.06f, 0.07f, 0.075f));
        GameObject barrelObject = CreatePart("Barrel", PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0.47f), new Vector3(0.045f, 0.36f, 0.045f), new Color(0.02f, 0.025f, 0.03f));
        barrelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        barrel = barrelObject.transform;

        CreatePart("Magazine", PrimitiveType.Cube, new Vector3(0f, -0.2f, 0.03f), new Vector3(0.18f, 0.36f, 0.2f), new Color(0.025f, 0.03f, 0.032f));
        CreatePart("Grip", PrimitiveType.Cube, new Vector3(0f, -0.2f, -0.18f), new Vector3(0.16f, 0.33f, 0.16f), new Color(0.035f, 0.04f, 0.04f)).transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        CreatePart("Sight", PrimitiveType.Cube, new Vector3(0f, 0.14f, 0.08f), new Vector3(0.12f, 0.08f, 0.2f), new Color(0.015f, 0.018f, 0.02f));
        CreatePart("Support Hand", PrimitiveType.Capsule, new Vector3(-0.18f, -0.28f, 0.2f), new Vector3(0.12f, 0.28f, 0.12f), new Color(0.18f, 0.12f, 0.085f)).transform.localRotation = Quaternion.Euler(78f, 0f, 12f);
        CreatePart("Trigger Hand", PrimitiveType.Capsule, new Vector3(0.16f, -0.32f, -0.16f), new Vector3(0.12f, 0.32f, 0.12f), new Color(0.18f, 0.12f, 0.085f)).transform.localRotation = Quaternion.Euler(64f, 0f, -16f);

        muzzleFlash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        muzzleFlash.name = "Muzzle Flash";
        muzzleFlash.transform.SetParent(weaponRoot, false);
        muzzleFlash.transform.localPosition = new Vector3(0f, 0.02f, 0.86f);
        muzzleFlash.transform.localScale = new Vector3(0.16f, 0.16f, 0.16f);
        muzzleFlash.GetComponent<Renderer>().material = MakeMat(new Color(1f, 0.72f, 0.1f));
        Destroy(muzzleFlash.GetComponent<Collider>());
        muzzleFlash.SetActive(false);

        weaponRenderers = weaponRoot.GetComponentsInChildren<Renderer>();
    }

    private static Material MakeMat(Color color)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        Material mat = new Material(sh);
        mat.color = color;
        return mat;
    }

    private GameObject CreatePart(string name, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = name;
        part.transform.SetParent(weaponRoot, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().material = MakeMat(color);
        Destroy(part.GetComponent<Collider>());
        return part;
    }

    private GameObject CreatePart(Transform parent, string name, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().material = MakeMat(color);
        Destroy(part.GetComponent<Collider>());
        return part;
    }

    private void BuildThirdPersonWeapon()
    {
        if (thirdPersonWeaponRoot != null) return;

        GameObject root = new GameObject("Third Person Weapon");
        root.transform.SetParent(transform, false);
        thirdPersonWeaponRoot = root.transform;
        thirdPersonWeaponRoot.localPosition = new Vector3(0.42f, 0.72f, 0.55f);
        thirdPersonWeaponRoot.localRotation = Quaternion.Euler(5f, 4f, 0f);

        CreatePart(thirdPersonWeaponRoot, "TP Receiver", PrimitiveType.Cube, Vector3.zero, new Vector3(0.16f, 0.12f, 0.72f), new Color(0.035f, 0.04f, 0.045f));
        GameObject barrelObject = CreatePart(thirdPersonWeaponRoot, "TP Barrel", PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0.5f), new Vector3(0.035f, 0.34f, 0.035f), new Color(0.015f, 0.018f, 0.02f));
        barrelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        CreatePart(thirdPersonWeaponRoot, "TP Magazine", PrimitiveType.Cube, new Vector3(0f, -0.16f, -0.05f), new Vector3(0.13f, 0.28f, 0.16f), new Color(0.02f, 0.024f, 0.028f));
        CreatePart(thirdPersonWeaponRoot, "TP Stock", PrimitiveType.Cube, new Vector3(0f, 0f, -0.48f), new Vector3(0.14f, 0.1f, 0.32f), new Color(0.025f, 0.03f, 0.034f));

        thirdPersonMuzzleFlash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        thirdPersonMuzzleFlash.name = "TP Muzzle Flash";
        thirdPersonMuzzleFlash.transform.SetParent(thirdPersonWeaponRoot, false);
        thirdPersonMuzzleFlash.transform.localPosition = new Vector3(0f, 0.03f, 0.86f);
        thirdPersonMuzzleFlash.transform.localScale = Vector3.one * 0.16f;
        thirdPersonMuzzleFlash.GetComponent<Renderer>().material = MakeMat(new Color(1f, 0.72f, 0.1f));
        Destroy(thirdPersonMuzzleFlash.GetComponent<Collider>());
        thirdPersonMuzzleFlash.SetActive(false);
        thirdPersonWeaponRoot.gameObject.SetActive(false);
    }

    private void BuildCrosshair()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        Transform existing = canvas.transform.Find("Crosshair");
        if (existing != null)
        {
            crosshairText = existing.GetComponent<Text>();
            return;
        }

        GameObject crosshairObject = new GameObject("Crosshair");
        crosshairObject.transform.SetParent(canvas.transform, false);
        crosshairText = crosshairObject.AddComponent<Text>();
        crosshairText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        crosshairText.text = "+";
        crosshairText.fontSize = 46;
        crosshairText.alignment = TextAnchor.MiddleCenter;
        crosshairText.color = new Color(1f, 1f, 1f, 0.82f);
        crosshairText.raycastTarget = false;

        RectTransform rect = crosshairText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(64f, 64f);
    }

    private void ApplyWeaponLook()
    {
        if (weaponRoot == null || weapons.Length == 0) return;

        Weapon weapon = weapons[Mathf.Clamp(selectedWeapon, 0, weapons.Length - 1)];
        weaponRoot.localScale = weapon.modelScale;

        if (weaponRenderers == null) return;
        foreach (Renderer renderer in weaponRenderers)
        {
            if (renderer != null && renderer.gameObject.name == "Sight")
            {
                renderer.material.color = weapon.color * 0.8f;
            }
        }
    }

    private void AnimateViewModel()
    {
        if (weaponRoot == null || weapons.Length == 0) return;

        Weapon weapon = weapons[Mathf.Clamp(selectedWeapon, 0, weapons.Length - 1)];
        recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 4.8f);

        float bob = Mathf.Sin(Time.time * 8f) * 0.012f;
        weaponRoot.localPosition = weaponBasePosition + weapon.modelOffset + new Vector3(0f, bob, -recoil * 0.25f);
        weaponRoot.localRotation = weaponBaseRotation * Quaternion.Euler(-recoil * 16f, recoil * 3f, recoil * 8f);

        if (muzzleFlash != null)
        {
            muzzleFlash.SetActive(Time.time < muzzleFlashUntil);
            muzzleFlash.transform.localScale = Vector3.one * Random.Range(0.12f, 0.24f);
        }

        if (crosshairText != null)
        {
            crosshairText.color = Color.Lerp(new Color(1f, 1f, 1f, 0.82f), weapon.color, recoil * 3f);
        }
    }

    private bool TryFindShotHit(Ray ray, float range, out RaycastHit bestHit)
    {
        RaycastHit[] hits = Physics.SphereCastAll(ray, aimAssistRadius, range, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null && !hit.collider.transform.IsChildOf(transform))
            {
                bestHit = hit;
                return true;
            }
        }

        bestHit = default;
        return false;
    }

    private void UpdateWeaponVisibility()
    {
        bool thirdPerson = movement != null && movement.IsThirdPerson;
        if (weaponRoot != null)
        {
            weaponRoot.gameObject.SetActive(!thirdPerson);
        }

        if (thirdPersonWeaponRoot != null)
        {
            thirdPersonWeaponRoot.gameObject.SetActive(thirdPerson);
        }
    }

    private void AnimateThirdPersonWeapon()
    {
        if (thirdPersonWeaponRoot == null) return;

        thirdPersonWeaponRoot.localPosition = new Vector3(0.42f, 0.72f, 0.55f - recoil * 0.08f);
        thirdPersonWeaponRoot.localRotation = Quaternion.Euler(5f - recoil * 8f, 4f, recoil * 5f);

        if (thirdPersonMuzzleFlash != null)
        {
            thirdPersonMuzzleFlash.SetActive(Time.time < muzzleFlashUntil && thirdPersonWeaponRoot.gameObject.activeSelf);
            thirdPersonMuzzleFlash.transform.localScale = Vector3.one * Random.Range(0.12f, 0.22f);
        }
    }

    private void SpawnImpact(Vector3 position, Vector3 normal, Color color)
    {
        GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        impact.name = "Bullet Impact";
        impact.transform.position = position + normal * 0.015f;
        impact.transform.localScale = Vector3.one * 0.08f;
        impact.GetComponent<Renderer>().material = MakeMat(color);
        Destroy(impact.GetComponent<Collider>());
        Destroy(impact, 1.2f);
    }

    private void BuildHitFeedback()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject go = new GameObject("HitFeedback");
        go.transform.SetParent(canvas.transform, false);
        hitFeedbackText = go.AddComponent<Text>();
        hitFeedbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (hitFeedbackText.font == null)
            hitFeedbackText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        hitFeedbackText.fontSize = 36;
        hitFeedbackText.fontStyle = FontStyle.Bold;
        hitFeedbackText.alignment = TextAnchor.MiddleCenter;
        hitFeedbackText.raycastTarget = false;
        hitFeedbackText.color = new Color(1f, 0.9f, 0.2f, 0f);

        RectTransform rect = hitFeedbackText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 80f);
        rect.sizeDelta = new Vector2(160f, 60f);
    }

    private void TickHitFeedback()
    {
        if (hitFeedbackText == null) return;
        float remaining = hitFeedbackUntil - Time.time;
        if (remaining <= 0f)
        {
            hitFeedbackText.color = new Color(1f, 0.9f, 0.2f, 0f);
            return;
        }
        float alpha = Mathf.Clamp01(remaining / 0.35f);
        Color c = hitFeedbackText.color;
        c.a = alpha;
        hitFeedbackText.color = c;
    }

    private void UpdateHud()
    {
        if (hudText == null || weapons.Length == 0) return;

        Weapon weapon = weapons[Mathf.Clamp(selectedWeapon, 0, weapons.Length - 1)];
        hudText.text = "SCORE  " + score + "\n[" + weapon.name + "]\nP / LMB - shoot\nV - FPS / TPS";
        hudText.color = new Color(0.92f, 0.95f, 1f);
    }
}
