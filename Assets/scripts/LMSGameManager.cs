using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LMSGameManager : MonoBehaviour
{
    private ShootableTarget[] allTargets;
    private int previousAliveCount;

    private int round = 1;
    private int kills = 0;
    private int bestRoundKills = 0;
    private bool roundActive = true;

    private Text modeHudText;
    private Text announcerText;
    private float announcerUntil;

    void Start()
    {
        allTargets = FindObjectsByType<ShootableTarget>(FindObjectsSortMode.None);
        previousAliveCount = CountAlive();

        modeHudText = BuildModeHud();
        BuildAnnouncer();
        ShowAnnouncer("ROUND " + round + "  FIGHT!");
        UpdateHud();
    }

    void Update()
    {
        if (!roundActive)
        {
            TickAnnouncer();
            return;
        }

        int alive = CountAlive();
        int newKills = previousAliveCount - alive;
        if (newKills > 0)
        {
            kills += newKills;
            if (kills > bestRoundKills) bestRoundKills = kills;
        }
        previousAliveCount = alive;

        if (alive <= 0)
        {
            roundActive = false;
            StartCoroutine(NextRound());
        }

        TickAnnouncer();
        UpdateHud();
    }

    private int CountAlive()
    {
        if (allTargets == null) return 0;
        int count = 0;
        foreach (ShootableTarget t in allTargets)
            if (t != null && t.gameObject.activeSelf) count++;
        return count;
    }

    private IEnumerator NextRound()
    {
        ShowAnnouncer("ROUND " + round + " CLEARED!  Next in 3s...");
        UpdateHud();
        yield return new WaitForSeconds(3f);

        round++;
        kills = 0;

        foreach (ShootableTarget t in allTargets)
            if (t != null) t.gameObject.SetActive(true);

        previousAliveCount = CountAlive();
        roundActive = true;
        ShowAnnouncer("ROUND " + round + "  FIGHT!");
        UpdateHud();
    }

    private void ShowAnnouncer(string msg)
    {
        if (announcerText == null) return;
        announcerText.text = msg;
        announcerText.color = new Color(1f, 0.9f, 0.2f, 1f);
        announcerUntil = Time.time + 2.5f;
    }

    private void TickAnnouncer()
    {
        if (announcerText == null) return;
        float remaining = announcerUntil - Time.time;
        if (remaining <= 0f)
        {
            Color c = announcerText.color;
            c.a = 0f;
            announcerText.color = c;
        }
        else
        {
            Color c = announcerText.color;
            c.a = Mathf.Clamp01(remaining / 0.5f);
            announcerText.color = c;
        }
    }

    private void UpdateHud()
    {
        if (modeHudText == null) return;
        int alive = CountAlive();
        modeHudText.text =
            "LAST MAN STANDING\n" +
            "Round: " + round + "\n" +
            "Enemies alive: " + alive + "\n" +
            "Kills this round: " + kills + "\n" +
            "Best round: " + bestRoundKills + " kills";
    }

    private Text BuildModeHud()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("LMS_HUD_Panel");
        if (existing != null)
            return existing.Find("LMS_HUD") != null
                ? existing.Find("LMS_HUD").GetComponent<Text>()
                : null;

        GameObject panel = new GameObject("LMS_HUD_Panel");
        panel.transform.SetParent(canvas.transform, false);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.025f, 0.03f, 0.65f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-14f, -14f);
        panelRect.sizeDelta = new Vector2(300f, 130f);

        GameObject textObj = new GameObject("LMS_HUD");
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

    private void BuildAnnouncer()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        Transform existing = canvas.transform.Find("LMS_Announcer");
        if (existing != null)
        {
            announcerText = existing.GetComponent<Text>();
            return;
        }

        GameObject go = new GameObject("LMS_Announcer");
        go.transform.SetParent(canvas.transform, false);
        announcerText = go.AddComponent<Text>();
        announcerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        announcerText.fontSize = 44;
        announcerText.fontStyle = FontStyle.Bold;
        announcerText.alignment = TextAnchor.MiddleCenter;
        announcerText.raycastTarget = false;
        announcerText.color = new Color(1f, 0.9f, 0.2f, 0f);

        RectTransform rect = announcerText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 120f);
        rect.sizeDelta = new Vector2(700f, 100f);
    }
}
