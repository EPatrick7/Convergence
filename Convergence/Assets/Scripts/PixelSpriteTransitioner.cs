using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelSpriteTransitioner : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer pixelRenderer;

    [SerializeField]
    private SpriteRenderer transitionRenderer;

    [SerializeField]
    [Tooltip("The amount of time in seconds a transition takes to complete"), Min(0)]
    private float duration = .1f;

    [SerializeField]
    private List<Sprite> baseSprites;

    private TransitionQueue queue = new TransitionQueue();

    private Sprite latestTarget;

    public void UpdateTexture(Sprite target)
    {
        transitionRenderer.sortingOrder = pixelRenderer.sortingOrder + 1;

        if (latestTarget == target) return;

        latestTarget = target;

        // When the pixel is just spawned, instantly set its sprite instead of tweening
        if (baseSprites.Contains(pixelRenderer.sprite) || pixelRenderer.sprite == null)
        {
            pixelRenderer.sprite = target;
        }
        else
        {
            queue?.AddTransition(target, pixelRenderer, transitionRenderer, duration);
        }
    }

    private void OnDestroy()
    {
        queue?.Destroy();
    }
}

public class TransitionQueue
{
    private Queue<Tween> transitions = new Queue<Tween>();

    public void AddTransition(Sprite to, SpriteRenderer pixel, SpriteRenderer transition, float duration)
    {
        if (to == null || pixel == null || transition == null) return;
        
        Tween tween = DOTween.To(() => transition.color, x => transition.color = x, new Color(pixel.color.r, pixel.color.g, pixel.color.b, 0f), duration);
        tween.Pause();
        tween.OnPlay(() => OnPlay(to, pixel, transition));
        tween.OnComplete(() => OnCompleted(pixel, transition));

        transitions.Enqueue(tween);

        if (transitions.Count == 1)
            Play();
    }

    public void Play()
    {
        if (transitions.Count > 0)
        {
            transitions.Peek().Play();
        }
    }

    public void OnPlay(Sprite to, SpriteRenderer pixel, SpriteRenderer transition)
    {
        transition.color = new Color(pixel.color.r, pixel.color.g, pixel.color.b, 1f);

        transition.sprite = pixel.sprite;

        pixel.sprite = to;
    }

    public void OnCompleted(SpriteRenderer pixel, SpriteRenderer transition)
    {
        transition.sprite = pixel.sprite;

        transition.color = new Color(pixel.color.r, pixel.color.g, pixel.color.b, 1f);

        transitions.Dequeue();

        Play();
    }

    public void Destroy()
    {
        if (transitions.Count > 0 && transitions.Peek().IsActive()) transitions.Peek().Pause();

        while (transitions.Count > 0)
        {
            Tween s = transitions.Dequeue();

            s.Kill();
        }
    }
}