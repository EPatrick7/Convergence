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

    [Tooltip("The linear drag applied when shield is up.")]
    public float ShieldDrag = 0;

    public Color activeColor;

    public Color inactiveColor;

    private PlayerPixelManager player;

    private Collider2D col;

    private SpriteRenderer maskSpr;
    public SpriteRenderer overlaySpr;

    private ParticleSystem objPS;

    private Coroutine coroutine;

    private Sequence tweenSequence;

    private void Awake()
    {
        player = GetComponentInParent<PlayerPixelManager>();

        col = GetComponentInParent<Collider2D>();

        Enabled(false);

        //objPS = GetComponentInChildren<ParticleSystem>(); //For PS
        //objPS.gameObject.SetActive(false);

        maskSpr = GetComponent<SpriteRenderer>();
        maskSpr.color = inactiveColor;

        //overlaySpr.color = inactiveColor; //for additional overlay

    }

    public bool IsActive()
    {
        return col.enabled;
    }

    public void ShieldUp()
    {
        if (IsActive()) return;

        if (maskSpr == null) return;

        if (coroutine != null) return;

        coroutine = StartCoroutine(ShieldTick(TickRate));
    }

    private void ShieldUpOnComplete()
    {
        Enabled(true);
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
        
        tweenSequence?.Kill();

        tweenSequence = DOTween.Sequence();

        tweenSequence.Append(DOTween.To(() => maskSpr.color, x => maskSpr.color = x, inactiveColor, ShieldDelay));
        //seq.Insert(0, DOTween.To(() => overlaySpr.color, x => overlaySpr.color = x, inactiveColor, ShieldDelay));
        tweenSequence.OnComplete(ShieldDownOnComplete);
        tweenSequence.Play();

    }

    private void ShieldDownOnComplete()
    {
        Enabled(false);
    }

    private IEnumerator ShieldTick(float interval)
    {
        while (player.isShielding)
        {
            while (IsActive() && player.Ice > 0f)
            {
                float expendedIce = Mathf.Max(1f, Mathf.Clamp(player.mass() + player.Ice, player.Ice, player.Ice * 10f) * ShieldCost) * interval;
                player.Ice -= expendedIce * 0.75f;

                yield return interval;
            }

            if (!tweenSequence.IsActive())
            {
                if (!IsActive())
                {
                    if (player.Ice > 0f)
                    {
                        player.shieldActivated = true;

                        maskSpr.sortingOrder = player.GetComponent<SpriteRenderer>().sortingOrder + 2;
                        overlaySpr.sortingOrder = maskSpr.sortingOrder + 1;

                        /*
                        if (objPS != null)
                        {
                            objPS.gameObject.SetActive(true);
                        }
                        */


                        tweenSequence?.Kill();

                        tweenSequence = DOTween.Sequence();

                        tweenSequence.Append(DOTween.To(() => maskSpr.color, x => maskSpr.color = x, activeColor, ShieldDelay));
                        //seq.Insert(0, DOTween.To(() => overlaySpr.color, x => overlaySpr.color = x, activeColor, ShieldDelay));
                        tweenSequence.OnComplete(ShieldUpOnComplete);
                        tweenSequence.Play();
                    }
                }
                else
                {
                    player.playerPixel.shieldActivated = false;

                    ShieldDown();
                }
            }

            yield return interval;
        }

        player.playerPixel.shieldActivated = false;

        ShieldDown();

        StopCoroutine(coroutine);

        coroutine = null;
    }

    private void Enabled(bool enabled)
    {
        transform.parent.gameObject.layer = enabled ? LayerMask.NameToLayer("Ignore Pixel") : LayerMask.NameToLayer("Player");

        if (col != null) {
            col.enabled = enabled;
                }
        transform.parent.GetComponent<Rigidbody2D>().drag = enabled ? ShieldDrag:0;
    }

    private void OnDestroy()
    {
        tweenSequence?.Kill();
    }
}
