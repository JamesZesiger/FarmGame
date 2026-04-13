using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkUI : MonoBehaviour
{
    const string gameplaySceneName = "Grass";

    public GameObject NetworkPanel;
    public Menu menu;


    public void OnHostButton()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            bool started = NetworkManager.Singleton.StartHost();
            if (!started)
                return;

            if (SceneManager.GetActiveScene().name != gameplaySceneName)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
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
