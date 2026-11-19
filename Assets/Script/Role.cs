using System.Collections;
using TMPro;
using UnityEngine;

public class Role : MonoBehaviour
{
    public Sprite portrait;

    public int health = 30;
    public int physicalAttack = 10;
    public int magicAttack = 10;
    public int manaMax = 10;
    public int manaRegen;
    public int moveRange = 4;
    public int range = 1;
    public Sprite poisonSprite;
    public Sprite wetSprite;
    public string roleName;

    public int healthMax { get; private set; }
    public int currentMana { get; private set; }
    public int poisonTurns { get; private set; }
    public int wetTurns { get; private set; }

    enum StatusIcon
    {
        None,
        Poison,
        Wet,
    }

    sealed class StatusView
    {
        public GameObject root;
        public SpriteRenderer image;
        public TMP_Text label;
        public StatusIcon icon;
    }

    SpriteRenderer _renderer;
    Color _color = Color.white;
    Vector3 _scale;
    StatusView[] _statuses;
    Coroutine _hurt;
    bool _dying;
    Transform _healthFill;
    float _healthFillScale = 1f;
    float _healthFillLeft;
    float _healthFillWidth;
    int _shownHealth = int.MinValue;

    public bool PlayingDeath { get; private set; }

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null)
            _color = _renderer.color;
        _scale = transform.localScale;
        healthMax = health;
        currentMana = 0;
        BindHealthBar();
        BindStatuses();
        RefreshStatusIcons();
    }

    void LateUpdate()
    {
        if (_healthFill == null || health == _shownHealth)
            return;
        _shownHealth = health;
        ApplyHealthFill();
    }

    void BindHealthBar()
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform fill = children[i];
            if (fill.name != "HealthBar1")
                continue;
            SpriteRenderer sprite = fill.GetComponent<SpriteRenderer>();
            if (sprite == null || sprite.sprite == null)
                continue;
            _healthFill = fill;
            _healthFillScale = fill.localScale.x;
            _healthFillWidth = sprite.sprite.bounds.size.x;
            _healthFillLeft = fill.localPosition.x - _healthFillWidth * _healthFillScale * 0.5f;
            return;
        }
    }

    void ApplyHealthFill()
    {
        float ratio = healthMax > 0 ? health / (float)healthMax : 0f;
        if (ratio < 0f)
            ratio = 0f;
        float scale = _healthFillScale * ratio;
        Vector3 size = _healthFill.localScale;
        size.x = scale;
        _healthFill.localScale = size;
        Vector3 position = _healthFill.localPosition;
        position.x = _healthFillLeft + _healthFillWidth * scale * 0.5f;
        _healthFill.localPosition = position;
    }

    public void PlayHurt()
    {
        if (General.Instance != null)
            General.Instance.Refresh(this);
        if (_dying)
            return;
        StopHurt();
        _hurt = StartCoroutine(HurtFlash());
    }

    public void PlayDeath()
    {
        if (_dying)
            return;
        _dying = true;
        PlayingDeath = true;
        StopHurt();
        if (GetComponent<Enemy>() != null && BoardGrid.Instance != null)
            BoardGrid.Instance.ClearPiece(this);
        StartCoroutine(Die());
    }

    void StopHurt()
    {
        if (_hurt == null)
            return;
        StopCoroutine(_hurt);
        _hurt = null;
        if (_renderer != null)
            _renderer.color = _color;
        transform.localScale = _scale;
    }

    IEnumerator Die()
    {
        _hurt = StartCoroutine(HurtFlash());
        yield return _hurt;

        float time = 0f;
        const float duration = 1f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float factor = 1f - Mathf.Clamp01(time / duration);
            transform.localScale = _scale * factor;
            yield return null;
        }

        transform.localScale = Vector3.zero;
        PlayingDeath = false;
        gameObject.SetActive(false);
    }

    IEnumerator HurtFlash()
    {
        const float duration = 0.28f;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float wave = Mathf.Sin(Mathf.Clamp01(time / duration) * Mathf.PI);
            if (_renderer != null)
                _renderer.color = Color.Lerp(_color, new Color(1f, 0.35f, 0.35f, _color.a), wave);
            transform.localScale = _scale * (1f + 0.12f * wave);
            yield return null;
        }

        if (_renderer != null)
            _renderer.color = _color;
        transform.localScale = _scale;
        _hurt = null;
    }

    public void ApplyPoison(int x)
    {
        poisonTurns += x;
        if (poisonTurns < 0)
            poisonTurns = 0;
        RefreshStatusIcons();
    }

    public void ApplyWet(int x)
    {
        wetTurns += x;
        if (wetTurns < 0)
            wetTurns = 0;
        RefreshStatusIcons();
    }

    public void TickStatuses()
    {
        if (poisonTurns > 0)
        {
            int damage = healthMax * 10 / 100;
            if (damage > 0)
                TakeSkillDamage(damage);
        }

        if (wetTurns > 0)
            currentMana = Mathf.Max(0, currentMana - 2);

        if (poisonTurns > 0)
            poisonTurns--;
        if (wetTurns > 0)
            wetTurns--;
        RefreshStatusIcons();
    }

    void BindStatuses()
    {
        _statuses = new StatusView[4];
        for (int i = 0; i < _statuses.Length; i++)
        {
            var view = new StatusView();
            Transform folder = transform.Find("status");
            Transform child = folder != null ? folder.Find("status" + (i + 1)) : null;
            if (child == null)
                child = transform.Find("status" + (i + 1));
            if (child != null)
            {
                view.root = child.gameObject;
                view.image = child.GetComponent<SpriteRenderer>();
                Transform text = child.Find("Text (TMP)");
                if (text != null)
                    view.label = text.GetComponent<TMP_Text>();
            }
            _statuses[i] = view;
            ClearSlot(view);
        }
    }

    void RefreshStatusIcons()
    {
        if (_statuses == null)
            return;

        SyncIcon(StatusIcon.Poison, poisonTurns, poisonSprite);
        SyncIcon(StatusIcon.Wet, wetTurns, wetSprite);
        CompactStatuses();
    }

    void SyncIcon(StatusIcon icon, int turns, Sprite sprite)
    {
        int index = FindIcon(icon);
        if (turns <= 0)
        {
            if (index >= 0)
                ClearSlot(_statuses[index]);
            return;
        }

        if (index < 0)
        {
            index = FindIcon(StatusIcon.None);
            if (index < 0)
                return;
        }

        StatusView view = _statuses[index];
        view.icon = icon;
        if (view.root != null)
            view.root.SetActive(true);
        if (view.image != null)
            view.image.sprite = sprite;
        if (view.label != null)
            view.label.text = turns.ToString();
    }

    void CompactStatuses()
    {
        int write = 0;
        for (int read = 0; read < _statuses.Length; read++)
        {
            if (_statuses[read].icon == StatusIcon.None)
                continue;
            if (write != read)
            {
                CopySlot(_statuses[read], _statuses[write]);
                ClearSlot(_statuses[read]);
            }
            write++;
        }
    }

    static void CopySlot(StatusView from, StatusView to)
    {
        to.icon = from.icon;
        if (to.root != null)
            to.root.SetActive(true);
        if (to.image != null)
            to.image.sprite = from.image != null ? from.image.sprite : null;
        if (to.label != null)
            to.label.text = from.label != null ? from.label.text : "";
    }

    static void ClearSlot(StatusView view)
    {
        view.icon = StatusIcon.None;
        if (view.image != null)
            view.image.sprite = null;
        if (view.label != null)
            view.label.text = "";
        if (view.root != null)
            view.root.SetActive(false);
    }

    int FindIcon(StatusIcon icon)
    {
        for (int i = 0; i < _statuses.Length; i++)
        {
            if (_statuses[i].icon == icon)
                return i;
        }
        return -1;
    }

    public void RecoverMana()
    {
        currentMana = Mathf.Clamp(currentMana + manaRegen, 0, manaMax);
        if (General.Instance != null)
            General.Instance.Refresh(this);
    }

    public bool SpendMana(int amount)
    {
        if (amount < 0 || currentMana < amount)
            return false;
        currentMana -= amount;
        if (General.Instance != null)
            General.Instance.Refresh(this);
        return true;
    }

    public void TakeSkillDamage(int damage)
    {
        health -= damage;
        PlayHurt();
        DamagePopup.Spawn(this, damage);
    }

    public bool Cast(Role target)
    {
        Skill skill = ActionSkills.Instance != null ? ActionSkills.Instance.Selected : null;
        if (skill == null || target == null || !SpendMana(skill.mana))
            return false;

        ApplySkillDamage(target, skill);
        ActionSkills.Instance.ClearSelection();
        return true;
    }

    public void ApplySkillDamage(Role target, Skill skill)
    {
        if (target == null || skill == null)
            return;

        int damage = magicAttack * skill.power / 100;
        target.TakeSkillDamage(damage);
    }
}
