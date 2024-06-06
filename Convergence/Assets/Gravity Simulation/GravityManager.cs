using JetBrains.Annotations;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.InputSystem;
using UnityEngine.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

#region DataStructure
[System.Serializable]
public struct OnlineBodyUpdate
{
    public int id;
    public Vector2 pos;
    public Vector2 vel;
    public Vector2 acc;
    public float mass;
    public Vector2 elements;
    public double time;
    private static readonly DateTime referencePoint = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);


    public OnlineBodyUpdate(GravityBody body,PixelManager pixel)
    {
        id = body.id;
        pos = new Vector2(body.x, body.y);
        acc = new Vector2(body.dx, body.dy);
        vel = pixel.rigidBody.velocity;
        mass = body.mass;
        elements = body.elements;
        time = (DateTime.UtcNow - referencePoint).TotalSeconds;
    }
    public void UpdateBody(GravityBody body,PixelManager pixel)
    {
        body.id =id;
        body.x = pos.x;
        body.y = pos.y;
        body.dx = acc.x;
        body.dy = acc.y;
        body.mass = mass;
        body.elements = elements;

        pixel.transform.position = new Vector3(pos.x,pos.y,pixel.transform.position.z);
        pixel.rigidBody.mass = mass;
        pixel.rigidBody.velocity = vel;
        pixel.Ice = body.elements[0];
        pixel.Gas = body.elements[1];

    }
}

//If you ever add more floats to GravityBody, be sure to adjust the sizeof(float) * # of floats in buffer setup, and the corresponding struct in compute shader!
[System.Serializable]
public struct GravityBody
{
    public float x;
    public float y;
    public float dx;
    public float dy;

    public float mass;
    public float radius;

    public Vector2 elements;


    public int id;
    public int sendOnly;
    public GravityBody(int id)
    {
        this.id = id;
        x = 0;
        y = 0;
        dx = 0;
        dy = 0;
        mass = 0;
        elements = new Vector3(0, 0);
        radius = 1;
        sendOnly = 0;
    }
    public void makeSendOnly()
    {
        sendOnly = 1;
    }

    public Vector2 acceleration()
    {
        return new Vector2((dx)/4f, (dy/4f));
    }
    public Vector2 pos()
    {
        return new Vector2(x, y);
    }
    public void resetVectors(Vector2 pos)
    {
        dx = 0;
        dy = 0;
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
            onlineBodies = new List<OnlineBodyUpdate>();
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
            if (g.GetComponent<PlayerPixelManager>() == null)
                g.name = "Body (" + numBodies + ")";

            pixels.Add(g);
            bodies.Add(b);

            genBodies++;
            numBodies++;

            if (GravityManager.Instance.isOnline&&g.GetComponent<PhotonView>()!=null&&!g.GetComponent<PhotonView>().IsMine)
            {
                b.makeSendOnly();
            }
        }
        public int FetchBody(int id)
        {
            return bodies.BinarySearch(new GravityBody(id), new BodyComparer());
        }
        public bool RemoveBody(int id)
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
        public List<OnlineBodyUpdate> onlineBodies;//The array of all incoming updates from the server;
        public int numBodies;//Number of bodies in use;
        private int genBodies;//Number of generated bodies;
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
    int NUM_FLOATS=8;
    int NUM_INTS = 2;
    public static GravityManager Instance;


    public static PlayerPixelManager GameWinner;
    [HideInInspector]
    public bool isMultiplayer;
    [HideInInspector]
    public bool isOnline;
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
    [Tooltip("Whether or not to teleport objects nearing the world border to the other side of the world.")]
    public bool world_wrap=false;

    public enum BorderNPCBehavior {None,Despawn,Wrap,WrapHybrid};
    [Tooltip("How should NPC planets react to exiting the galaxy?")]
    public BorderNPCBehavior borderBehavior;

    [Header("Time Managers")]
    [Tooltip("How much physics time should occur between each gravity check.")]
    public float TimePerGravitySample = 0.1f;

    [Header("Debug")]
    [Tooltip("The number of gravity steps that have been applied so far.")]
    public int SimulationStep;
    public bool MultiplayerCameraTest;

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
    [Tooltip("If true, the gravity simulation will wait for some player input before running gravity")]
    public bool WaitForPlayerInput;

    [Header("Player Buffs")]
    [Tooltip("How many planets will be spawned in the radius around a player.")]
    public int PlayerGoodiesCount=15;
    [Tooltip("Number of those planets that should respect the close goodies range.")]
    public int Close_PlayerGoodiesCount = 3;
    [Tooltip("Closer range of spawning radius for the ClosePlayerGoodies.")]
    public Vector2 CloseGoodiesRange = new Vector2(40, 60);
    [Tooltip("Range of spawning from which goodies will spawn around the player.")]
    public Vector2 GoodiesRange = new Vector2(40, 175);
    [Tooltip("The number of NPC planets that will receive goodies as if they were players.")]
    public int NPCGoodiesRecieverCount;


    [HideInInspector]
    public bool GameLost;
    bool isWithinCamera(CameraLook look, Vector2 loc,float inflated_radius=0.1f)
    {
        Vector3 vp = look.GetComponent<Camera>().WorldToViewportPoint(loc);
        float wiggleRoom = inflated_radius;
        if ((vp.x > -wiggleRoom && vp.x < 1 + wiggleRoom && vp.y > -wiggleRoom && vp.y < 1 + wiggleRoom))
        {//Object is within viewport, abort.
            return true;
        }
        return false;
    }
    public bool isWithinACamera(Vector2 loc,float inflated_radius=0.1f)
    {
        if (CameraLook.camLooks != null)
        {
            foreach (CameraLook look in CameraLook.camLooks)
            {
                if (isWithinCamera(look, loc, inflated_radius))
                    return true;
            }
        }
        return false;
    }

    public void Respawn()
    {
        if (gravUniverse.numBodies < SpawnCount)
        {
            Vector2 loc = UnityEngine.Random.insideUnitCircle * (SpawnRadius);

            if(isWithinACamera(loc))
            {
                return;
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
    public void PreRegister_Block(Transform c)
    {
        c.gameObject.SetActive(true);
        foreach (Transform sub_c in c.transform)
        {
            PreRegister(sub_c);
        }
    }
    public void PreRegister(Transform c)
    {
        c.gameObject.SetActive(true);
        PixelManager pixel = c.GetComponent<PixelManager>();

        if (pixel != null)
        {
            //pixel.indManagers = indManagers;
            RegisterBody(pixel.gameObject, pixel.GetComponent<Rigidbody2D>().velocity, pixel.elements());

            if (pixel.GetComponent<PlayerPixelManager>() != null)
            {
                if (pixel.GetComponent<OnlinePixelManager>()==null)
                    pixel.GetComponent<PlayerPixelManager>().PlayerID = 1;
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
    [HideInInspector]
    public Vector2 DesiredPlayerPos;
    public IEnumerator Initialize()
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


        if(PlayerCount>1)
        {
            isMultiplayer = true;
            if(!MultiplayerCameraTest)
                PlayerCount = Mathf.Clamp(Gamepad.all.Count,1,4);
        }

        if (BlackHole!=null)
        {
            GameObject bHole = Instantiate(BlackHole, Vector2.zero, Player.transform.rotation, transform); //INDIC
            bHole.GetComponent<PixelManager>().spawnBhole = true;
            if (MenuSim)
			{
                bHole.GetComponent<SpriteRenderer>().enabled = false;
                SpriteRenderer transitioner = bHole.transform.GetChild(0).GetComponent<SpriteRenderer>();
                if (transitioner != null)
                {
                    transitioner.enabled = false;
                }
            }
            RegisterBody(bHole, Vector2.zero);
            for (var i = 0; i < indManagers.Length; i++)
			{
                indManagers[i].AddTargetIndicator(bHole, indManagers[i].bholeTriggerDist, indManagers[i].bholeColor,true);
            }
        }

        foreach(Transform c in transform) //Load in existing particles.
        {
            if (c.gameObject.activeSelf)
            {
                if (c.GetComponent<PixelManager>() != null)
                {
                    PreRegister(c);
                }
                else
                {
                    PreRegister_Block(c);
                }
            }
        }
        if (!MenuSim)
        {
            if(PlayerCount<=0)
            {
                Vector2 playerLoc = UnityEngine.Random.insideUnitCircle * SpawnRadius;
                if (playerLoc.sqrMagnitude <= InnerSpawnRadius * InnerSpawnRadius)
                {
                    playerLoc = playerLoc.normalized * (SpawnRadius);
                }
                DesiredPlayerPos = playerLoc;
                foreach (CameraLook look in CameraLook.camLooks)
                {
                look.transform.position = new Vector3(DesiredPlayerPos.x, DesiredPlayerPos.y, look.transform.position.z);

                }

                GenerateGoodiesArea(DesiredPlayerPos, 1);
            }
            for (int i = 0; i < PlayerCount; i++)
            {
                Vector2 playerLoc = UnityEngine.Random.insideUnitCircle * SpawnRadius;
                if (playerLoc.sqrMagnitude <= InnerSpawnRadius * InnerSpawnRadius)
                {
                    playerLoc = playerLoc.normalized * (SpawnRadius);
                }
                Vector2 playerVelocity = UnityEngine.Random.insideUnitCircle * InitVelocityScale / 7f;

                playerVelocity += OrbitalVector(playerLoc);
                GameObject playerObj = Instantiate(Player, transform.position + new Vector3(playerLoc.x, playerLoc.y, 0), Player.transform.rotation, transform);
                RegisterBody(playerObj, playerVelocity);
                playerObj.GetComponent<PlayerPixelManager>().PlayerID = i + 1;
                foreach(CameraLook look in CameraLook.camLooks)
                {
                    if(look.PlayerID==i+1)
                    {
                        look.transform.position = new Vector3(playerObj.transform.position.x, playerObj.transform.position.y,look.transform.position.z);

                    }
                }
                foreach (PlayerHud hud in PlayerHud.huds)
                {
                    if (hud.PlayerID == playerObj.GetComponent<PlayerPixelManager>().PlayerID)
                    {

                        hud.Initialize(playerObj.GetComponent<PlayerPixelManager>());
                        break;
                    }
                }

                //Generate Goodies Area:

                GenerateGoodiesArea(playerLoc,1);

                //



            }
        }
        int k = 0;
        //Spawn and fill arrays with new generated particles
        for (int i = 0; i < SpawnCount; i++)
        {
            if((i+1)%50==0)
            {
                yield return new WaitForEndOfFrame();
            }
            Vector2 loc = UnityEngine.Random.insideUnitCircle * SpawnRadius;



            if(loc.sqrMagnitude <= InnerSpawnRadius* InnerSpawnRadius)
            {
                loc = loc.normalized * (SpawnRadius + loc.sqrMagnitude);
            }



            if (isWithinACamera(loc))
            {
                i--;
            }
            else
            {

                Vector2 sharedVelocity = UnityEngine.Random.insideUnitCircle * InitVelocityScale;

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
                GameObject pixel = Instantiate(Pixel, transform.position + new Vector3(loc.x, loc.y, 0), Pixel.transform.rotation, transform); ;

                if (k < NPCGoodiesRecieverCount && !isWithinACamera(loc, 4))
                {
                    k++;
                    if (pixel != null )
                        pixel.GetComponent<Rigidbody2D>().mass *= 12;
                    GenerateGoodiesArea(loc, 0.4f);
                }



                if (InitRandomElementComposition > 0)
                {
                    //pixel.GetComponent<PixelManager>().indManagers = indManagers; //INDIC
                    RegisterBody(pixel, sharedVelocity, elements);
                }
                else
                {
                    //pixel.GetComponent<PixelManager>().indManagers = indManagers; //INDIC
                    RegisterBody(pixel, sharedVelocity);
                }


            }
        }
        Initialized?.Invoke();


        StartCoroutine(GravRun());
    }
    void GenerateGoodiesArea(Vector2 playerLoc,float densityOffset)
    {
        for (int k = 0; k < PlayerGoodiesCount; k++)
        {

            Vector2 loc = UnityEngine.Random.insideUnitCircle.normalized;
            Vector2 sharedVelocity = UnityEngine.Random.insideUnitCircle * InitVelocityScale / 5f - loc * 3f;
            float mass_mult = 1.5f;

            if (k < Close_PlayerGoodiesCount)
            {
                loc *= UnityEngine.Random.Range(CloseGoodiesRange.x, CloseGoodiesRange.y);

            }
            else
                loc *= UnityEngine.Random.Range(GoodiesRange.x, GoodiesRange.y);

            loc *= densityOffset;


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
    [HideInInspector]
    public float wrap_dist;
    void Start()
    {
        wrap_dist = SpawnRadius * 3f;
        GameWinner = null;
        Instance = this;
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;

        StartCoroutine(Initialize());


    }

    void OnDestroy()
    {
        GameWinner = null;
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
        Color col_target = Color.white;
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
                if (pixel.GetComponent<PlayerPixelManager>()!=null && pixel.GetComponent<SpriteRenderer>().sprite != targ&&pixel.RunCutscene)
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
        {
            targ = None;
            col_target = BlackHoleColor;
        }
        if(mass>=10000&&pixel.playerPixel!=null)
        {
            col_target = BlackHoleColor;
        }

        

        pixel.UpdateTexture(targ,col_target);
    }
    public Color BlackHoleColor;
    [HideInInspector]
    public bool AllowSimulation;
    public void FreezeSimulation()
    {
        AllowSimulation = false;
        for (int i = 0; i < gravUniverse.numBodies; i++)
        {
            if (gravUniverse.pixels[i] != null)
            {
                //Does not work for central black hole.
                gravUniverse.pixels[i].GetComponent<PixelManager>().InitialVelocity = gravUniverse.pixels[i].GetComponent<Rigidbody2D>().velocity;
                gravUniverse.pixels[i].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            }
        }
    }
    public void UnfreezeSimulation()
    {
        AllowSimulation = true;
        for (int i = 0; i < gravUniverse.numBodies; i++)
        {
            if (gravUniverse.pixels[i] != null)
            {
                //Does not work for central black hole.
                gravUniverse.pixels[i].GetComponent<Rigidbody2D>().velocity=gravUniverse.pixels[i].GetComponent<PixelManager>().InitialVelocity;
                gravUniverse.pixels[i].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.None;
            }
        }
    }
    public bool SimulationFrozen()
    {
        return WaitForPlayerInput &&!AllowSimulation;
    }
    bool simulationUnfrozen()
    {
        return AllowSimulation;
    }
    public void AddUpdateToQueue(OnlineBodyUpdate onlineBodyUpdate)
    {
        gravUniverse.onlineBodies.Add(onlineBodyUpdate);
    }

    [Tooltip("DO NOT MAKE THIS TOO HIGH BECAUSE IT CONTROLS DATA SENT TO SERVER!")]
    int contextWidth= 100;
    public void CheckUpdate(int i,GravityBody body, PixelManager pixel)
    {
        if (!isOnline)
            return;
        if (pixel.isPlayer)
            return;
        if (!PhotonNetwork.IsMasterClient)
            return;

        int numSteps =Mathf.CeilToInt(gravUniverse.numBodies /(float) contextWidth);
        int k = (SimulationStep % numSteps) * contextWidth;
        if (i > k && i-10 < k)
        {
            MultiplayerManager.Instance.SendBodyUpdateEvent(new OnlineBodyUpdate(body,pixel));
        }


    }
    public Action GravRunStart;
    public IEnumerator GravRun()
    {
        //Run forever
        while (true)
        {
            GravRunStart?.Invoke();
            
            if (WaitForPlayerInput && !AllowSimulation)
            {
                yield return new WaitUntil(simulationUnfrozen);
            }
            if (DoParticleRespawn)
            {
                for(int i =1;i%8!=0&&gravUniverse.numBodies < SpawnCount;i++)
                    Respawn();
            }
            SimulationStep++;

            wrap_dist = SpawnRadius * 2f;
            foreach (CameraLook look in CameraLook.camLooks)
            {
                look.LastNumPixelsInView = look.NumPixelsInView;
                look.NumPixelsInView = 0;
                if(look.focusedPixel!=null&&look.focusedPixel.mass()>500)
                {//If at least one player is large, then switch the wrap distance to the larger one.
                    wrap_dist = SpawnRadius * 3f;
                }
            }

            if(isOnline)
            {
                //k* O(log n) Fetch the body and then run update. (K is number of updates to apply)
                foreach(OnlineBodyUpdate onlineBodyUpdate in gravUniverse.onlineBodies)
                {
                    int i = gravUniverse.FetchBody(onlineBodyUpdate.id);

                    if (i >= 0 && i < gravUniverse.bodies.Count && gravUniverse.pixels[i] != null)
                    {
                        GravityBody body = gravUniverse.bodies[i];
                        PixelManager this_pixel = gravUniverse.pixels[i].GetComponent<PixelManager>();
                        if (onlineBodyUpdate.time > this_pixel.lastTime)
                        {
                            this_pixel.lastTime = onlineBodyUpdate.time;
                            //  Debug.Log("Updating Body " + i);

                            if (this_pixel.isPlayer)
                            {
                                Debug.LogError("Online Body Update ID Leak! (Tried to update a player planet)");
                            }
                            else
                            {

                                //Update Body
                                onlineBodyUpdate.UpdateBody(body, this_pixel);

                                gravUniverse.ReplaceBody(i, body);
                            }
                        }
                    }
                }
                gravUniverse.onlineBodies.Clear();
            }

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

                    CheckUpdate(i, body, this_pixel);

                    if (this_pixel.playerPixel != null)
                    {
                        if (GameWinner != null)
                        {
                            this_pixel.rigidBody.drag = 0.25f;
                        }
                        if (this_pixel.playerPixel.camLook != null)
                        {
                            float bh_dist = Vector2.Distance(this_pixel.playerPixel.transform.position, transform.position);
                            if (bh_dist > wrap_dist && world_wrap && this_pixel.playerPixel.camLook.LastNumPixelsInView <= 1)
                            {

                                Vector3 localP = this_pixel.transform.InverseTransformPoint(this_pixel.playerPixel.camLook.transform.position);
                                this_pixel.transform.position = (transform.position - this_pixel.transform.position).normalized * wrap_dist;
                                this_pixel.playerPixel.camLook.transform.position = this_pixel.transform.TransformPoint(localP);
                            }

                            if (PlayerCount <= 1&&this_pixel.RunCutscene)
                                CutsceneManager.Instance?.DistToBlackHole(bh_dist);
                        }
                    }

                    if(PlayerRespawner.playerRespawners!=null&&this_pixel!=null&&random_respawn_players)
                    {
                        foreach(PlayerRespawner respawner in PlayerRespawner.playerRespawners)
                        {
                            if(respawner!=null && !respawner.LerpNow && Vector2.Distance(this_pixel.transform.position,respawner.transform.position) < this_pixel.transform.localScale.x*1.2f)
                            {
                                respawner.transform.position = RespawnPos();
                            }
                        }
                    }
                    if (CameraLook.camLooks != null)
                    {
                        bool inView=false;
                        foreach (CameraLook look in CameraLook.camLooks)
                        {

                            if (this_pixel != null&&this_pixel.GetComponent<PlayerPixelManager>() == null&& look.focusedPixel!=null)
                            {
                                PlayerPixelManager player = look.focusedPixel;
                                if (look.focusedPixel.shieldActivated&&!this_pixel.ConstantMass)
                                {
                                    //this_pixel
                                    if (Vector2.Distance(player.transform.position, this_pixel.transform.position)<player.ShieldRadius())
                                    {
                                       this_pixel.transform.position=player.transform.position+ (this_pixel.transform.position-player.transform.position).normalized*(player.ShieldRadius()+(this_pixel.transform.lossyScale.x));
                                    }

                                    
                                }
                                if (this_pixel.mass()>player.mass() && !(this_pixel.planetType!=PixelManager.PlanetType.BlackHole && player.planetType==PixelManager.PlanetType.BlackHole) && this_pixel.AboutToHitMutual(player))
                                {
                                    player.WarnDanger();
                                }
                            }


                            if(isWithinCamera(look,this_pixel.transform.position))
                            {
                                look.NumPixelsInView++;
                                inView = true;
                            }
                        }
                        if (gravUniverse.pixels[i].GetComponent<PlayerPixelManager>() == null)
                        {
                            if (!inView && this_pixel.planetType==PixelManager.PlanetType.BlackHole&&!this_pixel.ConstantMass&&this_pixel.mass()<SunTransition_MassReq)
                            {
                                this_pixel.planetType=PixelManager.PlanetType.Sun;
                            }

                            if (borderBehavior == BorderNPCBehavior.Despawn)
                            {
                                if (body.pos().sqrMagnitude > (SpawnRadius * SpawnRadius * 8) && !inView)
                                {
                                    Destroy(gravUniverse.pixels[i].gameObject);
                                }
                            }
                            else if (borderBehavior == BorderNPCBehavior.Wrap)
                            {
                                float bh_dist = Vector2.Distance(this_pixel.transform.position, transform.position);
                                if (bh_dist > wrap_dist*0.85f && !inView)
                                {
                                    Vector3 target = (transform.position - this_pixel.transform.position).normalized * wrap_dist * 0.85f;
                                    bool isSeen = isWithinACamera(target);
                                    if(!isSeen)
                                        this_pixel.transform.position = target;
                                }
                            }
                            else if (borderBehavior == BorderNPCBehavior.WrapHybrid)
                            {
                                if (this_pixel.mass() < SunTransition_MassReq*0.75f)
                                {
                                    if (body.pos().sqrMagnitude > (SpawnRadius * SpawnRadius * 8) && !inView)
                                    {
                                        Destroy(gravUniverse.pixels[i].gameObject);
                                    }
                                }
                                else
                                {
                                    float bh_dist = Vector2.Distance(this_pixel.transform.position, transform.position);
                                    if (bh_dist > wrap_dist * 0.85f && !inView)
                                    {
                                        Vector3 target = (transform.position - this_pixel.transform.position).normalized * wrap_dist*0.85f;
                                        bool isSeen = isWithinACamera(target);
                                        if (!isSeen)
                                            this_pixel.transform.position = target;
                                    }
                                }
                            }
                        }
                    }


                    Vector2 acceleration = gravUniverse.bodies[i].acceleration();
                    gravUniverse.pixels[i].GetComponent<PixelManager>().CheckTransitions();
                    if(!isOnline)
                        gravUniverse.pixels[i].transform.localScale = Vector3.Lerp(gravUniverse.pixels[i].transform.localScale, Vector3.one * gravUniverse.pixels[i].GetComponent<PixelManager>().radius(), 0.1f);
                    else if (this_pixel.GetComponent<PhotonView>()==null|| this_pixel.GetComponent<PhotonView>().IsMine)
                    {
                        gravUniverse.pixels[i].transform.localScale = Vector3.Lerp(gravUniverse.pixels[i].transform.localScale, Vector3.one * gravUniverse.pixels[i].GetComponent<PixelManager>().radius(), 0.1f);

                    }
                    if (!float.IsNaN(acceleration.x) && !float.IsNaN(acceleration.y))
                    {
                        //Update acceleration of gravity

                        if (gravUniverse.pixels[i].GetComponent<PixelManager>().spawnBhole == true) //put above cover galaxy particles
                        {
                            gravUniverse.pixels[i].GetComponent<SpriteRenderer>().sortingOrder = 7500;
                        } else
						{
                            gravUniverse.pixels[i].GetComponent<SpriteRenderer>().sortingOrder = Mathf.RoundToInt(Mathf.Min(32767, body.mass));
                        }
                        gravUniverse.pixels[i].GetComponent<Rigidbody2D>().velocity += acceleration;

                        if(acceleration.sqrMagnitude>15)
                        {
                            if (this_pixel.isPlayer)
                            {
                                this_pixel.playerPixel.Shield.Bonk();
                            }
                        }

                        UpdateTexture(gravUniverse.pixels[i].GetComponent<PixelManager>());
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
                bodyBuffer = new ComputeBuffer(numBodies, sizeof(float) * NUM_FLOATS+sizeof(int)*NUM_INTS);
                bodyBuffer.SetData(bodies);

                _compute.SetBuffer(0, "bodyBuffer", bodyBuffer);
                _compute.SetInt("numBodies", bodyBuffer.count);
            }
            else
            {
                _compute.SetInt("numBodies", 0);

                if (bodyBuffer == null)
                {
                    bodyBuffer = new ComputeBuffer(1, sizeof(float) * NUM_FLOATS + sizeof(int) * NUM_INTS);
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
