using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    const float Duration = 0.8f;
    const float Rise = 0.55f;

    TextMesh _text;
    Color _color;
    Vector3 _start;
    float _time;

    public static void Spawn(Role target, int damage)
    {
        var go = new GameObject("DamagePopup");
        var popup = go.AddComponent<DamagePopup>();
        popup.Setup(target, damage);
    }

    void Setup(Role target, int damage)
    {
        SpriteRenderer sprite = target.GetComponent<SpriteRenderer>();
        Vector3 position = target.transform.position;
        if (sprite != null)
            position = new Vector3(sprite.bounds.center.x, sprite.bounds.max.y, sprite.bounds.center.z - 0.2f);
        transform.position = position;
        _start = position;

        _text = gameObject.AddComponent<TextMesh>();
        _text.text = "-" + damage;
        _text.anchor = TextAnchor.MiddleCenter;
        _text.alignment = TextAlignment.Center;
        _text.fontSize = 64;
        _text.characterSize = 0.08f;
        _text.color = Color.red;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
            _text.font = font;

        MeshRenderer mesh = GetComponent<MeshRenderer>();
        if (sprite != null)
        {
            mesh.sortingLayerID = sprite.sortingLayerID;
            mesh.sortingOrder = sprite.sortingOrder;
        }
        else
        {
            mesh.sortingOrder = 0;
        }

        _color = _text.color;
    }

    void Update()
    {
        _time += Time.deltaTime;
        float t = Mathf.Clamp01(_time / Duration);
        transform.position = _start + Vector3.up * (Rise * t);
        Color color = _color;
        color.a = 1f - t;
        _text.color = color;
        if (t >= 1f)
            Destroy(gameObject);
    }
}
