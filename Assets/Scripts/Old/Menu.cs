using UnityEngine;
using UnityEngine.SceneManagement;
public class Menu : MonoBehaviour
{
    public GameObject settingsPanel;
    public GameObject StartPanel;
    private bool settingsPanelIsOpen = false;
    private bool StartPanelIsOpen = false;

    void Awake()
    {
        settingsPanel.SetActive(settingsPanelIsOpen);
        StartPanel.SetActive(settingsPanelIsOpen);
    }
    public void OnPlayButton()
    {
        int scene = SettingsManager.Instance.GetDifficultyAsInt()+1;
        Debug.Log(scene);
        SceneManager.LoadScene(scene);
        SettingsManager.Instance.gameState = GameState.inProgress;
        SettingsManager.Instance.timerStarted = !SettingsManager.Instance.timerStarted;
    }
    public void OnQuitButton()
    {
        Application.Quit(); // Works in build, not in Editor
    }

    public void OnToggleSettingsButton()
    {
        settingsPanelIsOpen = !settingsPanelIsOpen;
        settingsPanel.SetActive(settingsPanelIsOpen);
    }

    public void OnToggleStartButton()
    {
        StartPanelIsOpen = !StartPanelIsOpen;
        StartPanel.SetActive(StartPanelIsOpen);
    }
}