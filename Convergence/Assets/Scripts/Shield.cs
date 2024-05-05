using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shield : MonoBehaviour
{
    [Min(0), Tooltip("The proportion of ice expended when shielding")]
    public float ShieldCost = 0.01f;

    [Min(0), Tooltip("The delay in seconds for when the shield becomes active/inactive")]
    public float ShieldDelay = 0.25f;

    [Min(0), Tooltip("The interval in seconds between each shield tick")]
    public float TickRate = 0.1f;

    public Color activeColor;

    public Color inactiveColor;

    private PixelManager pixel;

    private Collider2D col;

    private SpriteRenderer sprite;

    private Tween tween;

    private void Awake()
    {
        pixel = GetComponentInParent<PixelManager>();

        col = GetComponentInParent<Collider2D>();

        Enabled(false);

        sprite = GetComponent<SpriteRenderer>();

        sprite.color = inactiveColor;
    }

    public void ShieldUp()
    {
        if (col == null || col.enabled) return;

        if (sprite == null) return;

        if (pixel.Ice < 1.0f) return;

        tween?.Kill();

        tween = DOTween.To(() => sprite.color, x => sprite.color = x, activeColor, ShieldDelay);
        tween.OnComplete(ShieldUpOnComplete);
        tween.Play();
    }

    private void ShieldUpOnComplete()
    {
        Enabled(true);

        StartCoroutine(ShieldTick(TickRate));
    }

    public void ShieldDown()
    {
        if (col == null) return;

        if (sprite == null) return;

        tween?.Kill();

        tween = DOTween.To(() => sprite.color, x => sprite.color = x, inactiveColor, ShieldDelay);
        tween.OnComplete(ShieldDownOnComplete);

        tween.Play();
    }

    private void ShieldDownOnComplete()
    {
        Enabled(false);
    }

    private IEnumerator ShieldTick(float interval)
    {
        if (col.enabled)
        {
            if (pixel.Ice > 1.0f)
            {
                pixel.Ice -= pixel.Ice * ShieldCost * interval;

                yield return interval;

                StartCoroutine(ShieldTick(interval));
            }
            else
            {
                ShieldDown();
            }
        }
    }

    private void Enabled(bool enabled)
    {
        if (col != null) col.enabled = enabled;

        transform.parent.gameObject.layer = col.enabled ? LayerMask.NameToLayer("Ignore Pixel") : LayerMask.NameToLayer("Default");
    }

    private void OnDestroy()
    {
        tween?.Kill();
    }
}
