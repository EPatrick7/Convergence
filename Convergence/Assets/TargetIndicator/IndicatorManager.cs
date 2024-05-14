using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndicatorManager : MonoBehaviour
{
    
    [SerializeField]
    private Canvas canvas;

    [SerializeField]
    private List<TargetIndicator> targetIndicators = new List<TargetIndicator>();

    [SerializeField]
    private new Camera camera;

    [SerializeField]
    private GameObject TargetIndicatorPrefab;

    //[SerializeField]
    //private float triggerDist;

    public float maxIndicatorAlpha;

    public Color bholeColor;
    public float bholeTriggerDist;
    public Color sunColor;
    public Color bluesunColor;
    public Color npcbholeColor;
    public float sunTriggerDist;
    public float offsetMultiplier=1;

    // Update is called once per frame
    void FixedUpdate()
    {
     
        if (targetIndicators.Count > 0)
		{
            for (int i = 0; i < targetIndicators.Count; i++)
			{
                if (targetIndicators[i].gameObject.activeSelf) targetIndicators[i].UpdateTargetIndicator();
			}
		}

    }

    public void RemoveTargetIndicator(GameObject target)
	{
        if (targetIndicators.Count > 0)
		{
            for (int i = 0; i < targetIndicators.Count; i++)
			{
                if (targetIndicators[i].target == target)
				{
                    targetIndicators[i].target = null;
                    //targetIndicators.Remove(targetIndicators[i]);
				}
			}
		}
	}

    public void UpdateTargetIndicatorColor(GameObject target, Color color)
	{
        if (targetIndicators.Count > 0)
		{
            for (int i = 0; i < targetIndicators.Count; i++)
			{
                if (targetIndicators[i].target == target)
				{
                    targetIndicators[i].GetComponent<TargetIndicator>().UpdateColor(color);
				}
			}
		}
	}

    public void DisableIndicators()
	{
        if(targetIndicators.Count > 0)
        {
            for (int i = 0; i < targetIndicators.Count; i++)
            {
                if (targetIndicators[i] != null)
                    targetIndicators[i].offscreenTargetIndicatorImage.enabled = false;
            }
        }
    }

    public void EnableIndicators()
	{
        if (targetIndicators.Count > 0)
        {
            for (int i = 0; i < targetIndicators.Count; i++)
            {
                targetIndicators[i].offscreenTargetIndicatorImage.enabled = true;
            }
        }
    }

    /*
    public void AddTargetIndicator(GameObject target)
	{
        if (target != null)
        {
            TargetIndicator indicator = GameObject.Instantiate(TargetIndicatorPrefab, canvas.transform).GetComponent<TargetIndicator>();
            //GameObject glow = GameObject.Instantiate(TargetIndicatorGlowPrefab, this.transform);
            indicator.InitializeTargetIndicator(target, camera, canvas, triggerDist, maxIndicatorAlpha);
            targetIndicators.Add(indicator);
        }
	}
    */

    public void AddTargetIndicator(GameObject target, float tDist, Color color) //overloaded w/ tDist parameter
    {
        if (target != null)
        {
            TargetIndicator indicator = GameObject.Instantiate(TargetIndicatorPrefab, canvas.transform).GetComponent<TargetIndicator>();
            indicator.transform.SetAsLastSibling();
            indicator.InitializeTargetIndicator(target, camera, canvas, tDist, color, maxIndicatorAlpha);
            targetIndicators.Add(indicator);
            indicator.GetComponent<TargetIndicator>().OutOfSightOffset *= offsetMultiplier;
            indicator.indicatorManager = this;
            //Debug.Log(targetIndicators.Count);
        }
    }

}
