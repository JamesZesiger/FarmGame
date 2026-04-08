using UnityEngine;
using TMPro;
public class Timer : MonoBehaviour
{
    private TMP_Text text;
    void Awake()
    {
        text = this.GetComponent<TMP_Text>();
    }
    void Update()
    {
        if (SettingsManager.Instance != null)
            text.text = $"{Mathf.FloorToInt(SettingsManager.Instance.timeRemaining / 60)}:{Mathf.FloorToInt(SettingsManager.Instance.timeRemaining % 60)}";
    }
}
