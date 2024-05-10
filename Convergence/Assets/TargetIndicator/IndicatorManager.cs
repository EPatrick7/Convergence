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
    private float triggerDist;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
     
        if (targetIndicators.Count > 0)
		{
            for (int i = 0; i < targetIndicators.Count; i++)
			{
                if (targetIndicators[i].gameObject.activeSelf)
                    targetIndicators[i].UpdateTargetIndicator();
			}
		}

    }

    public void AddTargetIndicator(GameObject target)
	{
        TargetIndicator indicator = GameObject.Instantiate(TargetIndicatorPrefab, canvas.transform).GetComponent<TargetIndicator>();
        //GameObject glow = GameObject.Instantiate(TargetIndicatorGlowPrefab, this.transform);
        indicator.InitializeTargetIndicator(target, camera, canvas, triggerDist); //, glow);
        targetIndicators.Add(indicator);
	}
    
}
