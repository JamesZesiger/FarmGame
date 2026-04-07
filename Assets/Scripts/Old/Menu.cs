using UnityEngine;
using UnityEngine.SceneManagement;
public class Menu : MonoBehaviour
{
   public void OnPlayButton()
   {
       SceneManager.LoadScene(1); // Load Level 1
   }
   public void OnQuitButton()
   {
       Application.Quit(); // Works in build, not in Editor
   }
}