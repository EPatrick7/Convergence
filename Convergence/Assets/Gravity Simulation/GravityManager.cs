using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

//If you ever add more floats to GravityBody, be sure to adjust the sizeof(float) * # of floats in buffer setup, and the corresponding struct in compute shader!
[System.Serializable]
public struct GravityBody
{
    public float x;
    public float y;
    public float dx;
    public float dy;

    public float mass;
    public float dense;

    public uint id;
    public GravityBody(uint id)
    {
        this.id = id;
        x = 0;
        y = 0;
        dx = 0;
        dy = 0;
        mass = 0;
        dense = 1;
    }
    public Vector2 acceleration()
    {
        return new Vector2(dx, dy);
    }
    public Vector2 pos()
    {
        return new Vector2(x, y);
    }
    public void setVectors(Vector2 vel,Vector2 pos)
    {
        dx = vel.x;
        dy = vel.y;
        x = pos.x;
        y = pos.y;
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
            b.dense = g.GetComponent<PixelManager>().Density;
            g.transform.localScale = new Vector3(b.mass, b.mass, b.mass);


            pixels.Add(g);
            bodies.Add(b);

            genBodies++;

            numBodies++;
        }
        public int FetchBody(uint id)
        {
            return bodies.BinarySearch(new GravityBody(id), new BodyComparer());
        }
        public void RemoveBody(uint id)
        {
            int tid = FetchBody(id);
            bodies.RemoveAt(tid);
            pixels.RemoveAt(tid);
            numBodies--;
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
    int NUM_FLOATS=6;
    int NUM_UINTS = 1;





    [Header("Init")]
    [Tooltip("The player pixel prefab")]
    public GameObject Player;
    [Tooltip("The pixel prefab for spawning")]
    public GameObject Pixel;
    [Min(-1), Tooltip("Randomized seed for world gen")]
    public int RandomSeed = -1;
    [Min(0), Tooltip("How many clusters will be spawned")]
    public int SpawnCount=100;
    [Min(0), Tooltip("How large the radius of spawning is for cluster cores")]
    public float SpawnRadius=100;
    [Min(0),Tooltip("Intensity of initial random impulse for each cluster")]
    public float InitVelocityScale;

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


    [Header("Time Managers")]
    [Tooltip("How much physics time should occur between each gravity check.")]
    public float TimePerGravitySample = 0.1f;
    [Header("Debug"),Tooltip("Should the pixles get colors based on acceleration")]
    public bool DoStressColors = true;
    [Tooltip("What the stress colors are (left is no movement, right is heavy gravity)")]
    public Gradient AccelerationColoring;
    [Tooltip("The upper bound on stress gravity (Final Color = Gravity Force / MaxStress)")]
    public float MaxStress;

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

        //Spawn and fill arrays with new generated particles
        for (int i = 0; i < SpawnCount; i++)
        {

            Vector2 loc = UnityEngine.Random.insideUnitCircle * SpawnRadius;
            Vector2 sharedVelocity = UnityEngine.Random.insideUnitCircle * InitVelocityScale;

            int index = i;

            RegisterBody(Instantiate(Pixel, transform.position + new Vector3(loc.x, loc.y, 0), Pixel.transform.rotation, transform),sharedVelocity);
            


        }

        Vector2 playerLoc = UnityEngine.Random.insideUnitCircle * SpawnRadius;
        Vector2 playerVelocity = UnityEngine.Random.insideUnitCircle * InitVelocityScale;
        RegisterBody(Instantiate(Player, transform.position + new Vector3(playerLoc.x, playerLoc.y, 0), Player.transform.rotation, transform), playerVelocity);
    }
    public void RegisterBody(GameObject g,Vector2 velocity)
    {
        gravUniverse.AddBody(g,velocity,g.transform.localScale.x);
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

            //O(n) run through each body and update it according to the last compute shader run
            int numBodies = gravUniverse.numBodies;
            for (int i = 0; i < numBodies; i++)
            {
                if (gravUniverse.pixels[i] == null)
                {
                    gravUniverse.RemoveBody(gravUniverse.bodies[i].id);
                    i--;
                    numBodies--;
                }
                else
                {
                    GravityBody body = gravUniverse.bodies[i];
                    body.mass = gravUniverse.pixels[i].GetComponent<Rigidbody2D>().mass;
                    body.dense = gravUniverse.pixels[i].GetComponent<PixelManager>().Density;
                    if (!float.IsNaN(gravUniverse.bodies[i].dx) && !float.IsNaN(gravUniverse.bodies[i].dy))
                    {
                        //Update acceleration of gravity
                        if (DoStressColors)
                        {
                            gravUniverse.pixels[i].GetComponent<SpriteRenderer>().color = Color.Lerp(gravUniverse.pixels[i].GetComponent<SpriteRenderer>().color, AccelerationColoring.Evaluate(new Vector2(gravUniverse.bodies[i].dx, gravUniverse.bodies[i].dy).sqrMagnitude / MaxStress), 0.1f);
                        }
                        gravUniverse.pixels[i].GetComponent<SpriteRenderer>().sortingOrder = Mathf.RoundToInt(body.mass);
                        gravUniverse.pixels[i].transform.localScale = Vector3.Lerp(gravUniverse.pixels[i].transform.localScale,  Vector3.one * gravUniverse.bodies[i].mass/gravUniverse.pixels[i].GetComponent<PixelManager>().Density,0.1f);
                        gravUniverse.pixels[i].GetComponent<Rigidbody2D>().velocity += gravUniverse.bodies[i].acceleration();
                    }
                    else
                    {
                        Debug.Log("NAN at bodies[" + i + "]");
                    }
                    //Reset for next pass (each of the multithreaded gravity calcs just += acceleration, so we must reset after fetching)
                   

                    //Update position for next pass
                    body.setVectors(Vector2.zero, new Vector2(gravUniverse.pixels[i].transform.position.x, gravUniverse.pixels[i].transform.position.y));
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
