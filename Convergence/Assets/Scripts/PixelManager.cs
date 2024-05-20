using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelManager : MonoBehaviour
{
    [System.Serializable]
    public class StateTransition
    {
        [Tooltip("The size of this pixel is sample of this curve(mass/MassScale) * radiusScalar+RadiusOffset")]
        public AnimationCurve Radius;
        [Tooltip("How much the radius curve is at val=1")]
        public float RadiusScalar;
        [Tooltip("How much the radius curve is offset by (equal to this when val=0)")]
        public float RadiusOffset;
        [Tooltip("How much mass to increase the animation curve to val =1 (Real Mass / Mass Scale)")]
        public float MassScale;



    }

    public StateTransition PlanetState;
    public StateTransition SunState;
    public StateTransition BlackHoleState;


    [Tooltip("How fast this pixel will absorb the mass from other pixels it touches."),Range(0,1)]
    public float AbsorptionSpeed=0.25f;


    [Tooltip("How strong the force of attraction is when two pixels collide.")]
    public float StickyFactor=0.5f;

    [Tooltip("If the mass of the particle should not increase ever.")]
    public bool ConstantMass;
    [Tooltip("How much mass is needed to consume this object = (Real Mass + OverrideMass)")]
    public int MassOverride;


    [Tooltip("How much ice element this pixel has"), Min(0)]
    [SerializeField]
    private float ice;

    [HideInInspector]
    public Rigidbody2D rigidBody;
    [HideInInspector]
    public PlayerPixelManager playerPixel;
    [HideInInspector]
    public bool isPlayer;
    public void Initialize()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        playerPixel = GetComponent<PlayerPixelManager>();
        isPlayer = playerPixel != null;
        if (!isPlayer) indManagers = FindObjectsOfType<IndicatorManager>();
    }
    private void Start()
    {
        Initialize();
    }
    public float Ice
    {
        get { return ice; }
        set
        {
            ice = Mathf.Max(0f, value);

            if (planetType == PlanetType.Planet)
                ElementChanged?.Invoke(ElementType.Ice, Ice, 1000f);
            else
                ElementChanged?.Invoke(ElementType.Ice, Ice, 10000f);
        }
    }
    [Tooltip("How much gas element this pixel has"), Min(0)]
    [SerializeField]
    private float gas;

    public float Gas
    {
        get { return gas; }
        set
        {
            gas = Mathf.Max(0f, value);
            if(planetType==PlanetType.Planet)
                ElementChanged?.Invoke(ElementType.Gas, Gas, 1000f);
            else
                ElementChanged?.Invoke(ElementType.Gas, Gas, 10000f);
        }
    }

    bool isKilled;

    public event Action<float, float> MassChanged;
    public event Action<ElementType, float, float> ElementChanged;
    public event Action<PlanetType, PlanetType> PlanetTypeChanged;

    public event Action Destroyed;
    public enum ElementType {Ice,Gas };

    public enum PlanetType {Planet,Sun,BlackHole };
    public PlanetType planetType = PlanetType.Planet;


    [HideInInspector]
    public bool isShielding = false;



    [HideInInspector]
    public float SunTransition_MassReq=750;
    [HideInInspector]
    public float SunTransition_GasReq=1000;

    public IndicatorManager[] indManagers;
    private bool indicating = false;
    public bool spawnBhole = false;



    [HideInInspector]
    public float BlackHoleTransition_MassReq=7500;
    //Check if a body should transition between Planet Types.
    public void CheckTransitions()
    {
        PlanetType last = planetType;
        if (planetType == PlanetType.Planet)
        {
            if (mass() > SunTransition_MassReq && Gas >= SunTransition_GasReq)
            {
                planetType = PlanetType.Sun;
            }
        }
        else if (planetType == PlanetType.Sun)
        {
            if (mass() > BlackHoleTransition_MassReq)
            {
                if(isPlayer)  
                    transform.gameObject.layer = LayerMask.NameToLayer("Black Hole");

                planetType = PlanetType.BlackHole;
            }
            else if(mass() < SunTransition_MassReq*0.9f)
            {
                planetType = PlanetType.Planet;
            }
        }

        if (planetType != last)
        {
            PlanetTypeChanged?.Invoke(planetType,last);
        }

        if (!spawnBhole && !isPlayer)
        {
            if (mass() > SunTransition_MassReq && !indicating)
            {
                if (gameObject != null && indManagers.Length > 0)
                {
                    for (var i = 0; i < indManagers.Length; i++)
					{
                        indManagers[i].AddTargetIndicator(gameObject, indManagers[i].sunTriggerDist, indManagers[i].sunColor);
                    }
                    indicating = true;
                }
            }
            else if (mass() < SunTransition_MassReq && indicating)
            {
                if (gameObject != null && indManagers.Length > 0)
                {
                    for (var i = 0; i < indManagers.Length; i++)
                    {
                        indManagers[i].RemoveTargetIndicator(gameObject);
                    }
                    indicating = false;
                }
            }
            if (indicating&& gameObject != null && indManagers.Length > 0)
            {
                if (mass() > BlackHoleTransition_MassReq)
                {
                    for (var i = 0; i < indManagers.Length; i++)
					{
                        indManagers[i].UpdateTargetIndicatorColor(gameObject, indManagers[i].npcbholeColor);
                    }    
                    
                }
                else if (mass() > 5000)
                {
                    for (var i = 0; i < indManagers.Length; i++)
                    {
                        indManagers[i].UpdateTargetIndicatorColor(gameObject, indManagers[i].bluesunColor);
                    }
                }
                else if (mass() > SunTransition_MassReq)
                {
                    for (var i = 0; i < indManagers.Length; i++)
                    {
                        indManagers[i].UpdateTargetIndicatorColor(gameObject, indManagers[i].sunColor);
                    }
                }
            }
        }

        playerPixel?.Ambient();
        
    }

    //Steals elements from other
    public void StealElement(PixelManager other,float percentage,ElementType target)
    {
        if (target == ElementType.Ice)
        {
            float damage = other.Ice * Mathf.Clamp(percentage, 0, 1);

            damage = Mathf.Round(damage * 64) / 64f;

            if (!ConstantMass)
                Ice += damage;
            other.Ice -= damage;
        }
        else if (target == ElementType.Gas)
        {
            float damage = other.Gas * Mathf.Clamp(percentage, 0, 1);

            damage = Mathf.Round(damage * 64) / 64f;

            if (!ConstantMass)
                Gas += damage;
            other.Gas -= damage;
        }
    }

    //Transfers mass from other and then kills other pixel if its mass drops <=0
    public void StealMass(PixelManager other,float percentage)
    {
        float damage = Mathf.Min(other.mass(),Mathf.Max(1, other.mass() * Mathf.Clamp(percentage,0,1)));
        
        damage = Mathf.Round(damage * 64) / 64f;
        
        if(!ConstantMass)
            rigidBody.mass += damage;

        if(!isPlayer&&rigidBody.mass>GravityManager.Instance.max_npc_mass)
        {//Cap mass at NPC max.
            rigidBody.mass = GravityManager.Instance.max_npc_mass;
        }

        other.rigidBody.mass -=damage;
        if (playerPixel != null)
        {
            if (other.Ice > 0 && other.Ice > other.Gas)
            {
                CutsceneManager.Instance.ElementConsumed(ElementType.Ice);
            }
            else if (other.Gas > 0 && other.Gas > other.Ice)
            {
                    CutsceneManager.Instance.ElementConsumed(ElementType.Gas);
            }
            if (other.ConstantMass && other.planetType == PlanetType.BlackHole)
            {
                //If we just consumed the central black hole...
                ConstantMass = true;
                other.rigidBody.constraints = RigidbodyConstraints2D.None;
                rigidBody.constraints = RigidbodyConstraints2D.FreezePosition;
                rigidBody.mass += 10000;
                GravityManager.Instance.drift_power = 100;
                GravityManager.Instance.DoParticleRespawn = false;
                GravityManager.Instance.respawn_players = false;
                GravityManager.GameWinner = playerPixel;
                BlackHoleState.RadiusScalar *= 1.3f;
                CutsceneManager.Instance.BlackHoleConsumed();
            }
        }
        if (other.mass()-other.MassOverride <= 1)
        {
            //Floating point artithmetic means we loose some net mass overall here :(
            if(!ConstantMass)
                rigidBody.mass += other.mass();
            other.isKilled = true;

            if(other.isPlayer)
            {
                other.playerPixel.RunDeath();
                CutsceneManager.Instance.PlayerConsumed(playerPixel);

               // Debug.LogFormat("{0} | {1} | {2} | {3}", other.playerPixel, other.playerPixel.PlayerID, PlayerKillNotifier.GetNotifier(other.playerPixel.PlayerID), playerPixel);
                if(other.playerPixel!=null&& playerPixel!=null)
                    PlayerKillNotifier.GetNotifier(other.playerPixel.PlayerID)?.Notify(playerPixel,"Player "+playerPixel.PlayerID);
            }
            Destroy(other.gameObject);
        }

        InvokeMassChanged();
        other.InvokeMassChanged();
    }
    private void OnDrawGizmosSelected()
    {
        if(Application.isPlaying&&Application.isEditor)
        {
            transform.localScale = Vector3.one * radius();
        }
    }

    public float radius(float mass)
    {
        if (planetType == PlanetType.Planet)
        {
            return calc_radius(mass, PlanetState.Radius, PlanetState.MassScale, PlanetState.RadiusScalar, PlanetState.RadiusOffset);
        }
        else if (planetType == PlanetType.Sun)
        {
            return calc_radius(mass, SunState.Radius, SunState.MassScale, SunState.RadiusScalar, SunState.RadiusOffset);
        }
        else
            return calc_radius(mass, BlackHoleState.Radius, BlackHoleState.MassScale, BlackHoleState.RadiusScalar, BlackHoleState.RadiusOffset);
    }
    private float calc_radius(float mass,AnimationCurve Radius,float MassScale,float RadiusScalar,float RadiusOffset)
    {
        return ( Radius.Evaluate((mass) / MassScale) * RadiusScalar + RadiusOffset);
    }
    public float radius()
    {
        return radius(mass()-MassOverride);
    }
    public Vector2 elements()
    {
        return new Vector2(Ice, Gas);
    }
    public float mass()
    {
        return rigidBody.mass+ MassOverride;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isShielding)
        {
            PixelManager other = collision.gameObject.GetComponent<PixelManager>();
            if (other != null && !other.isKilled && !isKilled && rigidBody != null)
            {

                if(isPlayer)
                {
                    playerPixel.Bonk(other.mass() > mass(),other.mass()< mass()/20f,other.mass()>mass()/2f);
                }

                if ((other.mass() <= mass() && !((other.ConstantMass&&playerPixel==null)|| (other.planetType == PlanetType.BlackHole && planetType != PlanetType.BlackHole)))|| (other.planetType != PlanetType.BlackHole && planetType == PlanetType.BlackHole)||(ConstantMass&&other.playerPixel==null))
                {
                    Vector2 sticky_force = ((collision.transform.position - transform.position).normalized * StickyFactor);
                    other.rigidBody.velocity -= sticky_force;

                    //   Vector2 sticky_force = ((collision.transform.position - transform.position).normalized * other.StickyFactor);
                    // rigidBody.velocity += sticky_force;

                    StealMass(other, AbsorptionSpeed);
                    StealElement(other, AbsorptionSpeed, ElementType.Ice);
                    StealElement(other, AbsorptionSpeed, ElementType.Gas);
                }
            }
        }

    }
    /*private void OnCollisionStay2D(Collision2D collision)
    {
        PixelManager other = collision.gameObject.GetComponent<PixelManager>();
        if (other != null && !other.isKilled && !isKilled && rigidBody != null)
        {



            if (other.mass() <= mass())
            {
                Vector2 sticky_force= ((collision.transform.position - transform.position).normalized * StickyFactor);
                collision.rigidbody.velocity -= sticky_force;

             //   Vector2 sticky_force = ((collision.transform.position - transform.position).normalized * other.StickyFactor);
               // rigidBody.velocity += sticky_force;

                StealMass(other, AbsorptionSpeed);
                StealElement(other, AbsorptionSpeed, ElementType.Ice);
                StealElement(other, AbsorptionSpeed, ElementType.Gas);
            }
        }
    }*/

    protected virtual void OnDestroy()
    {
        Destroyed?.Invoke();
    }

    // TODO: Make a better solution; not as clean as invoking the action in a setter
    public void InvokeMassChanged()
    {
        MassChanged?.Invoke(mass(), 10000f);
    }
}
