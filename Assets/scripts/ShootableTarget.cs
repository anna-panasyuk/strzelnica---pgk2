using UnityEngine;

public class ShootableTarget : MonoBehaviour
{
    public int points = 10;
    public float health = 1f;
    public bool moving;
    public float moveDistance = 2.5f;
    public float moveSpeed = 1.5f;
    public bool randomizeOnHit = true;

    private Vector3 startPosition;
    private Renderer cachedRenderer;
    private float currentHealth;

    void Awake()
    {
        startPosition = transform.position;
        cachedRenderer = GetComponentInChildren<Renderer>();
        currentHealth = health;
        ApplyVisualState();
    }

    void Update()
    {
        if (!moving) return;

        float offset = Mathf.Sin(Time.time * moveSpeed) * moveDistance;
        transform.position = startPosition + transform.right * offset;
    }

    public int Hit(float damage)
    {
        currentHealth -= damage;
        if (currentHealth > 0f)
        {
            return 0;
        }

        currentHealth = health;
        if (randomizeOnHit)
        {
            RespawnVariant();
        }
        else
        {
            gameObject.SetActive(false);
        }

        return points;
    }

    private void RespawnVariant()
    {
        float newScale = Random.Range(0.7f, 1.35f);
        // Preserve disc aspect ratio: original scale is (0.9, 0.14, 0.9) — keep Y thin
        transform.localScale = new Vector3(newScale, 0.14f, newScale);
        startPosition += new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(-0.3f, 0.6f), 0f);
        ApplyVisualState();
    }

    private static Material MakeMat(Color color)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        Material mat = new Material(sh);
        mat.color = color;
        return mat;
    }

    private void ApplyVisualState()
    {
        if (cachedRenderer == null) return;
        cachedRenderer.material = MakeMat(moving ? new Color(0.1f, 0.8f, 1f) : new Color(1f, 0.25f, 0.2f));
    }
}
