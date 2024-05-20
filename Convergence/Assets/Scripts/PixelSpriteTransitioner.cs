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

    public void UpdateTexture(Sprite target)
    {
        transitionRenderer.sortingOrder = pixelRenderer.sortingOrder + 1;

        if (pixelRenderer.sprite == target) return;

        // When the pixel is just spawned, instantly set its sprite instead of tweening
        if (baseSprites.Contains(pixelRenderer.sprite) || pixelRenderer.sprite == null)
        {
            pixelRenderer.sprite = target;
        }
        else
        {
            queue?.AddTransition(pixelRenderer.sprite, target, pixelRenderer, transitionRenderer, duration);
        }
    }

    private void OnDestroy()
    {
        queue?.Destroy();
    }
}

public class TransitionQueue
{
    public Queue<Sequence> transitions = new Queue<Sequence>();

    private Sequence current;
    public void AddTransition(Sprite from, Sprite to, SpriteRenderer pixel, SpriteRenderer transition, float duration)
    {
        if (from == null || to == null || pixel == null || transition == null) return;

        pixel.sprite = to;

        transition.sprite = from;
        transition.color = new Color(pixel.color.r, pixel.color.g, pixel.color.b, 1f);

        Sequence sequence = DOTween.Sequence();
        sequence.Pause();
        sequence.Append(DOTween.To(() => transition.color, x => transition.color = x, new Color(pixel.color.r, pixel.color.g, pixel.color.b, 0f), duration));
        sequence.OnComplete(() => OnCompleted());

        transitions.Enqueue(sequence);

        Play();
    }

    public void Play()
    {
        if (current == null)
        {
            current = transitions.Dequeue();

            current?.Play();
        }
    }

    public void OnCompleted()
    {
        Play();
    }

    public void Destroy()
    {
        if (current != null) current.Pause();

        while (transitions.Count > 0)
        {
            Sequence s = transitions.Dequeue();

            s.Kill();
        }
    }
}