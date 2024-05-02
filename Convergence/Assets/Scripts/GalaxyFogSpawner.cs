using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GalaxyFogSpawner : MonoBehaviour
{

    [SerializeField]
    private GameObject prefab;

    [SerializeField]
    private int objMax;

    [SerializeField]
    private int spawnRadius;

    [SerializeField]
    private float alphaVal;

    [SerializeField]
    private List<Color> objColor = new List<Color>();

    // Start is called before the first frame update
    void Start()
    {

        //Random.InitState(42); //initiates RNG w/ seed

        for (var i = 0; i < Random.Range(5, objMax); i++)
		{

            Vector2 playerLoc = UnityEngine.Random.insideUnitCircle * spawnRadius;
            GameObject obj = Instantiate(prefab, playerLoc, Quaternion.identity,transform);
            ParticleSystem objPS = obj.GetComponentInChildren<ParticleSystem>();
            var col = objPS.colorOverLifetime;
            col.enabled = true;

            Gradient grad = new Gradient();
            grad.SetKeys(new GradientColorKey[] { new GradientColorKey(objColor[Random.Range(0, objColor.Count)], 0.0f), new GradientColorKey(objColor[Random.Range(0, objColor.Count)], 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(alphaVal, 0.0f), new GradientAlphaKey(alphaVal, 1.0f) });

            col.color = grad;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
