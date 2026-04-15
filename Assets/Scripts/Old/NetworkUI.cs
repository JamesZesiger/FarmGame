using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkUI : MonoBehaviour
{
    private int _sceneIndex = 0;
    private string _sceneName;
    public GameObject NetworkPanel;
    public Menu menu;

    void Awake()
    {
        GetNextScene();
    }
    void GetNextScene()
    {
        _sceneIndex = SettingsManager.Instance.GetDifficultyAsInt()+1;
        string scenePath = SceneUtility.GetScenePathByBuildIndex(_sceneIndex);
         _sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
    }
    public void OnHostButton()
    {
        GetNextScene();

        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            bool started = NetworkManager.Singleton.StartHost();
            if (!started)
                return;

            if (SceneManager.GetActiveScene().name != _sceneName)
            {
                Debug.Log($"{_sceneName}");
                NetworkManager.Singleton.SceneManager.LoadScene(_sceneName, LoadSceneMode.Single);
                SettingsManager.Instance.timerStarted = true;
            }
        }
    }

    public void OnClientButton()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.StartClient();
        }
    }
}
