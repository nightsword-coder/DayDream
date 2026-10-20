using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadSceneButton : MonoBehaviour
{
    public string sceneName;
    public int sceneId;

    void Awake()
    {
        Button button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(Load);
    }

    public void Load()
    {
        if (TransitionPanel.PlayBefore(sceneName, sceneId))
            return;

        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneId);
    }
}
