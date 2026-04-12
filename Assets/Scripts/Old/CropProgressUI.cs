using UnityEngine;
using UnityEngine.UI;

public class CropProgressUI : MonoBehaviour
{
    public Image fillImage;
    public CanvasGroup canvasGroup;

    public float fadeStartDistance = 3f;
    public float fadeEndDistance = 6f;

    private Transform target;
    private float currentProgress;

    public void Initialize(Transform target)
    {
        this.target = target;

        currentProgress = 0f;
        fillImage.fillAmount = 0f;
        canvasGroup.alpha = 1f;
    }

    

    public void SetProgress(float t)
    {
        currentProgress = t;
        fillImage.fillAmount = t;
    }
    void Update()
    {
        PlayerController localPlayer = PlayerController.LocalPlayer;
        Camera cam = localPlayer != null ? localPlayer.PlayerCamera : null;

        if (target == null || cam == null || localPlayer == null) return;

        Vector3 worldPos = target.position + Vector3.up * 1.5f;
        transform.position = worldPos;
        transform.forward = cam.transform.forward;

        float dist = Vector3.Distance(localPlayer.transform.position, target.position);
        float alpha = Mathf.InverseLerp(fadeEndDistance, fadeStartDistance, dist);

        if (currentProgress >= 1f)
            alpha = 0f;

        canvasGroup.alpha = alpha;
    }
}
