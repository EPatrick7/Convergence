using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GalaxyFogSpawner : MonoBehaviour
{

    [SerializeField]
    private GameObject prefab;

    [SerializeField]
    private GameObject coverPrefab;

    [SerializeField]
    private int objMin, objMax;

    [SerializeField]
    private int coverNum;

    [SerializeField]
    private int spawnRadius;

    [SerializeField]
    private float alphaVal;

    [SerializeField]
    private float coverAlphaVal;

    [SerializeField]
    private float Spread;

    [SerializeField]
    private float coverSpread;

    [SerializeField]
    private float Scale;

    [SerializeField]
    private float coverScale;

    [SerializeField]
    private Vector2 StartSpeed;

    [SerializeField]
    private List<Color> objColor = new List<Color>();


    public enum FogSeedMode {Ignore,IsMenu,IsMenuListener };
    public FogSeedMode SeedMode = FogSeedMode.Ignore;
    public static int MenuFogSeed=-1;
    // Start is called before the first frame update
    void Start()
    {
        if(SeedMode==FogSeedMode.IsMenu)
        {
            MenuFogSeed= Random.Range(10, 100000000);
            Random.InitState(MenuFogSeed); //initiates RNG w/ seed
        }
        else if(SeedMode==FogSeedMode.IsMenuListener&& MenuFogSeed>0)
        {
            Random.InitState(MenuFogSeed); //initiates RNG w/ seed
        }

        for (var i = 0; i < Random.Range(objMin, objMax); i++)
		{

            Vector2 playerLoc = UnityEngine.Random.insideUnitCircle * spawnRadius + new Vector2(transform.position.x, transform.position.y);
            GameObject obj = Instantiate(prefab, playerLoc, Quaternion.identity,transform);
            ParticleSystem objPS = obj.GetComponentInChildren<ParticleSystem>();
            objPS.randomSeed = (uint)Random.Range(10, 100000000);
            objPS.Play();
            var col = objPS.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(new GradientColorKey[] { new GradientColorKey(objColor[Random.Range(0, objColor.Count)], 0.0f), new GradientColorKey(objColor[Random.Range(0, objColor.Count)], 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(alphaVal, 0.0f), new GradientAlphaKey(alphaVal, 1.0f) });
            col.color = grad;

            var shape = objPS.shape;
            shape.scale = new Vector3(Spread, Spread, Spread);

            var psMain = objPS.main;
            psMain.startSize = Scale;

            psMain.startSpeed = new ParticleSystem.MinMaxCurve(StartSpeed.x, StartSpeed.y);

        }

        for (var i = 0; i < coverNum; i++)
		{
            Vector2 playerLoc = UnityEngine.Random.insideUnitCircle * spawnRadius+ new Vector2(transform.position.x,transform.position.y);
            GameObject obj = Instantiate(coverPrefab, playerLoc, Quaternion.identity, transform);
            ParticleSystem objPS = obj.GetComponentInChildren<ParticleSystem>();
            objPS.randomSeed = (uint)Random.Range(10, 100000000);
            objPS.Play();

            var col = objPS.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(new GradientColorKey[] { new GradientColorKey(objColor[Random.Range(0, objColor.Count)], 0.0f), new GradientColorKey(objColor[Random.Range(0, objColor.Count)], 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(coverAlphaVal, 0.0f), new GradientAlphaKey(coverAlphaVal, 1.0f) });
            col.color = grad;

            var shape = objPS.shape;
            shape.scale = new Vector3(coverSpread, coverSpread, coverSpread);

            var psMain = objPS.main;
            psMain.startSize = coverScale;

            psMain.startSpeed = new ParticleSystem.MinMaxCurve(StartSpeed.x, StartSpeed.y);
        }
    }

}
