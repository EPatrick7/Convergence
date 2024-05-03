using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Rendering;

//If you ever add more floats to GravityBody, be sure to adjust the sizeof(float) * # of floats in buffer setup, and the corresponding struct in compute shader!
[System.Serializable]
public struct GravityBody
{
    public float x;
    public float y;
    public Vector4 dx;
    public Vector4 dy;

    public float mass;
    public float dense;

    public Vector3 elements;
    

    public uint id;
    public GravityBody(uint id)
    {
        this.id = id;
        x = 0;
        y = 0;
        dx = Vector4.zero;
        dy = Vector4.zero;
        mass = 0;
        elements = new Vector3(1,0,0);
        dense = 1;
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
    public void updateElements(Vector3 el)
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
            b.dense = g.GetComponent<PixelManager>().density();
            b.elements = g.GetComponent<PixelManager>().elements();
            g.transform.localScale =Vector3.one * b.mass/b.dense;


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

    GravUniverse gravUniverse;//Where the simulation data is stored.
    ComputeBuffer bodyBuffer; //The buffer for all bodies in the simulation
    bool asyncDone; // Whether or not the compute shader is done working
    int NUM_FLOATS=15;
    int NUM_UINTS = 1;





    [Header("Init")]
    [Tooltip("The player pixel prefab")]
    public GameObject Player;
    [Tooltip("The pixel prefab for spawning")]
    public GameObject Pixel;
    [Tooltip("The Black Hole prefab for spawning")]
    public GameObject BlackHole;
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

    [Tooltip("Should random element compositions be generated for non player bodies")]
    public bool InitRandomElementComposition;

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

    [Header("Time Managers")]
    [Tooltip("How much physics time should occur between each gravity check.")]
    public float TimePerGravitySample = 0.1f;
    [Header("Debug"),Tooltip("Should the pixles get colors based on acceleration")]
    public bool DoStressColors = true;
    [Tooltip("Should the pixels get colors based on elements")]
    public bool DoElementalColors = false;
    [Tooltip("What the stress colors are (left is no movement, right is heavy gravity)")]
    public Gradient AccelerationColoring;
    [Tooltip("The upper bound on stress gravity (Final Color = Gravity Force / MaxStress)")]
    public float MaxStress;
    [Tooltip("The color upper bound on elemental color (Final Color = Element Amount / ElementScale)")]
    public float ElementScale=1;
    [Tooltip("The number of gravity steps that have been applied so far.")]
    public int SimulationStep;
    [Header("Sprite Replacers")]

    [Tooltip("The sprite applied when terra is the majority element")]
    public Sprite Terra;
    [Tooltip("The sprite applied when ice is the majority element")]
    public Sprite Ice;
    [Tooltip("The sprite applied when gas is the majority element")]
    public Sprite Gas;
    [Tooltip("The sprite applied when the object is a black hole")]
    public Sprite None;
    [Tooltip("Should sprites be updated to reflect element amounts")]
    public bool DoBasicReplacement;
    public event Action Initialized;
    public void Respawn()
    {
        if (gravUniverse.numBodies < SpawnCount)
        {
            Vector2 loc = UnityEngine.Random.insideUnitCircle * (SpawnRadius);

            if(Camera.main!=null)
            {
                if(Vector2.Distance(loc,Camera.main.transform.position)< respawn_dist)
                {
                    return;
                }
            }

            if (loc.sqrMagnitude <= (2*InnerSpawnRadius) * (2*InnerSpawnRadius))
            {
                loc = loc.normalized * (SpawnRadius + loc.sqrMagnitude);
            }
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
            elements *= 255;

            if (InitRandomElementComposition)
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

        RegisterBody(Instantiate(BlackHole, Vector2.zero, Player.transform.rotation, transform), Vector2.zero);

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
            elements *= 255;

            if (InitRandomElementComposition)
            {
                RegisterBody(Instantiate(Pixel, transform.position + new Vector3(loc.x, loc.y, 0), Pixel.transform.rotation, transform), sharedVelocity, elements);
            }
            else
            {
                RegisterBody(Instantiate(Pixel, transform.position + new Vector3(loc.x, loc.y, 0), Pixel.transform.rotation, transform), sharedVelocity);
            }


        }

        Vector2 playerLoc = UnityEngine.Random.insideUnitCircle * SpawnRadius;

        if (playerLoc.sqrMagnitude <= InnerSpawnRadius * InnerSpawnRadius)
        {
            playerLoc = playerLoc.normalized * (SpawnRadius);
        }
        Vector2 playerVelocity = UnityEngine.Random.insideUnitCircle * InitVelocityScale;

        playerVelocity += OrbitalVector(playerLoc);
        RegisterBody(Instantiate(Player, transform.position + new Vector3(playerLoc.x, playerLoc.y, 0), Player.transform.rotation, transform), playerVelocity);

        Initialized?.Invoke();
    }
    public Vector2 OrbitalVector(Vector2 loc)
    {
        return (Vector2)Vector3.Cross(new Vector3(loc.x, loc.y, 0), new Vector3(loc.x, loc.y, 1)) * -InitOrbitScale*Mathf.Log10(Vector2.Distance(Vector2.zero,loc)/100f);
    }
    public void RegisterBody(GameObject g, Vector2 velocity,Vector3 elements)
    {
        
        g.GetComponent<PixelManager>().Terra = elements.x;
        g.GetComponent<PixelManager>().Ice = elements.y;
        g.GetComponent<PixelManager>().Gas = elements.z;
        gravUniverse.AddBody(g, velocity, g.GetComponent<Rigidbody2D>().mass);
    }
    public void RegisterBody(GameObject g,Vector2 velocity)
    {
        gravUniverse.AddBody(g,velocity,g.GetComponent<Rigidbody2D>().mass);
    }
    
    void Start()
    {

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
                    body.dense = gravUniverse.pixels[i].GetComponent<PixelManager>().density();
                    Vector2 acceleration = gravUniverse.bodies[i].acceleration();
                    if (!float.IsNaN(acceleration.x) && !float.IsNaN(acceleration.y))
                    {
                        //Update acceleration of gravity

                        if(DoBasicReplacement)
                        {
                            Sprite targ = Terra;
                            float terra = gravUniverse.pixels[i].GetComponent<PixelManager>().Terra;
                            float gas = gravUniverse.pixels[i].GetComponent<PixelManager>().Gas;
                            float ice = gravUniverse.pixels[i].GetComponent<PixelManager>().Ice;
                            if (terra>= gas && terra >=ice)
                            {
                                //Terra largest!
                                targ = Terra;
                            }
                            else if (gas >= terra && gas >= ice)
                            {
                                //Gas largest!
                                targ = Gas;
                            }
                            else if (ice >= terra && ice >= gas)
                            {
                                //Ice largest!
                                targ = Ice;
                            }
                            if (gravUniverse.pixels[i].GetComponent<PixelManager>().ConstantMass)
                                targ = None;

                            gravUniverse.pixels[i].GetComponent<SpriteRenderer>().sprite=targ;
                        }
                        else if (DoStressColors)
                        {
                            gravUniverse.pixels[i].GetComponent<SpriteRenderer>().color = Color.Lerp(gravUniverse.pixels[i].GetComponent<SpriteRenderer>().color, AccelerationColoring.Evaluate(new Vector2(acceleration.x, acceleration.y).sqrMagnitude / MaxStress), 0.1f);
                        }
                        else if(DoElementalColors)
                        {
                            gravUniverse.pixels[i].GetComponent<SpriteRenderer>().color = Color.Lerp(gravUniverse.pixels[i].GetComponent<SpriteRenderer>().color, new Color(gravUniverse.bodies[i].elements.x/ElementScale, gravUniverse.bodies[i].elements.y / ElementScale, gravUniverse.bodies[i].elements.z / ElementScale, 1), 0.1f);
                        }

                        gravUniverse.pixels[i].GetComponent<SpriteRenderer>().sortingOrder = Mathf.RoundToInt(body.mass);
                        gravUniverse.pixels[i].transform.localScale = Vector3.Lerp(gravUniverse.pixels[i].transform.localScale,  Vector3.one * gravUniverse.bodies[i].mass/gravUniverse.pixels[i].GetComponent<PixelManager>().density(),0.1f);
                        gravUniverse.pixels[i].GetComponent<Rigidbody2D>().velocity += acceleration;
                    }
                    else
                    {
                        //Debug.Log("NAN acceleration at bodies[" + i + "]!");
                    }
                    //Reset for next pass (each of the multithreaded gravity calcs just += acceleration, so we must reset after fetching)
                   

                    //Update position for next pass
                    body.resetVectors(new Vector2(gravUniverse.pixels[i].transform.position.x, gravUniverse.pixels[i].transform.position.y));
                    body.updateElements(new Vector3(gravUniverse.pixels[i].GetComponent<PixelManager>().Terra, gravUniverse.pixels[i].GetComponent<PixelManager>().Ice, gravUniverse.pixels[i].GetComponent<PixelManager>().Gas));
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
