using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public enum Difficulty
{
    Easy,
    Hard
}
public enum GameState{unstarted, inProgress, Win, Lose}
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }
    [Header("Settings")]
    public Difficulty difficulty = Difficulty.Easy;
    public float timeRemaining = 300;
    public GameState gameState = GameState.unstarted;
    public bool timerStarted = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

    }

    public void OnDropdownChanged(int diff)
    {
        difficulty = (Difficulty)diff;
        Debug.Log(difficulty);
    }

    public int GetDifficultyAsInt()
    {
        return (int)difficulty;
    }
    void Update()
    {
        if (!timerStarted) return;
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
        }
        else
        {
            if (gameState != GameState.Win)
                if (SceneManager.GetActiveScene().name != "Credits")
                {
                    gameState = GameState.Lose;
                    NetworkManager.Singleton.SceneManager.LoadScene("Credits", LoadSceneMode.Single);
                    Destroy(this);
                }
                
        }
        if (gameState == GameState.Win)
        {
           if (SceneManager.GetActiveScene().name != "Credits")
            {
                NetworkManager.Singleton.SceneManager.LoadScene("Credits", LoadSceneMode.Single);
                Destroy(this);
            }
            
        }
    }
    
}
