using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionPanel : MonoBehaviour
{
    public RectTransform leftImage;
    public RectTransform rightImage;
    public RectTransform circleImage;
    public float duration = 0.45f;
    public float stay = 1f;

    const float LeftFrom = -1440f;
    const float LeftTo = -480f;
    const float RightFrom = 1440f;
    const float RightTo = 480f;

    static TransitionPanel _instance;

    float _progress;
    bool _closing;
    bool _loadAfterCover;
    string _pendingName;
    int _pendingId;
    Coroutine _play;
    CanvasGroup _group;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        if (leftImage == null)
            leftImage = transform.Find("leftimage") as RectTransform;
        if (rightImage == null)
            rightImage = transform.Find("rightimage") as RectTransform;
        if (circleImage == null)
            circleImage = transform.Find("circleimage") as RectTransform;
        Apply(0f);
        _group = GetComponent<CanvasGroup>();
        if (_group == null)
            _group = gameObject.AddComponent<CanvasGroup>();
        SetBlocking(false);

        Canvas canvas = GetComponentInParent<Canvas>();
        GameObject root = canvas != null ? canvas.transform.root.gameObject : transform.root.gameObject;
        DontDestroyOnLoad(root);
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    public static bool PlayBefore(string sceneName, int sceneId)
    {
        if (_instance == null)
            return false;

        _instance._pendingName = sceneName;
        _instance._pendingId = sceneId;
        _instance._loadAfterCover = true;
        _instance._closing = false;
        _instance.Play(1f);
        return true;
    }

    public void Hide()
    {
        if (!gameObject.activeInHierarchy || _closing)
            return;

        _closing = true;
        Play(0f);
    }

    void Play(float target)
    {
        SetBlocking(true);
        if (_play != null)
            StopCoroutine(_play);
        _play = StartCoroutine(Animate(target));
    }

    IEnumerator Animate(float target)
    {
        float start = _progress;
        float time = 0f;
        float length = Mathf.Max(0.01f, duration);
        while (time < length)
        {
            time += Time.deltaTime;
            _progress = Mathf.Lerp(start, target, Mathf.Clamp01(time / length));
            Apply(_progress);
            yield return null;
        }

        _progress = target;
        Apply(_progress);
        _play = null;
        if (target <= 0f)
            SetBlocking(false);
        if (!_closing && target >= 1f && _loadAfterCover)
            LoadPending();
        else if (!_closing && target >= 1f)
            _play = StartCoroutine(WaitThenHide());
    }

    void LoadPending()
    {
        _loadAfterCover = false;
        if (!string.IsNullOrEmpty(_pendingName))
            SceneManager.LoadScene(_pendingName);
        else
            SceneManager.LoadScene(_pendingId);
        _play = StartCoroutine(WaitThenHide());
    }

    IEnumerator WaitThenHide()
    {
        float time = 0f;
        float length = Mathf.Max(0f, stay);
        while (time < length)
        {
            if (_closing)
                yield break;
            time += Time.deltaTime;
            yield return null;
        }

        _play = null;
        Hide();
    }

    void SetBlocking(bool blocking)
    {
        if (_group == null)
            return;
        _group.blocksRaycasts = blocking;
        _group.interactable = blocking;
    }

    void Apply(float t)
    {
        if (leftImage != null)
        {
            Vector2 position = leftImage.anchoredPosition;
            position.x = Mathf.Lerp(LeftFrom, LeftTo, t);
            leftImage.anchoredPosition = position;
        }

        if (rightImage != null)
        {
            Vector2 position = rightImage.anchoredPosition;
            position.x = Mathf.Lerp(RightFrom, RightTo, t);
            rightImage.anchoredPosition = position;
        }

        if (circleImage != null)
        {
            Vector3 euler = circleImage.localEulerAngles;
            euler.z = Mathf.Lerp(-180f, 0f, t);
            circleImage.localEulerAngles = euler;

            Vector3 scale = circleImage.localScale;
            scale.x = Mathf.Lerp(0f, 1f, t);
            scale.y = Mathf.Lerp(0f, 1f, t);
            circleImage.localScale = scale;
        }
    }
}
