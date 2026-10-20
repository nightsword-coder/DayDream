using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class NpcDialogue : MonoBehaviour
{
    public RoleControl player;
    public float talkRange = 2.5f;

    public GameObject dialoguePanel;
    public GameObject dialogueText;
    public GameObject descriptionText;
    public GameObject acceptButton;
    public GameObject declineButton;

    [TextArea]
    public string declineText;

    bool _open;
    bool _showingChoice;
    string _greeting;

    void Start()
    {
        if (player == null)
            player = FindObjectOfType<RoleControl>();

        if (dialoguePanel == null)
            dialoguePanel = GameObject.Find("DialoguePanel");
        if (dialogueText == null)
            dialogueText = GameObject.Find("DialogueText");
        if (descriptionText == null)
            descriptionText = GameObject.Find("DescriptionText");
        if (acceptButton == null)
            acceptButton = FindNamed("AcceptButton", "Accept");
        if (declineButton == null)
            declineButton = FindNamed("DeclineButton", "Decline");

        _greeting = ReadText(dialogueText);
        if (declineButton != null)
        {
            Button button = declineButton.GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(ShowDeclineFollow);
        }

        HideAll();
    }

    void Update()
    {
        if (player == null)
            return;

        float distance = Vector2.Distance(player.transform.position, transform.position);
        if (distance > talkRange)
        {
            if (_open)
                HideAll();
            return;
        }

        if (!_open)
            ShowGreeting();

        if (!_showingChoice && Input.GetMouseButtonDown(0) && PointerOverPanel())
            ShowChoice();
    }

    void ShowGreeting()
    {
        _open = true;
        _showingChoice = false;
        SetActive(dialoguePanel, true);
        SetActive(dialogueText, true);
        WriteText(dialogueText, _greeting);
        SetActive(descriptionText, false);
        SetActive(acceptButton, false);
        SetActive(declineButton, false);
    }

    void ShowChoice()
    {
        _showingChoice = true;
        SetActive(dialogueText, false);
        SetActive(descriptionText, true);
        SetActive(acceptButton, true);
        SetActive(declineButton, true);
    }

    void ShowDeclineFollow()
    {
        _showingChoice = true;
        SetActive(descriptionText, false);
        SetActive(acceptButton, false);
        SetActive(declineButton, false);
        SetActive(dialogueText, true);
        WriteText(dialogueText, declineText);
    }

    void HideAll()
    {
        _open = false;
        _showingChoice = false;
        SetActive(dialoguePanel, false);
    }

    bool PointerOverPanel()
    {
        if (dialoguePanel == null)
            return false;

        var rect = dialoguePanel.transform as RectTransform;
        if (rect == null)
            return false;

        Camera camera = null;
        Canvas canvas = dialoguePanel.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            camera = canvas.worldCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, camera);
    }

    static GameObject FindNamed(string first, string second)
    {
        GameObject found = GameObject.Find(first);
        if (found != null)
            return found;
        return GameObject.Find(second);
    }

    static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    static string ReadText(GameObject target)
    {
        if (target == null)
            return "";

        TMP_Text tmp = target.GetComponent<TMP_Text>();
        if (tmp != null)
            return tmp.text;

        Text text = target.GetComponent<Text>();
        return text != null ? text.text : "";
    }

    static void WriteText(GameObject target, string value)
    {
        if (target == null)
            return;

        TMP_Text tmp = target.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = value;
            return;
        }

        Text text = target.GetComponent<Text>();
        if (text != null)
            text.text = value;
    }
}
