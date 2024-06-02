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

    [SerializeField]
    private GameObject BlackHoleIndicatorPrefab;

    [SerializeField]
    private bool isMainMenu = false;

    [Range(1, 4)]
    public int PlayerID = 1;

    public static List<IndicatorManager> instances = new List<IndicatorManager>();
    private void Start()
    {
        if(instances==null) instances = new List<IndicatorManager>();
        instances.Add(this);
    }
    private void OnDestroy()
    {
        instances.Remove(this);
    }
    public static void DisableAllIndicators()
    {
        if (instances == null) instances = new List<IndicatorManager>();
        foreach (IndicatorManager manager in instances)
        {
            manager.DisableIndicators();
        }

    }
    public static void EnableAllIndicators()
    {
        if (instances == null) instances = new List<IndicatorManager>();
        foreach (IndicatorManager manager in instances)
        {
            manager.EnableIndicators();
        }

    }

    //[SerializeField]
    //private float triggerDist;

    public float maxIndicatorAlpha;
    public float maxBHoleIndicatorAlpha;

    public Color bholeColor;
    public float bholeTriggerDist;
    public Color sunColor;
    public Color bluesunColor;
    public Color npcbholeColor;
    public float sunTriggerDist;
    public float offsetMultiplier=1;

    // Update is called once per frame
    void LateUpdate()
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
                    targetIndicators[i].UpdateColor(color);
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

    public void AddTargetIndicator(GameObject target, float tDist, Color color,bool ICB=false) //overloaded w/ tDist parameter
    {
        if (target != null && !isMainMenu)
        {
            if (ICB)
			{
                TargetIndicator indicator = GameObject.Instantiate(BlackHoleIndicatorPrefab, canvas.transform).GetComponent<TargetIndicator>();
                indicator.isCentralBlackHole = ICB;
                indicator.transform.SetAsLastSibling();
                indicator.InitializeTargetIndicator(target, camera, canvas, tDist, color, maxBHoleIndicatorAlpha);
                targetIndicators.Add(indicator);
                indicator.GetComponent<TargetIndicator>().OutOfSightOffset *= offsetMultiplier;
                indicator.indicatorManager = this;

                indicator.gameObject.layer = LayerMask.NameToLayer(string.Format("P{0}Only", PlayerID));
                indicator.offscreenTargetIndicatorImage.gameObject.layer = indicator.gameObject.layer;
            } else
			{
                TargetIndicator indicator = GameObject.Instantiate(TargetIndicatorPrefab, canvas.transform).GetComponent<TargetIndicator>();
                indicator.isCentralBlackHole = ICB;
                indicator.transform.SetAsLastSibling();
                indicator.InitializeTargetIndicator(target, camera, canvas, tDist, color, maxIndicatorAlpha);
                targetIndicators.Add(indicator);
                indicator.GetComponent<TargetIndicator>().OutOfSightOffset *= offsetMultiplier;
                indicator.indicatorManager = this;

                indicator.gameObject.layer = LayerMask.NameToLayer(string.Format("P{0}Only", PlayerID));
                indicator.offscreenTargetIndicatorImage.gameObject.layer = indicator.gameObject.layer;
                //Debug.Log(targetIndicators.Count);
            }

        }
    }

}
