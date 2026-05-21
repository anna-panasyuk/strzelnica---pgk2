using UnityEngine;
using UnityEngine.UI;

public class CTFGameManager : MonoBehaviour
{
    private Transform playerTransform;
    private GameObject enemyFlagObject;
    private Vector3 enemyFlagHome;
    private Vector3 blueBasePosition;

    private int blueScore = 0;
    private bool playerHasFlag = false;

    private Text modeHudText;

    private const float pickupRadius = 2.8f;
    private const float captureRadius = 3.2f;

    void Start()
    {
        GameObject player = GameObject.Find("Gracz");
        if (player != null) playerTransform = player.transform;

        enemyFlagObject = GameObject.Find("CTF Red Flag");
        if (enemyFlagObject != null)
            enemyFlagHome = enemyFlagObject.transform.position;

        blueBasePosition = new Vector3(0f, 0f, -14f);

        modeHudText = BuildModeHud();
        UpdateHud();
    }

    void Update()
    {
        if (playerTransform == null || enemyFlagObject == null) return;

        if (!playerHasFlag && enemyFlagObject.activeSelf)
        {
            float dist = Vector3.Distance(playerTransform.position, enemyFlagObject.transform.position);
            if (dist < pickupRadius)
                PickupFlag();
        }

        if (playerHasFlag)
        {
            enemyFlagObject.transform.position = playerTransform.position + Vector3.up * 2.4f;

            float distToBase = Vector3.Distance(playerTransform.position, blueBasePosition);
            if (distToBase < captureRadius)
                CaptureFlag();
        }

        UpdateHud();
    }

    private void PickupFlag()
    {
        playerHasFlag = true;
    }

    private void CaptureFlag()
    {
        blueScore++;
        playerHasFlag = false;
        if (enemyFlagObject != null)
            enemyFlagObject.transform.position = enemyFlagHome;
    }

    private void UpdateHud()
    {
        if (modeHudText == null) return;

        string flagLine = playerHasFlag
            ? "FLAG CAPTURED - RETURN TO BLUE BASE!"
            : "Find and grab the RED flag (north)";

        float proximity = playerHasFlag && playerTransform != null
            ? Vector3.Distance(playerTransform.position, blueBasePosition)
            : 0f;

        string proximityLine = playerHasFlag
            ? "Base distance: " + proximity.ToString("F1") + " m"
            : "";

        modeHudText.text =
            "CTF - Capture the Flag\n" +
            "Blue: " + blueScore + "\n" +
            flagLine + "\n" +
            proximityLine;
    }

    private Text BuildModeHud()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("CTF_HUD_Panel");
        if (existing != null)
            return existing.Find("CTF_HUD") != null
                ? existing.Find("CTF_HUD").GetComponent<Text>()
                : null;

        GameObject panel = new GameObject("CTF_HUD_Panel");
        panel.transform.SetParent(canvas.transform, false);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.025f, 0.03f, 0.65f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-14f, -14f);
        panelRect.sizeDelta = new Vector2(360f, 112f);

        GameObject textObj = new GameObject("CTF_HUD");
        textObj.transform.SetParent(panel.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 17;
        text.alignment = TextAnchor.UpperLeft;
        text.raycastTarget = false;
        text.color = new Color(0.92f, 0.95f, 1f);

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(10f, 8f);
        rect.offsetMax = new Vector2(-8f, -8f);

        return text;
    }
}
