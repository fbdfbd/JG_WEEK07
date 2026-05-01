using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UI_EndingPanel : MonoBehaviour
{
    [SerializeField] private Button ExitButton;
    [SerializeField] private Button RestartButton;

    private void Awake()
    {
        ExitButton.onClick.AddListener(OnClickExit);
        RestartButton.onClick.AddListener(OnClickRestart);
    }

    private void OnDestroy()
    {
        ExitButton.onClick.RemoveListener(OnClickExit);
        RestartButton.onClick.RemoveListener(OnClickRestart);
    }

    private async void OnClickExit()
    {
        await GameplayAnalyticsLogger.EndSessionAndUpload();

    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    private void OnClickRestart()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}