using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CursorItemPreview : MonoBehaviour
{
    public Image previewIcon;

    private RectTransform _rect;
    private Canvas _canvas;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        Hide();
    }

    void Update()
    {
        if (previewIcon != null && previewIcon.enabled)
            FollowMouse();
    }

    public void Show(Sprite sprite)
    {
        if (previewIcon == null)
            return;

        previewIcon.sprite = sprite;
        previewIcon.enabled = true;
    }

    public void Hide()
    {
        if (previewIcon == null)
            return;

        previewIcon.enabled = false;
    }

    private void FollowMouse()
    {
        if (Mouse.current == null || _canvas == null)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            mousePos,
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
            out Vector2 localPoint
        );

        _rect.localPosition = localPoint;
    }
}
