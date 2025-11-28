using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIAnimator : MonoBehaviour
{
    public static UIAnimator Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optional: DontDestroyOnLoad(gameObject); if you want it persistent
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- FADE ---

    public void FadeIn(CanvasGroup group, float duration, Action onComplete = null)
    {
        StartCoroutine(FadeRoutine(group, 0f, 1f, duration, onComplete));
    }

    public void FadeOut(CanvasGroup group, float duration, Action onComplete = null)
    {
        StartCoroutine(FadeRoutine(group, 1f, 0f, duration, onComplete));
    }

    private IEnumerator FadeRoutine(CanvasGroup group, float start, float end, float duration, Action onComplete)
    {
        float elapsed = 0f;
        group.alpha = start;
        
        // Ensure blocks raycasts state
        if (end > 0.5f) 
        {
            group.interactable = true;
            group.blocksRaycasts = true;
        }
        else
        {
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        group.alpha = end;
        onComplete?.Invoke();
    }

    // --- SCALE (Pop) ---

    public void ScaleTo(RectTransform target, Vector3 endScale, float duration)
    {
        StartCoroutine(ScaleRoutine(target, target.localScale, endScale, duration));
    }

    private IEnumerator ScaleRoutine(RectTransform target, Vector3 start, Vector3 end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Simple EaseOutBack-ish or SmoothStep
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); // SmoothStep

            target.localScale = Vector3.Lerp(start, end, t);
            yield return null;
        }
        target.localScale = end;
    }
}
