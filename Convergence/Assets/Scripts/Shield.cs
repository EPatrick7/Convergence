using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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

    private SpriteRenderer maskSpr;
    //public SpriteRenderer overlaySpr;

    private ParticleSystem objPS;

    private Tween tween;

    private void Awake()
    {
        pixel = GetComponentInParent<PixelManager>();

        col = GetComponentInParent<Collider2D>();

        Enabled(false);

        //objPS = GetComponentInChildren<ParticleSystem>(); //For PS
        //objPS.gameObject.SetActive(false);

        maskSpr = GetComponent<SpriteRenderer>();
        maskSpr.color = inactiveColor;

        //overlaySpr.color = inactiveColor; //for additional overlay

    }

    public void ShieldUp()
    {
        if (col == null || col.enabled) return;

        if (maskSpr == null) return;

        if (pixel.Ice < 1.0f) return;

        maskSpr.sortingOrder = pixel.GetComponent<SpriteRenderer>().sortingOrder + 1;
        //overlaySpr.sortingOrder = maskSpr.sortingOrder + 1;

        /*
        if (objPS != null)
		{
            objPS.gameObject.SetActive(true);
		}
        */


        tween?.Kill();

        var seq = DOTween.Sequence();

        seq.Append(DOTween.To(() => maskSpr.color, x => maskSpr.color = x, activeColor, ShieldDelay));
        //seq.Insert(0, DOTween.To(() => overlaySpr.color, x => overlaySpr.color = x, activeColor, ShieldDelay));
        seq.OnComplete(ShieldUpOnComplete);
        seq.Play();
        
    }

    private void ShieldUpOnComplete()
    {
        Enabled(true);

        StartCoroutine(ShieldTick(TickRate));
    }

    public void ShieldDown()
    {
        if (col == null) return;

        if (maskSpr == null) return;

        /*
        if (objPS != null)
		{
            objPS.gameObject.SetActive(false);
		}
        */
        
        tween?.Kill();

        var seq = DOTween.Sequence();

        seq.Append(DOTween.To(() => maskSpr.color, x => maskSpr.color = x, inactiveColor, ShieldDelay));
        //seq.Insert(0, DOTween.To(() => overlaySpr.color, x => overlaySpr.color = x, inactiveColor, ShieldDelay));
        seq.OnComplete(ShieldDownOnComplete);
        seq.Play();

    }

    private void ShieldDownOnComplete()
    {
        Enabled(false);
    }

    private IEnumerator ShieldTick(float interval)
    {
        while (col.enabled && pixel.Ice > 0f)
        {
            float expendedIce = Mathf.Max(1f, Mathf.Clamp(pixel.mass() + pixel.Ice, pixel.Ice, pixel.Ice * 10f) * ShieldCost) * interval;
            pixel.Ice -= expendedIce*0.75f;

            yield return interval;
        }

        ShieldDown();
    }

    private void Enabled(bool enabled)
    {
        transform.parent.gameObject.layer = enabled ? LayerMask.NameToLayer("Ignore Pixel") : LayerMask.NameToLayer("Player");

        if (col != null) col.enabled = enabled;
    }

    private void OnDestroy()
    {
        tween?.Kill();
    }
}
