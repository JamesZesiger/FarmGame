using UnityEngine;
using TMPro;

public class Wallet : MonoBehaviour
{
    public int playerCash = 0;
    public TextMeshProUGUI text;
    void Awake()
    {
        text.text = $"${playerCash}";
    }

    // Update is called once per frame
    void Update()
    {
        text.text = $"${playerCash}";
    }

    public void UpdateWallet(int value)
    {
        playerCash += value;
        if (playerCash >= 300)
        {
            SettingsManager.Instance.gameState = GameState.Win;
        }
    }
}