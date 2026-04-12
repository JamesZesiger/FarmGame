using UnityEngine;
using UnityEngine.UI;

public class CropProgressUI : MonoBehaviour
{
    public Image fillImage;
    public CanvasGroup canvasGroup;

    public float fadeStartDistance = 3f;
    public float fadeEndDistance = 6f;

    Transform _target;
    float currentProgress;
    PlayerController _localPlayer;
    Camera _localCamera;

    public void Initialize(Transform target)
    {
        _target = target;

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
        ResolveLocalReferences();

        if (_localPlayer == null || _localCamera == null)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        if (_target == null)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        Vector3 worldPos = _target.position + Vector3.up * 1.5f;
        transform.position = worldPos;
        transform.forward = _localCamera.transform.forward;

        float dist = Vector3.Distance(_localPlayer.transform.position, _target.position);
        float alpha = Mathf.InverseLerp(fadeEndDistance, fadeStartDistance, dist);

        if (currentProgress >= 1f)
            alpha = 0f;

        canvasGroup.alpha = alpha;
    }

    void ResolveLocalReferences()
    {
        if (_localPlayer == null || !_localPlayer.IsSpawned)
        {
            _localPlayer = PlayerController.LocalPlayer;
        }

        if (_localPlayer != null)
        {
            _localCamera = _localPlayer.PlayerCamera;
        }

        if (_localCamera == null)
        {
            _localCamera = Camera.main;
        }
    }
}
