using Unity.Netcode;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    public GameObject NetworkPanel;
    public Menu menu;


    public void OnHostButton()
    {
        if (NetworkManager.Singleton == null) return;

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.StartHost();

            // Load scene properly through Netcode
            NetworkManager.Singleton.SceneManager.LoadScene("Grass", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
    public void OnClientButton()
    {
         if (NetworkManager.Singleton == null)
            return;
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            menu.OnPlayButton();
            NetworkManager.Singleton.StartClient();
            
        }
    }
    
}