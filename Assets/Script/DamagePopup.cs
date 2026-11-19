using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    const float Duration = 0.72f;
    const float Rise = 0.22f;
    static readonly Color NumberColor = new Color(0.85f, 0.28f, 1f, 1f);
    static readonly Color ShadowColor = new Color(0.25f, 0.02f, 0.38f, 1f);
    static readonly Color HealColor = new Color(0.2f, 0.9f, 0.28f, 1f);
    static readonly Color HealShadow = new Color(0.02f, 0.28f, 0.06f, 1f);

    TextMesh _text;
    TextMesh _shadow;
    Vector3 _start;
    float _time;
    Color _numberColor;
    Color _shadowColor;

    public static void Spawn(Role target, int damage)
    {
        Create(target, "-" + damage, NumberColor, ShadowColor);
    }

    public static void SpawnHeal(Role target, int amount)
    {
        Create(target, "+" + amount, HealColor, HealShadow);
    }

    static void Create(Role target, string value, Color number, Color shadow)
    {
        var go = new GameObject("DamagePopup");
        var popup = go.AddComponent<DamagePopup>();
        popup.Setup(target, value, number, shadow);
    }

    void Setup(Role target, string value, Color number, Color shadow)
    {
        _numberColor = number;
        _shadowColor = shadow;
        SpriteRenderer sprite = target.GetComponent<SpriteRenderer>();
        Vector3 position = target.transform.position;
        if (sprite != null)
            position = new Vector3(sprite.bounds.center.x, sprite.bounds.max.y, sprite.bounds.center.z - 0.2f);
        transform.position = position;
        _start = position;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        _text = CreateNumber(transform, value, font, number, 0f);
        _shadow = CreateNumber(transform, value, font, shadow, 0.03f);
        _shadow.transform.localPosition = new Vector3(0.04f, -0.04f, 0.02f);

        if (sprite != null)
        {
            ApplySorting(_text, sprite);
            ApplySorting(_shadow, sprite);
        }
    }

    static TextMesh CreateNumber(Transform parent, string value, Font font, Color color, float z)
    {
        var go = new GameObject("Number");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0f, z);
        TextMesh text = go.AddComponent<TextMesh>();
        text.text = value;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 80;
        text.characterSize = 0.14f;
        text.color = color;
        if (font != null)
            text.font = font;
        return text;
    }

    static void ApplySorting(TextMesh text, SpriteRenderer sprite)
    {
        MeshRenderer mesh = text.GetComponent<MeshRenderer>();
        mesh.sortingLayerID = sprite.sortingLayerID;
        mesh.sortingOrder = sprite.sortingOrder;
    }

    void Update()
    {
        _time += Time.deltaTime;
        float t = Mathf.Clamp01(_time / Duration);
        float rise = Rise * (1f - (1f - t) * (1f - t));
        transform.position = _start + Vector3.up * rise;
        float punch = t < 0.1f ? Mathf.Lerp(1.28f, 1f, t / 0.1f) : 1f;
        transform.localScale = Vector3.one * punch;
        float alpha = t < 0.72f ? 1f : 1f - (t - 0.72f) / 0.28f;
        SetAlpha(_text, _numberColor, alpha);
        SetAlpha(_shadow, _shadowColor, alpha);
        if (t >= 1f)
            Destroy(gameObject);
    }

    static void SetAlpha(TextMesh text, Color color, float alpha)
    {
        color.a = alpha;
        text.color = color;
    }
}
