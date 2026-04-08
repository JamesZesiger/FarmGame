using UnityEngine;
using TMPro;

public class GameStateText : MonoBehaviour
{
    private TMP_Text text;
    void Awake()
    {
        text = this.GetComponent<TMP_Text>();
        text.text = $"You {SettingsManager.Instance.gameState}!";
    }
}
