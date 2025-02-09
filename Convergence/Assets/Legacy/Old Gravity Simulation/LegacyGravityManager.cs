using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEngine;
using UnityEngine.Rendering;

//If you ever add more floats to LegacyGravityBody, be sure to adjust the sizeof(float) * # of floats in buffer setup, and the corresponding struct in compute shader!
public struct LegacyGravityBody
{
    public float x;
    public float y;
    public float dx;
    public float dy;

    public float mass;
    public float close_neighbors;

    public float last_neighbor;


    public Vector2 acceleration()
    {
        return new Vector2(dx, dy);
    }
    public Vector2 pos()
    {
        return new Vector2(x, y);
    }
}
public class LegacyGravityManager : MonoBehaviour
{
    [SerializeField, HideInInspector] ComputeShader _compute;
    LegacyGravityBody[] bodies;//The array of all bodies in use (bodies and pixels should have 1 to 1 correspondance)
    GameObject[] pixels; //The array of all pixels in use
    ComputeBuffer bodyBuffer; //The buffer for all bodies in the simulation
    private float timeStart; //The time in which the simulation began
    bool asyncDone; // Whether or not the compute shader is done working
    int NUM_FLOATS=7;
    public enum RunType { Static, Dynamic };



    

    [Header("Init")]
    [Tooltip("The pixel prefab for spawning")]
    public GameObject Pixel;
    [Min(-1), Tooltip("Randomized seed for world gen")]
    public int RandomSeed = -1;
    [Min(0), Tooltip("How many clusters will be spawned")]
    public int SpawnCount=100;
    [Min(1), Tooltip("How many pixels are in each cluster")]
    public int PixelSize = 1;
    [Min(0), Tooltip("How large the radius of spawning is for cluster cores")]
    public float SpawnRadius=100;
    [Min(0.001f), Tooltip("How far particles spawn from the cluster core")]
    public float ClusterRadius = 1/25f;
    [Min(0),Tooltip("Intensity of initial impulse for each cluster")]
    public float InitVelocityScale;

    [Tooltip("What simulation to use (Classical or Compute Shader)")]
    public RunType simulationSelection;
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

    [Tooltip("How close a pixel has to be to be considered as a neighboring particle")]
    public float neighbor_cutoff;
    [Tooltip("How many neighbors must a particle have before culling neighbor gravity")]
    public float max_neighbors;
    [Tooltip("Multiplier of cutoff radius of which a pixel with neigbhors > max_neighbors will ignore")]
    public float ignore_mult;

    [Header("Time Managers")]
    [Min(1),Tooltip("How many calculations to run per frame maximum in a static simulation.")]
    public float StaticDelayTime = 2000; 
    [Tooltip("Whether or not this script will be overriding physics steps.")]
    public bool OverridePhysics;
    [Tooltip("How many gravity simulations should ideally occur each second")]
    public float targetStepsPerSecond = 13;
    [Tooltip("How many steps have occured in the simulation so far (cannot change)")]
    public int SimStep;
    [Tooltip("How many steps are actually occuring each second (average)")]
    public float StepsPerSecond;
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
            RandomSeed = Random.Range(10,100000000);

        //This should only run at most 7 times, we run this because our multithreading demands that total particle count MUST be divisible by 8
        while(SpawnCount*PixelSize % 8 !=0)
        {
            PixelSize += 1;
        }

        Random.InitState(RandomSeed);


        //Spawn and fill arrays with new generated particles
        int TotalSize = SpawnCount * PixelSize;
        pixels = new GameObject[TotalSize];
        bodies = new LegacyGravityBody[TotalSize];
        for (int i = 0; i < SpawnCount; i++)
        {

            Vector2 loc = Random.insideUnitCircle * SpawnRadius;
            Vector2 sharedVelocity = Random.insideUnitCircle * InitVelocityScale;
            for (int j = 0; j < PixelSize; j++)
            {
                int index = (i*PixelSize) + j;

                float inflated_radius=1;
                Vector2 fpos = loc+Random.insideUnitCircle*PixelSize* ClusterRadius* inflated_radius;
                for(int k=0;k<j;k++)
                {
                    int index2 = (i * PixelSize) + k;
                    if (pixels[index2] != null && Vector2.Distance(fpos, pixels[index2].transform.position) < (pixels[index2].transform.localScale.x* pixels[index2].GetComponent<CircleCollider2D>().radius))
                    {
                        inflated_radius += 0.1f;
                        fpos = loc + Random.insideUnitCircle * PixelSize * ClusterRadius * inflated_radius;
                        k = 0;
                    }
                }
                pixels[index] = Instantiate(Pixel, transform.position + new Vector3(fpos.x, fpos.y, 0), Pixel.transform.rotation, transform);
                pixels[index].GetComponent<Rigidbody2D>().velocity = sharedVelocity;

                bodies[index] = new LegacyGravityBody();
                bodies[index].x = pixels[index].transform.position.x;
                bodies[index].y = pixels[index].transform.position.y;
                bodies[index].mass = pixels[index].GetComponent<Rigidbody2D>().mass;


            }
        }
    }
    void Start()
    {
        if (!OverridePhysics&&Physics2D.simulationMode==SimulationMode2D.Script)
        {
            Physics2D.simulationMode= SimulationMode2D.FixedUpdate;
        }
        else if (OverridePhysics && Physics2D.simulationMode != SimulationMode2D.Script)
        {
            Physics2D.simulationMode = SimulationMode2D.Script;
        }

        timeStart = Time.timeSinceLevelLoad;
        Initialize();

        if(simulationSelection== RunType.Static)
            StartCoroutine(StaticRun());
        else if(simulationSelection==RunType.Dynamic)
            StartCoroutine(DynamicRun());
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
    public IEnumerator DynamicRun()
    {
        //Run forever
        while (true)
        {

            //O(n) run through each body and update it according to the last compute shader run
            int numBodies = bodies.Length;
            for (int i = 0; i < numBodies; i++)
            {
                if (!float.IsNaN(bodies[i].dx) && !float.IsNaN(bodies[i].dy))
                {
                    //Update acceleration of gravity
                    if (DoStressColors)
                    {
                        pixels[i].GetComponent<SpriteRenderer>().color = Color.Lerp(pixels[i].GetComponent<SpriteRenderer>().color,AccelerationColoring.Evaluate(new Vector2(bodies[i].dx, bodies[i].dy).sqrMagnitude / MaxStress),0.1f);
                    }
                    pixels[i].GetComponent<Rigidbody2D>().velocity += bodies[i].acceleration();
                }
                else
                {
                    Debug.Log("NAN at bodies[" + i + "]");
                }
                //Reset for next pass (each of the multithreaded gravity calcs just += acceleration, so we must reset after fetching)
                bodies[i].dx=0;
                bodies[i].dy= 0;
                bodies[i].close_neighbors = 0;
                bodies[i].last_neighbor = 0;

                if (bodies[i].close_neighbors>5)
                {
                    int last_neighbor = (int)bodies[i].last_neighbor;

                    pixels[i].transform.parent=pixels[last_neighbor].transform;
                }
                else
                {
                    pixels[i].transform.parent = null;
                }

                //Update position for next pass
                bodies[i].x = pixels[i].transform.position.x;
                bodies[i].y = pixels[i].transform.position.y;



            }


            //O(1) Update compute shader internal values (These can change freely, and cost little to just resend so we dont mind this)
            _compute.SetFloat("g", g);
            _compute.SetFloat("distance_scale", distance_scale);
            _compute.SetFloat("drift_power", drift_power);
            _compute.SetFloat("min_dist", min_dist);
            _compute.SetVector("drift", offset_drift);


            _compute.SetFloat("neighbor_cutoff", neighbor_cutoff);
            _compute.SetFloat("max_neighbors", max_neighbors);
            _compute.SetFloat("ignore_mult", ignore_mult);

            //


            //O(n) Update body buffer in the shader with new gravity body information (With new acclerations / pos etc.)
            if (pixels.Length > 0)
            {
                if (bodyBuffer != null)
                    bodyBuffer.Release();
                bodyBuffer = new ComputeBuffer(pixels.Length, sizeof(float) * NUM_FLOATS);
                bodyBuffer.SetData(bodies);

                _compute.SetBuffer(0, "bodyBuffer", bodyBuffer);
                _compute.SetInt("numBodies", bodyBuffer.count);
            }
            else
            {
                _compute.SetInt("numBodies", 0);

                if (bodyBuffer == null)
                {
                    bodyBuffer = new ComputeBuffer(1, sizeof(float) * NUM_FLOATS);
                    _compute.SetBuffer(0, "bodyBuffer", bodyBuffer);
                }

            }
            
            //O(n^2) Request the compute shader to pass through each combiation of x,y from x in [0..numBodies) and y in [0..numBodies]
            _compute.Dispatch(_compute.FindKernel("GravitySimulation"), numBodies / 8, numBodies/8,1);
            //This is still blazing fast despite O(n^2) because it runs on GPU in 8x8x1 Threads

            asyncDone = false;
            AsyncGPUReadback.Request(bodyBuffer, OnCompleteReadback);
            //After the gpu is done cooking, lets ask for our body buffer (with all calculated data back)

            yield return new WaitUntil(isAsyncDone);
            //Wait until it is done^


            //Adjust the rest of reality for physics simulations if needed or wait for proper timing if too fast in simulating
            SimStep++;
            StepsPerSecond = (SimStep / (Time.timeSinceLevelLoad - timeStart));
            yield return new WaitForFixedUpdate();

            if (OverridePhysics)
            {
                Physics2D.Simulate(TimePerGravitySample);
            }

            if (StepsPerSecond > targetStepsPerSecond)
                yield return new WaitUntil(isTargetSteps);
        }
    }
    //Is our simulation per second speed acceptable (not too fast)?
    bool isTargetSteps()
    {
        return (SimStep / (Time.timeSinceLevelLoad - timeStart)) <= targetStepsPerSecond;
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
        bodies = request.GetData<LegacyGravityBody>().ToArray();


        asyncDone=true;
    }

    //This is the CPU based O(n^2) gravity implementation (very slow for large sims but more stable)
    public IEnumerator StaticRun()
    {
        //Run forever
        while(true)
        {
            int delayTime = 0;
            //O(n^2) run for each particle combination a gravity simulation, slightly optimized to run both sides at once
            for(int i = 0; i < pixels.Length; i++)
            {
                //Static simulation does not simulate the offset drift features at this time.


                if (pixels[i] != null)
                {
                    for (int j = i + 1; j < pixels.Length; j++)
                    {

                        if (pixels[j] != null)
                        {

                            //Run the gravity simulation:

                            float dist = Vector2.Distance(pixels[i].transform.position, pixels[j].transform.position);
                            Vector2 dir = (pixels[i].transform.position - pixels[j].transform.position).normalized;


                            dist = Mathf.Max(min_dist, dist / distance_scale);

                            pixels[i].GetComponent<Rigidbody2D>().velocity += -dir * ((pixels[j].GetComponent<Rigidbody2D>().mass * g) / (dist * dist));
                            pixels[j].GetComponent<Rigidbody2D>().velocity += dir * ((pixels[i].GetComponent<Rigidbody2D>().mass * g) / (dist * dist));

                            //Keep track of how much work we have done and rest accordingly 
                            delayTime++;
                            if (delayTime % StaticDelayTime == 0)
                            {
                                yield return new WaitForEndOfFrame();
                            }
                        }
                    }
                }
            }

            //Adjust simulation/physics steps and wait if going to fast
            SimStep++;
            StepsPerSecond = (SimStep / (Time.timeSinceLevelLoad - timeStart));
            yield return new WaitForFixedUpdate();

            if (OverridePhysics)
            {
                Physics2D.Simulate(TimePerGravitySample);
            }
            if (StepsPerSecond > targetStepsPerSecond)
                yield return new WaitUntil(isTargetSteps);
        }
    }

}
