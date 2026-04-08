using UnityEngine;
using UnityEngine.SceneManagement;
public class Menu : MonoBehaviour
{
    public GameObject settingsPanel;
    private bool isOpen = false;

    void Awake()
    {
        settingsPanel.SetActive(isOpen);
    }
    public void OnPlayButton()
    {
        int scene = SettingsManager.Instance.GetDifficultyAsInt()+1;
        Debug.Log(scene);
        SceneManager.LoadScene(scene);
        SettingsManager.Instance.gameState = GameState.inProgress;
    }
    public void OnQuitButton()
    {
        Application.Quit(); // Works in build, not in Editor
    }

    public void OnToggleSettingsButton()
    {
        isOpen = !isOpen;
        settingsPanel.SetActive(isOpen);
    }
}