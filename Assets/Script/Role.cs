using System.Collections;
using UnityEngine;

public class Role : MonoBehaviour
{
    public Sprite portrait;

    public int health = 30;
    public int physicalAttack = 10;
    public int magicAttack = 10;
    public int manaMax = 10;
    public int moveRange = 4;
    public int range = 1;

    public int currentMana { get; private set; }

    SpriteRenderer _renderer;
    Color _color = Color.white;
    Vector3 _scale;

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null)
            _color = _renderer.color;
        _scale = transform.localScale;
        currentMana = manaMax;
    }

    public void PlayHurt()
    {
        StopAllCoroutines();
        StartCoroutine(HurtFlash());
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
    }

    public bool SpendMana(int amount)
    {
        if (amount < 0 || currentMana < amount)
            return false;
        currentMana -= amount;
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
