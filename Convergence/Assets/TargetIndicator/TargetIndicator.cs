using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using static UnityEngine.UI.Image;

public class TargetIndicator : MonoBehaviour
{
    [SerializeField]
    private Image targetIndicatorImage;

    [SerializeField]
    public Image offscreenTargetIndicatorImage;

    public float OutOfSightOffset = 20f;

    private float maxIndicatorAlpha;

    private float outOfSightOffset { get { return OutOfSightOffset /* canvasRect.LocalScale.x */; } }

    [HideInInspector]
    public GameObject target;

    private new Camera camera;

    private float triggerDist;

    private Color indicatorColor;
    private Color targetColor;

    private RectTransform canvasRect;

    [SerializeField]
    private RectTransform rectTransform;

    public IndicatorManager indicatorManager;

    private float timeElapsed;
    private float lerpDuration = 6;
    private float fadeOutDuration = 10;
    private float valueToLerp;
    private bool spawned = false;


    private RenderMode renderMode;

    private void FixedUpdate()
    {
        offscreenTargetIndicatorImage.color = Color.Lerp(offscreenTargetIndicatorImage.color, new Color(targetColor.r, targetColor.g, targetColor.b, offscreenTargetIndicatorImage.color.a),0.1f);
    }

    public void InitializeTargetIndicator(GameObject target, Camera camera, Canvas canvas, float triggerDist, Color color, float maxAlpha)
    {
        this.target = target;
        this.camera = camera;
        canvasRect = canvas.GetComponent<RectTransform>();
        renderMode = canvas.renderMode;
        rectTransform.SetAsFirstSibling(); //set indicator as first child so UI lays over it
        this.triggerDist = triggerDist;
        offscreenTargetIndicatorImage.color = color;
        targetColor = offscreenTargetIndicatorImage.color;
        maxIndicatorAlpha = maxAlpha;
    }

    public void InitializeTargetIndicator(GameObject target, Camera camera, Canvas canvas, float triggerDist, float maxAlpha) //overloaded w/o color arg
    {
        this.target = target;
        this.camera = camera;
        canvasRect = canvas.GetComponent<RectTransform>();
        renderMode = canvas.renderMode;
        rectTransform.SetAsFirstSibling(); //set indicator as first child so UI lays over it
        this.triggerDist = triggerDist;
        maxIndicatorAlpha = maxAlpha;
        targetColor = offscreenTargetIndicatorImage.color;
    }

    public void UpdateColor(Color color)
	{

        targetColor = color;
       // offscreenTargetIndicatorImage.color = new Color(color.r,color.g,color.b, offscreenTargetIndicatorImage.color.a);
	}

    public void UpdateTargetIndicator()
    {
        if (target == null)
        {
            if (spawned)
			{
                timeElapsed = 0; //reset timer
                spawned = false; //so it doesn't keep resetting
			}
            //indicatorManager.RemoveTargetIndicator(target);
            //gameObject.SetActive(false);
            //Destroy(gameObject);
            //return;
            FadeOutAlpha();
        }
        else
        {
            SetIndicatorPosition();
            //rectTransform.position = camera.WorldToViewportPoint(target.transform.position);
            SetIndicatorAlpha();
        }
        //Adjust distance display
        //turn on or off when in range/out of range
    }

    protected void SetIndicatorPosition()
    {
        Vector3 indicatorPos;//camera.WorldToScreenPoint(target.transform.position); //get pos of target relative to screenspace

        if (renderMode == RenderMode.ScreenSpaceOverlay)
        {
            indicatorPos = camera.WorldToScreenPoint(target.transform.position);

            //if target in front of camera and within bounds of frustum
            if (indicatorPos.z >= 0f && indicatorPos.x <= canvasRect.rect.width * canvasRect.localScale.x && indicatorPos.y <= canvasRect.rect.height * canvasRect.localScale.x && indicatorPos.x >= 0f && indicatorPos.y >= 0f)
            {
                indicatorPos.z = 0f; //set z to 0, since 2D
                targetOutOfSight(false, indicatorPos); //target is in sight
            }
            else if (indicatorPos.z >= 0f)
            {
                indicatorPos = OutOfRangeIndicatorPosB(indicatorPos);
                targetOutOfSight(true, indicatorPos);
            }
            else
            {
                indicatorPos *= -1f;
                indicatorPos = OutOfRangeIndicatorPosB(indicatorPos);
                targetOutOfSight(true, indicatorPos);
            }

            rectTransform.position = indicatorPos;
        }
        else
        {
            indicatorPos = camera.WorldToViewportPoint(target.transform.position);

            Vector3 camPos = camera.transform.position;
            Vector3 targetPos = target.transform.position;

            Vector3 origin = new Vector3(camPos.x, camPos.y, targetPos.z);
            Vector3 dir = (target.transform.position - origin).normalized;
            Ray ray = new Ray(origin, dir);

            float currentMinDistance = Mathf.Infinity;
            Vector3 hitPoint = Vector3.zero;
            Plane[] planes = new Plane[6];
            GeometryUtility.CalculateFrustumPlanes(camera, planes);
            for (var i = 0; i < 4; i++)
            {
                if (planes[i].Raycast(ray, out var distance))
                {
                    if (distance < currentMinDistance)
                    {
                        hitPoint = ray.GetPoint(distance);
                        currentMinDistance = distance;
                    }
                }
            }

            targetOutOfSight(Vector3.Distance(origin, targetPos) > currentMinDistance, targetPos);

            float offset = (canvasRect.rect.width * canvasRect.localScale.x * 0.35f);

            if (triggerDist < indicatorManager.bholeTriggerDist)
                offset += Mathf.Clamp((Vector3.Distance(origin, targetPos) * 0.25f), 0, canvasRect.rect.width * canvasRect.localScale.x * 0.5f);
            else
                offset *= 1.65f;

            hitPoint.x += dir.x * offset;
            hitPoint.y += dir.y * offset;
            rectTransform.position = new Vector3(hitPoint.x, hitPoint.y, indicatorPos.z);
        }
    }

    protected void FadeOutAlpha()
	{
        ///  Debug.Log(timeElapsed);

        
        var tempCol = offscreenTargetIndicatorImage.color;
        if (timeElapsed < fadeOutDuration)
		{
            tempCol.a = Mathf.Lerp(tempCol.a, 0f, timeElapsed / fadeOutDuration);
            offscreenTargetIndicatorImage.color = tempCol;
         //   Debug.Log(tempCol.a);
            timeElapsed += Time.fixedDeltaTime;
		} else
		{
            Deactivate();
		}
        
        //var fade = offscreenTargetIndicatorImage.DOFade(0f, 1);
       // fade.OnComplete(Deactivate);
	}

    protected void FadeInAlpha()
	{
        var tempCol = offscreenTargetIndicatorImage.color;
        if (timeElapsed < fadeOutDuration)
		{
            tempCol.a = Mathf.Lerp(tempCol.a, maxIndicatorAlpha, timeElapsed / fadeOutDuration);
            offscreenTargetIndicatorImage.color = tempCol;
            timeElapsed += Time.fixedDeltaTime;
		}
	}

    private void Deactivate()
	{
        gameObject.SetActive(false);
	}

    protected void SetIndicatorAlpha()
	{
        var tempCol = offscreenTargetIndicatorImage.color;
        if (timeElapsed < lerpDuration && target.transform.position != Vector3.zero) //if indicator not faded in and its not for the blackhole
        {
            tempCol.a = Mathf.Lerp(0f, maxIndicatorAlpha, timeElapsed / lerpDuration); //lerp to maxIndicatorAlpha before setting accurately for smooth fade in
            offscreenTargetIndicatorImage.color = tempCol;
            timeElapsed += Time.fixedDeltaTime;
        } else
        {
            spawned = true;
            float currentDist = Vector3.Distance(target.transform.position, camera.transform.position);

            if (triggerDist >= 2000) //if indicator is for spawn black hole
            {
                //Increase brightness as you get farther away
                float frac = (currentDist - triggerDist) / ((triggerDist * 1.3f) - triggerDist);
                if (GravityManager.Instance.world_wrap && currentDist > GravityManager.Instance.wrap_dist - 700)
                {
                    frac *= Mathf.Max(0, 1 - ((currentDist - (GravityManager.Instance.wrap_dist - 700)) / 700f));
                }
                tempCol.a = Mathf.Lerp(0f, maxIndicatorAlpha, frac);
                offscreenTargetIndicatorImage.color = tempCol;
            }
            else//if for larger planet
            {
                //Decrease brightness as you get farther away
                float frac = (currentDist - triggerDist) / ((triggerDist * 1.3f) - triggerDist);

                if (GravityManager.Instance.world_wrap && Mathf.Sqrt(transform.position.sqrMagnitude) > GravityManager.Instance.wrap_dist - 700)
                {
                    frac *= Mathf.Max(0, 1 - ((Mathf.Sqrt(transform.position.sqrMagnitude) - (GravityManager.Instance.wrap_dist - 700)) / 700f));
                }
                tempCol.a = Mathf.Lerp(maxIndicatorAlpha, 0f, frac);
                offscreenTargetIndicatorImage.color = tempCol;
            }

            // Debug.Log(offscreenTargetIndicatorImage.color);
            /*
            if (currentDist >= triggerDist)
            {
                offscreenTargetIndicatorImage.enabled = true;
            } else
            {
                offscreenTargetIndicatorImage.enabled = false;
            }
            */
        }

    }

    private Vector3 OutOfRangeIndicatorPosB(Vector3 indicatorPos)
	{
        indicatorPos.z = 0f;
        float offset = outOfSightOffset;

        Vector3 canvasCenter = new Vector3(canvasRect.rect.width / 2f, canvasRect.rect.height / 2f, 0f) * canvasRect.localScale.x; //calc center of canvas, scaled
        indicatorPos -= canvasCenter; //subtract from indicatorPos to get pos from origin


        //Debug.Log(Vector3.Distance(indicatorPos, canvasCenter));

        /*
        if (Vector3.Distance(indicatorPos, canvasCenter) > (canvasRect.rect.width * .975) * canvasRect.localScale.x)
		{
            offset = -500;
		}
        */

        //Debug.Log(offset);

        /*
        if (indicatorPos.y > (canvasRect.rect.height * .95) * canvasRect.localScale.y) //if on top of screen
		{
            if (indicatorPos.x < (canvasRect.rect.width * .05) * canvasRect.localScale.x || indicatorPos.x > (canvasRect.rect.width * .95) * canvasRect.localScale.x) //top left or right corner
            {
                offset = -500;
            }
        } else if (indicatorPos.y < (canvasRect.rect.height * .05) * canvasRect.localScale.y) //if on bottom of screen
		{
            if (indicatorPos.x < (canvasRect.rect.width * .05) * canvasRect.localScale.x || indicatorPos.x > (canvasRect.rect.width * .95) * canvasRect.localScale.x) //bottom left or right corner
            {
                offset = -500;
            }
        }
        */


        float divX = (canvasRect.rect.width / 2f - offset) / Mathf.Abs(indicatorPos.x); //calc if vector to target intersects w/ border of canvas rect (off screen)
        float divY = (canvasRect.rect.height / 2f - offset) / Mathf.Abs(indicatorPos.y);

        if (divX < divY) //if intersects w/ x border first, put x to border and THEN adjust y
		{
            float angle = Vector3.SignedAngle(Vector3.right, indicatorPos, Vector3.forward);
            indicatorPos.x = Mathf.Sign(indicatorPos.x) * (canvasRect.rect.width / 2f - offset) * canvasRect.localScale.x; //max to border
            indicatorPos.y = Mathf.Tan(Mathf.Deg2Rad * angle) * indicatorPos.x; //find hypotenuse of angle to target and angle to border, providing y pos translated to x on border
		}
        else
		{
            float angle = Vector3.SignedAngle(Vector3.up, indicatorPos, Vector3.forward);
            indicatorPos.y = Mathf.Sign(indicatorPos.y) * (canvasRect.rect.height / 2f - offset) * canvasRect.localScale.y;
            indicatorPos.x = -Mathf.Tan(Mathf.Deg2Rad * angle) * indicatorPos.y;
		}

        indicatorPos += canvasCenter;
        return indicatorPos;
	}

    private void targetOutOfSight(bool oos, Vector3 indicatorPos)
	{
        if (oos) //declared out of sight
		{
            if (offscreenTargetIndicatorImage.gameObject.activeSelf == false) offscreenTargetIndicatorImage.gameObject.SetActive(true);
            if (targetIndicatorImage.isActiveAndEnabled == true) targetIndicatorImage.enabled = false;

            offscreenTargetIndicatorImage.rectTransform.rotation = Quaternion.Euler(rotationOutOfSightTargetIndicator(indicatorPos));

		} else
		{
            if (offscreenTargetIndicatorImage.gameObject.activeSelf == true) offscreenTargetIndicatorImage.gameObject.SetActive(false);
            if (targetIndicatorImage.isActiveAndEnabled == false) targetIndicatorImage.enabled = true;
		}
	}

    private Vector3 rotationOutOfSightTargetIndicator(Vector3 indicatorPos)
	{
        Vector3 canvasCenter = new Vector3(canvasRect.rect.width / 2f, canvasRect.rect.height / 2f, 0f) * canvasRect.localScale.x; //get center of canvas
        float angle = Vector3.SignedAngle(Vector3.up, indicatorPos - canvasCenter, Vector3.forward); //calc signedAngle between pos of indicator and direction Up
        return new Vector3(0f, 0f, angle);
	}

}
