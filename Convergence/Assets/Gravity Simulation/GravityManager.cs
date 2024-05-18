using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Rendering;

#region DataStructure
//If you ever add more floats to GravityBody, be sure to adjust the sizeof(float) * # of floats in buffer setup, and the corresponding struct in compute shader!
[System.Serializable]
public struct GravityBody
{
    public float x;
    public float y;
    public Vector4 dx;
    public Vector4 dy;

    public float mass;
    public float radius;

    public Vector2 elements;
    

    public uint id;
    public GravityBody(uint id)
    {
        this.id = id;
        x = 0;
        y = 0;
        dx = Vector4.zero;
        dy = Vector4.zero;
        mass = 0;
        elements = new Vector3(0,0);
        radius = 1;
    }

    public Vector2 acceleration()
    {
        return new Vector2((dx.x+ dx.y+ dx.z+ dx.w)/4f, (dy.x+ dy.y+ dy.z+ dy.w)/4f);
    }
    public Vector2 pos()
    {
        return new Vector2(x, y);
    }
    public void resetVectors(Vector2 pos)
    {
        dx = Vector4.zero;
        dy = Vector4.zero;
        x = pos.x;
        y = pos.y;
    }
    public void updateElements(Vector2 el)
    {
        elements = el;
    }
}
public class BodyComparer : IComparer<GravityBody>
{

    public int Compare(GravityBody x, GravityBody y)
    {
        return x.id.CompareTo(y.id);
    }

}
#endregion
public class GravityManager : MonoBehaviour
{
    [SerializeField, HideInInspector] ComputeShader _compute;

    [System.Serializable]
    public class GravUniverse
    {
        public GravUniverse()
        {
            bodies = new List<GravityBody>();
            pixels = new List<GameObject>();
        }
        public void AddBody(GameObject g,Vector2 initialVel,float InitialSize)
        {
            GravityBody b=new GravityBody(genBodies);

            g.GetComponent<Rigidbody2D>().velocity = initialVel;

            b.x = g.transform.position.x;
            b.y = g.transform.position.y;
            g.GetComponent<Rigidbody2D>().mass = InitialSize;
            b.mass = g.GetComponent<Rigidbody2D>().mass;
            b.radius = g.GetComponent<PixelManager>().radius();
            b.elements = g.GetComponent<PixelManager>().elements();
            g.transform.localScale =Vector3.one * b.radius;


            pixels.Add(g);
            bodies.Add(b);

            genBodies++;
            numBodies++;
        }
        public int FetchBody(uint id)
        {
            return bodies.BinarySearch(new GravityBody(id), new BodyComparer());
        }
        public bool RemoveBody(uint id)
        {
            int tid = FetchBody(id);
            if (tid >=0)
            {
                bodies.RemoveAt(tid);
                pixels.RemoveAt(tid);
                numBodies--;
                return true;
            }
            return false;
        }
        public void ReplaceBody(int id, GravityBody b)
        {
            bodies.RemoveAt(id);
            bodies.Insert(id, b);
        }
        public GravityBody[] bodyArray()
        {
            return bodies.ToArray();
        }
        public List<GravityBody> bodies;//The array of all bodies in use (bodies and pixels should have 1 to 1 correspondance)
        public List<GameObject> pixels; //The array of all pixels in use
        public int numBodies;//Number of bodies in use;
        private uint genBodies;//Number of generated bodies;
    }

    [System.Serializable]
    public class TextureMap
    {
        [Range(0, 100)]
        public float Mass;
        [Range(0,100)]
        public float Gas;
        [Range(0, 100)]
        public float Ice;
        public Sprite Texture;

        public Vector3 toVector()
        {
            return new Vector3(Mass, Gas, Ice )/100f;
        }
    }

    GravUniverse gravUniverse;//Where the simulation data is stored.
    ComputeBuffer bodyBuffer; //The buffer for all bodies in the simulation
    bool asyncDone=true; // Whether or not the compute shader is done working
    int NUM_FLOATS=14;
    int NUM_UINTS = 1;
    public static GravityManager Instance;


    public static PlayerPixelManager GameWinner;

    [Header("Init")]
    [Tooltip("If true then we will not spawn player.")]
    public bool MenuSim;
    [Tooltip("The player pixel prefab")]
    public GameObject Player;
    [Tooltip("The pixel prefab for spawning")]
    public GameObject Pixel;
    [Tooltip("The Black Hole prefab for spawning")]
    public GameObject BlackHole;
    [Range(0, 4),Tooltip("Number of player gameobjects to spawn.")]
    public int PlayerCount;
    [Min(-1), Tooltip("Randomized seed for world gen")]
    public int RandomSeed = -1;
    [Min(0), Tooltip("How many clusters will be spawned")]
    public int SpawnCount=100;
    [Min(0), Tooltip("How large the radius of spawning is for cluster cores")]
    public float SpawnRadius=100;
    [Min(0),Tooltip("How large the radius of where cluster cores cannot spawn is")]
    public float InnerSpawnRadius = 25;
    [Min(0),Tooltip("Intensity of initial random impulse for each cluster")]
    public float InitVelocityScale;
    [Tooltip("Intensity of the oribtal speed around the black hole.")]
    public float InitOrbitScale;

    [Tooltip("A random element is chosen and multiplied by this amount to set initial element amount")]
    public float InitRandomElementComposition=255;

    [Tooltip("Should particles respawn when lost?")]
    public bool DoParticleRespawn;

    [Header("Constants")]
    [Tooltip("How much the distance between each pixel is dampened in the gravity calculation")]
    public float distance_scale = 1;
    [Tooltip("The constant g in the gravity formula g*(m1*m2)/d^2")]
    public float g = 6.6743f;
    [Tooltip("The cutoff for how small d can be (if d < this value then we set d = min_dist)")]
    public float min_dist = 1;
    [Tooltip("A constant point to which all particles want to gravitate towards.")]
    public Vector2 offset_drift;
    [Tooltip("How strong the offset drift pull is for each particle.")]
    public float drift_power;
    [Tooltip("How far from the camera are respawning particles allowed?")]
    public float respawn_dist;
    [Tooltip("Should players respawn when killed?")]
    public bool respawn_players;
    [Tooltip("Should players respawn at random locations?")]
    public bool random_respawn_players=true;
    [Tooltip("The maximum amount of mass npc bodies are allowed to obtain.")]
    public float max_npc_mass = 10000;

    [Header("Time Managers")]
    [Tooltip("How much physics time should occur between each gravity check.")]
    public float TimePerGravitySample = 0.1f;

    [Header("Debug")]
    [Tooltip("The number of gravity steps that have been applied so far.")]
    public int SimulationStep;


    [Header("Sprite Replacers")]
    [Tooltip("The closest texture map is applied.")]
    public TextureMap[] textureMaps;


    [Tooltip("The sprite applied when the object is a black hole")]
    public Sprite None;
    [Tooltip("The sprite applied when the object is a sun")]
    public Sprite Sun;
    [Tooltip("The sprite applied when the object is a massive sun")]
    public Sprite LateSun;
    public event Action Initialized;

    [Header("Planetary Transitions")]
    [Tooltip("The amount of mass required before upgrading to a sun. (Downgrade is 0.9x this)")]
    public float SunTransition_MassReq = 750;
    [Tooltip("The amount of gas required before upgrading to a sun.")]
    public float SunTransition_GasReq = 1000;
    [Tooltip("The amount of mass required before upgrading to a black hole.")]
    public float BlackHoleTransition_MassReq = 7500;

    [Header("UI Utils")]
    [Tooltip("IndicatorManager object that creates indicators per target")]
    public IndicatorManager indicatorManager;

    //private List<IndicatorManager> indManagers = new List<IndicatorManager>();
    private IndicatorManager[] indManagers;

    [Header("Player Buffs")]
    [Tooltip("How many planets will be spawned in the radius around a player.")]
    public int PlayerGoodiesCount=15;
    [Tooltip("Number of those planets that should respect the close goodies range.")]
    public int Close_PlayerGoodiesCount = 3;
    [Tooltip("Closer range of spawning radius for the ClosePlayerGoodies.")]
    public Vector2 CloseGoodiesRange = new Vector2(40, 60);
    [Tooltip("Range of spawning from which goodies will spawn around the player.")]
    public Vector2 GoodiesRange = new Vector2(40, 175);
     

    public void Respawn()
    {
        if (gravUniverse.numBodies < SpawnCount)
        {
            Vector2 loc = UnityEngine.Random.insideUnitCircle * (SpawnRadius);

            if (CameraLook.camLooks!=null)
            {
                float bestDist = float.MaxValue;
                foreach (CameraLook look in CameraLook.camLooks)
                {
                    float LDist = Vector2.Distance(loc, look.transform.position);
                    if (LDist < bestDist)
                    {
                        bestDist = LDist;
                    }
                }
                if (bestDist< respawn_dist)
                {
                    return;
                }
            }

            if(Physics2D.Raycast(loc, Vector2.right, 0.1f))
            {
                return;
            }

            if (loc.sqrMagnitude <= (2*InnerSpawnRadius) * (2*InnerSpawnRadius))
            {
                loc = loc.normalized * (SpawnRadius + loc.sqrMagnitude);
            }
            Vector2 sharedVelocity = UnityEngine.Random.insideUnitCircle * InitVelocityScale;

            sharedVelocity += OrbitalVector(loc);
            Vector2 elements = Vector2.zero;
            int randomEl = UnityEngine.Random.Range(0, 3);
            switch (randomEl)
            {
                case 0:
                    elements = new Vector2(0, 0);
                    break;
                case 1:
                    elements = new Vector2(1, 0);
                    break;
                case 2:
                    elements = new Vector2(0, 1);
                    break;
            }
            elements *= InitRandomElementComposition;

            if (InitRandomElementComposition>0)
            {
                RegisterBody(Instantiate(Pixel, transform.position + new Vector3(loc.x, loc.y, 0), Pixel.transform.rotation, transform), sharedVelocity, elements);
            }
            else
            {
                RegisterBody(Instantiate(Pixel, transform.position + new Vector3(loc.x, loc.y, 0), Pixel.transform.rotation, transform), sharedVelocity);
            }
        }

    }
    public void Initialize()
    {
        if (RandomSeed <= 0)
            RandomSeed = UnityEngine.Random.Range(10,100000000);

        //This should only run at most 7 times, we run this because our multithreading demands that total particle count MUST be divisible by 8
        while(SpawnCount % 8 !=0)
        {
            SpawnCount += 1;
        }

        UnityEngine.Random.InitState(RandomSeed);
        gravUniverse = new GravUniverse();

        indManagers = FindObjectsOfType<IndicatorManager>();

        if (BlackHole!=null)
        {
            GameObject bHole = Instantiate(BlackHole, Vector2.zero, Player.transform.rotation, transform); //INDIC
            bHole.GetComponent<PixelManager>().spawnBhole = true;
            //bHole.GetComponent<SpriteRenderer>().sortingOrder = 501;
            RegisterBody(bHole, Vector2.zero);
            for (var i = 0; i < indManagers.Length; i++)
			{
                indManagers[i].AddTargetIndicator(bHole, indManagers[i].bholeTriggerDist, indManagers[i].bholeColor);
            }
        }

        foreach(Transform c in transform) //Load in existing particles.
        {
            PixelManager pixel = c.GetComponent<PixelManager>();

            if (pixel != null)
            {
                //pixel.indManagers = indManagers;
                RegisterBody(pixel.gameObject, pixel.GetComponent<Rigidbody2D>().velocity, pixel.elements());

                if (pixel.GetComponent<PlayerPixelManager>() != null)
                {
                    pixel.GetComponent<PlayerPixelManager>().PlayerID =1;
                    foreach (PlayerHud hud in PlayerHud.huds)
                    {
                        if (hud.PlayerID == pixel.GetComponent<PlayerPixelManager>().PlayerID)
                        {

                            hud.Initialize(pixel.GetComponent<PlayerPixelManager>());
                            break;
                        }
                    }
                }
            }
        }
        //Spawn and fill arrays with new generated particles
        for (int i = 0; i < SpawnCount; i++)
        {

            Vector2 loc = UnityEngine.Random.insideUnitCircle * SpawnRadius;
            if(loc.sqrMagnitude <= InnerSpawnRadius* InnerSpawnRadius)
            {
                loc = loc.normalized * (SpawnRadius + loc.sqrMagnitude);
            }
            Vector2 sharedVelocity = UnityEngine.Random.insideUnitCircle * InitVelocityScale;

            sharedVelocity += OrbitalVector(loc);
            Vector3 elements = Vector3.zero;
            int randomEl = UnityEngine.Random.Range(0, 3);
            switch(randomEl)
            {
                case 0: 
                    elements = new Vector3(1, 0, 0);
                    break;
                case 1:
                    elements = new Vector3(0, 1, 0);
                    break;
                case 2:
                    elements = new Vector3(0, 0, 1);
                    break;
            }
            elements *= InitRandomElementComposition;

            if (InitRandomElementComposition>0)
            {
                GameObject pixel = Instantiate(Pixel, transform.position + new Vector3(loc.x, loc.y, 0), Pixel.transform.rotation, transform);
                //pixel.GetComponent<PixelManager>().indManagers = indManagers; //INDIC
                RegisterBody(pixel, sharedVelocity, elements);
            }
            else
            {
                GameObject pixel = Instantiate(Pixel, transform.position + new Vector3(loc.x, loc.y, 0), Pixel.transform.rotation, transform);
                //pixel.GetComponent<PixelManager>().indManagers = indManagers; //INDIC
                RegisterBody(pixel, sharedVelocity);
            }


        }
        if (!MenuSim)
        {
            for (int i = 0; i < PlayerCount; i++)
            {
                Vector2 playerLoc = UnityEngine.Random.insideUnitCircle * SpawnRadius;
                if (playerLoc.sqrMagnitude <= InnerSpawnRadius * InnerSpawnRadius)
                {
                    playerLoc = playerLoc.normalized * (SpawnRadius);
                }
                Vector2 playerVelocity = UnityEngine.Random.insideUnitCircle * InitVelocityScale/7f;

                playerVelocity += OrbitalVector(playerLoc);
                GameObject playerObj = Instantiate(Player, transform.position + new Vector3(playerLoc.x, playerLoc.y, 0), Player.transform.rotation, transform);
                RegisterBody(playerObj, playerVelocity);
                playerObj.GetComponent<PlayerPixelManager>().PlayerID = i + 1;
                foreach (PlayerHud hud in PlayerHud.huds)
                {
                    if (hud.PlayerID== playerObj.GetComponent<PlayerPixelManager>().PlayerID)
                    {

                        hud.Initialize(playerObj.GetComponent<PlayerPixelManager>());
                        break;
                    }
                }

                //Generate Goodies Area:

                for(int k=0; k<PlayerGoodiesCount;k++)
                {

                    Vector2 loc = UnityEngine.Random.insideUnitCircle.normalized;
                    Vector2 sharedVelocity = UnityEngine.Random.insideUnitCircle * InitVelocityScale/5f-loc*3f;
                    float mass_mult = 1.5f;

                    if(k< Close_PlayerGoodiesCount)
                    {
                        loc *= UnityEngine.Random.Range(CloseGoodiesRange.x, CloseGoodiesRange.y);

                    }
                    else
                        loc *= UnityEngine.Random.Range(GoodiesRange.x, GoodiesRange.y);

                    
                    sharedVelocity += OrbitalVector(loc);
                    Vector3 elements = Vector3.zero;
                    int randomEl = UnityEngine.Random.Range(0, 3);
                    switch (randomEl)
                    {
                        case 0:
                            elements = new Vector3(1, 0, 0);
                            break;
                        case 1:
                            elements = new Vector3(0, 1, 0);
                            break;
                        case 2:
                            elements = new Vector3(0, 0, 1);
                            break;
                    }
                    elements *= InitRandomElementComposition;

                    GameObject pixel = Instantiate(Pixel, transform.position + new Vector3(playerLoc.x, playerLoc.y, 0) + new Vector3(loc.x, loc.y, 0), Pixel.transform.rotation, transform);
                    pixel.GetComponent<Rigidbody2D>().mass /= mass_mult;
                    //pixel.GetComponent<PixelManager>().indManagers = indManagers; //INDIC
                    RegisterBody(pixel, sharedVelocity, elements);

                }


                //



            }
        }
        Initialized?.Invoke();
    }
    public Vector2 RespawnPos()
    {
        Vector2 playerLoc = UnityEngine.Random.insideUnitCircle * SpawnRadius;
        if (playerLoc.sqrMagnitude <= InnerSpawnRadius * InnerSpawnRadius)
        {
            playerLoc = playerLoc.normalized * (SpawnRadius);
        }
        return playerLoc;
    }
    public Vector2 OrbitalVector(Vector2 loc)
    {
        return (Vector2)Vector3.Cross(new Vector3(loc.x, loc.y, 0), new Vector3(loc.x, loc.y, 1)) * -InitOrbitScale*Mathf.Log10(Vector2.Distance(Vector2.zero,loc)/100f);
    }
    public void RegisterBody(GameObject g, Vector2 velocity,Vector2 elements)
    {
        PixelManager pixel = g.GetComponent<PixelManager>();
        pixel.SunTransition_MassReq=SunTransition_MassReq;
        pixel.SunTransition_GasReq=SunTransition_GasReq;
        pixel.BlackHoleTransition_MassReq=BlackHoleTransition_MassReq;

        pixel.Ice = elements.x;
        pixel.Gas = elements.y;
        pixel.Initialize();

        CarefulAddBody(g, velocity);
        //gravUniverse.AddBody(g, velocity, g.GetComponent<Rigidbody2D>().mass);

        
        UpdateTexture(g.GetComponent<PixelManager>());
        
    }
    public void RegisterBody(GameObject g,Vector2 velocity)
    {
        PixelManager pixel = g.GetComponent<PixelManager>();
        pixel.SunTransition_MassReq = SunTransition_MassReq;
        pixel.SunTransition_GasReq = SunTransition_GasReq;
        pixel.BlackHoleTransition_MassReq = BlackHoleTransition_MassReq;
        pixel.Initialize();
        CarefulAddBody(g, velocity);
        //gravUniverse.AddBody(g,velocity,g.GetComponent<Rigidbody2D>().mass);
        
        UpdateTexture(g.GetComponent<PixelManager>());
        
    }
    public void CarefulAddBody(GameObject g, Vector2 velocity)
    {
        if(!isAsyncDone())
            StartCoroutine(Wait_AddBody(g, velocity));
        else
            gravUniverse.AddBody(g, velocity, g.GetComponent<Rigidbody2D>().mass);
    }
    public IEnumerator Wait_AddBody(GameObject g, Vector2 velocity)
    {
        yield return new WaitUntil(isAsyncDone);
        if(g!=null)
            gravUniverse.AddBody(g, velocity, g.GetComponent<Rigidbody2D>().mass);
    }
    
    void Start()
    {
        GameWinner = null;
        Instance = this;
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;

        Initialize();


        StartCoroutine(GravRun());
    }

    void OnDestroy()
    {
        if (bodyBuffer != null)
            bodyBuffer.Release();
    }
    Transform recurseParent(Transform t)
    {
        if (t.transform.parent == null)
        {
            return t;
        }
        else
            return recurseParent(t.transform.parent);
    }
    public void UpdateTexture(PixelManager pixel)
    {

        Sprite targ=null;
        float mass = pixel.mass();
        float gas = pixel.Gas;
        float ice = pixel.Ice;
        TextureMap closest_map = textureMaps[0];
        Vector3 elementMap = new Vector3(mass/250f,gas/(gas+ice), ice/(gas+ice));
        foreach (TextureMap map in textureMaps)
        {
            float dist = Vector3.Distance(map.toVector(), elementMap);
            if(dist < Vector3.Distance(closest_map.toVector(), elementMap))
            {
                closest_map = map;
            }
        }
        //Find the textureMap who is closest in element value to the target and apply it.
        targ = closest_map.Texture;

        if(pixel.planetType==PixelManager.PlanetType.Sun)
        {
            targ = Sun;
            if (mass > 5000)
            {
                targ = LateSun;
                if (pixel.GetComponent<PlayerPixelManager>()!=null && pixel.GetComponent<SpriteRenderer>().sprite != targ)
                {
                    CutsceneManager.Instance?.IsBlueStar();
                }
            }
        }
        else if (pixel.planetType == PixelManager.PlanetType.BlackHole)
        {
            targ = None;
        }

        if (pixel.ConstantMass)
            targ = None;

        pixel.GetComponent<SpriteRenderer>().sprite = targ;
    }
    public IEnumerator GravRun()
    {
        //Run forever
        while (true)
        {
            if(DoParticleRespawn)
            {
                for(int i =1;i%8!=0&&gravUniverse.numBodies < SpawnCount;i++)
                    Respawn();
            }
            SimulationStep++;
            //O(n) run through each body and update it according to the last compute shader run
            for (int i = 0; i < gravUniverse.numBodies; i++)
            {
                if (gravUniverse.pixels[i] == null)
                {
                    if(gravUniverse.RemoveBody(gravUniverse.bodies[i].id))
                        i--;
                }
                else if (i < gravUniverse.bodies.Count)
                {
                    GravityBody body = gravUniverse.bodies[i];
                    body.mass = gravUniverse.pixels[i].GetComponent<Rigidbody2D>().mass;
                    body.radius = gravUniverse.pixels[i].GetComponent<PixelManager>().radius();



                    PixelManager this_pixel = gravUniverse.pixels[i]?.GetComponent<PixelManager>();

                    if(this_pixel.playerPixel != null&&PlayerCount<=1)
                    {
                        CutsceneManager.Instance.DistToBlackHole(Vector2.Distance(this_pixel.playerPixel.transform.position, transform.position));
                    }
                    //float bestDist= Vector2.Distance(body.pos(), Camera.main.transform.position);
                    //float largSize= (200 + Camera.main.orthographicSize);

                    if(PlayerRespawner.playerRespawners!=null&&this_pixel!=null)
                    {
                        foreach(PlayerRespawner respawner in PlayerRespawner.playerRespawners)
                        {
                            if(respawner!=null && !respawner.LerpNow && Vector2.Distance(this_pixel.transform.position,respawner.transform.position) < this_pixel.transform.localScale.x*1.2f)
                            {
                                respawner.transform.position = RespawnPos();
                            }
                        }
                    }

                    float bestDist = float.MaxValue;
                    float largSize = float.MinValue;
                    if (CameraLook.camLooks != null)
                    {
                        foreach (CameraLook look in CameraLook.camLooks)
                        {

                            if (this_pixel != null&&this_pixel.GetComponent<PlayerPixelManager>() == null&& look.focusedPixel!=null)
                            {
                                if (look.focusedPixel.isShielding)
                                {
                                    PlayerPixelManager player = look.focusedPixel;
                                    //this_pixel
                                    if (Vector2.Distance(player.transform.position, this_pixel.transform.position)<player.ShieldRadius())
                                    {
                                       this_pixel.transform.position=player.transform.position+ (this_pixel.transform.position-player.transform.position).normalized*(player.ShieldRadius()+(this_pixel.transform.lossyScale.x));
                                    }
                                }
                            }



                            float LDist = Vector2.Distance(body.pos(), look.transform.position);
                            float LSize = (200 + look.GetComponent<Camera>().orthographicSize);


                            if (LSize > largSize)
                            {
                                LSize = largSize;
                            }
                            if (LDist < bestDist)
                            {
                                bestDist = LDist;
                            }
                        }
                        if (body.pos().sqrMagnitude > (SpawnRadius * SpawnRadius * 8) && bestDist > largSize && gravUniverse.pixels[i].GetComponent<PlayerPixelManager>() == null)
                        {
                            Destroy(gravUniverse.pixels[i].gameObject);
                        }
                    }


                    Vector2 acceleration = gravUniverse.bodies[i].acceleration();
                    gravUniverse.pixels[i].GetComponent<PixelManager>().CheckTransitions();
                    if (!float.IsNaN(acceleration.x) && !float.IsNaN(acceleration.y))
                    {
                        //Update acceleration of gravity

                        UpdateTexture(gravUniverse.pixels[i].GetComponent<PixelManager>());

                        if (gravUniverse.pixels[i].GetComponent<PixelManager>().spawnBhole == true) //put above cover galaxy particles
                        {
                            gravUniverse.pixels[i].GetComponent<SpriteRenderer>().sortingOrder = 7500;
                        } else
						{
                            gravUniverse.pixels[i].GetComponent<SpriteRenderer>().sortingOrder = Mathf.RoundToInt(Mathf.Min(32767, body.mass));
                        }
                        gravUniverse.pixels[i].transform.localScale = Vector3.Lerp(gravUniverse.pixels[i].transform.localScale,  Vector3.one * gravUniverse.pixels[i].GetComponent<PixelManager>().radius(),0.1f);
                        gravUniverse.pixels[i].GetComponent<Rigidbody2D>().velocity += acceleration;
                    }
                    else
                    {
                        //Debug.Log("NAN acceleration at bodies[" + i + "]!");
                    }
                    //Reset for next pass (each of the multithreaded gravity calcs just += acceleration, so we must reset after fetching)
                   

                    //Update position for next pass
                    body.resetVectors(new Vector2(gravUniverse.pixels[i].transform.position.x, gravUniverse.pixels[i].transform.position.y));
                    body.updateElements(new Vector2(gravUniverse.pixels[i].GetComponent<PixelManager>().Ice, gravUniverse.pixels[i].GetComponent<PixelManager>().Gas));
                    gravUniverse.ReplaceBody(i, body);

                }
            }


            GravityBody[] bodies = gravUniverse.bodyArray();
            //O(1) Update compute shader internal values (These can change freely, and cost little to just resend so we dont mind this)
            _compute.SetFloat("g", g);
            _compute.SetFloat("distance_scale", distance_scale);
            _compute.SetFloat("drift_power", drift_power);
            _compute.SetFloat("min_dist", min_dist);
            _compute.SetVector("drift", offset_drift);


            //
            int numBodies = gravUniverse.numBodies;

            //O(n) Update body buffer in the shader with new gravity body information (With new acclerations / pos etc.)
            if (numBodies > 0)
            {
                if (bodyBuffer != null)
                    bodyBuffer.Release();
                bodyBuffer = new ComputeBuffer(numBodies, sizeof(float) * NUM_FLOATS+sizeof(uint)*NUM_UINTS);
                bodyBuffer.SetData(bodies);

                _compute.SetBuffer(0, "bodyBuffer", bodyBuffer);
                _compute.SetInt("numBodies", bodyBuffer.count);
            }
            else
            {
                _compute.SetInt("numBodies", 0);

                if (bodyBuffer == null)
                {
                    bodyBuffer = new ComputeBuffer(1, sizeof(float) * NUM_FLOATS + sizeof(uint) * NUM_UINTS);
                    _compute.SetBuffer(0, "bodyBuffer", bodyBuffer);
                }

            }
            
            //O(n^2) Request the compute shader to pass through each combiation of x,y from x in [0..numBodies) and y in [0..numBodies]
            while(numBodies%8!=0)
            {
                numBodies++;
            }
            _compute.Dispatch(_compute.FindKernel("GravitySimulation"), numBodies / 8, numBodies/8,1);
            //This is still blazing fast despite O(n^2) because it runs on GPU in 8x8x1 Threads

            asyncDone = false;
            AsyncGPUReadback.Request(bodyBuffer, OnCompleteReadback);
            //After the gpu is done cooking, lets ask for our body buffer (with all calculated data back)

            yield return new WaitUntil(isAsyncDone);
            //Wait until it is done^

            //Wait for next sample time
            yield return new WaitForSecondsRealtime(TimePerGravitySample);
        }
    }
    //Is the compute shader done cooking?
    bool isAsyncDone()
    {
        return asyncDone;
    }

    //Run when we get our data back from the shader
    void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        if (request.hasError)
        {
            UnityEngine.Debug.Log("GPU readback error detected.");
            return;
        }
        gravUniverse.bodies = request.GetData<GravityBody>().ToList<GravityBody>();


        asyncDone=true;
    }

}
