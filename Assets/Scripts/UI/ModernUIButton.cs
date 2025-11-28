using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ModernUIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float clickScale = 0.95f;
    [SerializeField] private float animationDuration = 0.1f;

    private Vector3 originalScale;
    private RectTransform rectTransform;
    private Button button;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        button = GetComponent<Button>();
        originalScale = rectTransform.localScale;
    }

    private void OnEnable()
    {
        rectTransform.localScale = originalScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.interactable) return;
        if (UIAnimator.Instance != null)
            UIAnimator.Instance.ScaleTo(rectTransform, originalScale * hoverScale, animationDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!button.interactable) return;
        if (UIAnimator.Instance != null)
            UIAnimator.Instance.ScaleTo(rectTransform, originalScale, animationDuration);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable) return;
        if (UIAnimator.Instance != null)
            UIAnimator.Instance.ScaleTo(rectTransform, originalScale * clickScale, animationDuration * 0.5f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!button.interactable) return;
        // Return to hover scale if still inside, else original
        bool isInside = RectTransformUtility.RectangleContainsScreenPoint(rectTransform, eventData.position);
        Vector3 target = isInside ? originalScale * hoverScale : originalScale;
        
        if (UIAnimator.Instance != null)
            UIAnimator.Instance.ScaleTo(rectTransform, target, animationDuration);
    }
}
