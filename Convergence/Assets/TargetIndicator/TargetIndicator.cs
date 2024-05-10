using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TargetIndicator : MonoBehaviour
{
    [SerializeField]
    private Image targetIndicatorImage;

    [SerializeField]
    private Image offscreenTargetIndicatorImage;

    [SerializeField]
    private float OutOfSightOffset = 20f;

    private float outOfSightOffset { get { return OutOfSightOffset /* canvasRect.LocalScale.x */; } }

    private GameObject target;

    private new Camera camera;

    private RectTransform canvasRect;

    private RectTransform rectTransform;

    // Start is called before the first frame update
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void InitializeTargetIndicator(GameObject target, Camera camera, Canvas canvas)
    {
        this.target = target;
        this.camera = camera;
        canvasRect = canvas.GetComponent<RectTransform>();
        rectTransform.SetAsFirstSibling(); //set indicator as first child so UI lays over it
    }

    public void UpdateTargetIndicator()
    {
        if(target==null)
        {
            gameObject.SetActive(false);
            return;
        }
        SetIndicatorPosition();
        //Adjust distance display
        //turn on or off when in range/out of range
    }

    protected void SetIndicatorPosition()
    {
        Vector3 indicatorPos = camera.WorldToScreenPoint(target.transform.position); //get pos of target relative to screenspace

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

    private Vector3 OutOfRangeIndicatorPosB(Vector3 indicatorPos)
	{
        indicatorPos.z = 0f;

        Vector3 canvasCenter = new Vector3(canvasRect.rect.width / 2f, canvasRect.rect.height / 2f, 0f) * canvasRect.localScale.x; //calc center of canvas, scaled
        indicatorPos -= canvasCenter; //subtract from indicatorPos to get pos from origin

        float divX = (canvasRect.rect.width / 2f - outOfSightOffset) / Mathf.Abs(indicatorPos.x); //calc if vector to target intersects w/ border of canvas rect (off screen)
        float divY = (canvasRect.rect.height / 2f - outOfSightOffset) / Mathf.Abs(indicatorPos.y);

        if (divX < divY) //if intersects w/ x border first, put x to border and THEN adjust y
		{
            float angle = Vector3.SignedAngle(Vector3.right, indicatorPos, Vector3.forward);
            indicatorPos.x = Mathf.Sign(indicatorPos.x) * (canvasRect.rect.width / 2f - outOfSightOffset) * canvasRect.localScale.x; //max to border
            indicatorPos.y = Mathf.Tan(Mathf.Deg2Rad * angle) * indicatorPos.x; //find hypotenuse of angle to target and angle to border, providing y pos translated to x on border
		}
        else
		{
            float angle = Vector3.SignedAngle(Vector3.up, indicatorPos, Vector3.forward);
            indicatorPos.y = Mathf.Sign(indicatorPos.y) * (canvasRect.rect.height / 2f - outOfSightOffset) * canvasRect.localScale.y;
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
