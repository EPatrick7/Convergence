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

    public IndicatorManager indicatorManager;
    private bool indicating = false;
    public bool spawnBhole = false;

    /*
    void Start()
	{
        if (mass() >= BlackHoleTransition_MassReq &&)
		{
            spawnBhole = true;
		}
	}
    */

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
                if(GetComponent<PlayerPixelManager>() == null)  
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

        if (!spawnBhole)
        {
            if (mass() > SunTransition_MassReq && !indicating)
            {
                if (gameObject != null && indicatorManager != null)
                {
                    indicatorManager.AddTargetIndicator(gameObject, indicatorManager.sunTriggerDist, indicatorManager.sunColor);
                    indicating = true;
                    //Debug.Log(indicating);
                }
                //Debug.Log(indicating);
            }
            else if (mass() < SunTransition_MassReq && indicating)
            {
                if (gameObject != null && indicatorManager != null)
                {
                    indicatorManager.RemoveTargetIndicator(gameObject);
                    indicating = false;
                }
            }
            if (indicating&& gameObject != null && indicatorManager != null)
            {
                if (mass() > BlackHoleTransition_MassReq)
                {

                        indicatorManager.UpdateTargetIndicatorColor(gameObject, indicatorManager.npcbholeColor);
                    
                }
                else if (mass() > 5000)
                {
                        indicatorManager.UpdateTargetIndicatorColor(gameObject, indicatorManager.bluesunColor);
                        // indicatorManager.RemoveTargetIndicator(gameObject);
                        //indicatorManager.AddTargetIndicator(gameObject, indicatorManager.sunTriggerDist, indicatorManager.bsunColor);
                    
                }
                else if (mass() > SunTransition_MassReq)
                {
                        indicatorManager.UpdateTargetIndicatorColor(gameObject, indicatorManager.sunColor);
                }
            }
        }

        if(GetComponent<PlayerPixelManager>()!=null)
        {
            GetComponent<PlayerPixelManager>().Ambient();
        }
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
            GetComponent<Rigidbody2D>().mass += damage;
        other.GetComponent<Rigidbody2D>().mass -=damage;
        if (GetComponent<PlayerPixelManager>() != null)
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
                other.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.None;
                GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezePosition;
                GetComponent<Rigidbody2D>().mass += 10000;
                FindObjectOfType<GravityManager>().drift_power = 100;
                FindObjectOfType<GravityManager>().DoParticleRespawn = false;
                BlackHoleState.RadiusScalar *= 1.3f;
                CutsceneManager.Instance.BlackHoleConsumed();
            }
        }
        if (other.mass()-other.MassOverride <= 1)
        {
            //Floating point artithmetic means we loose some net mass overall here :(
            if(!ConstantMass)
                GetComponent<Rigidbody2D>().mass += other.mass();
            other.isKilled = true;

            if(other.GetComponent<PlayerPixelManager>()!=null)
            {
                other.GetComponent<PlayerPixelManager>().RunDeath();
                CutsceneManager.Instance.PlayerConsumed();
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
        return GetComponent<Rigidbody2D>().mass+ MassOverride;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isShielding)
        {
            PixelManager other = collision.gameObject.GetComponent<PixelManager>();
            if (other != null && !other.isKilled && !isKilled && GetComponent<Rigidbody2D>() != null)
            {

                if(GetComponent<PlayerPixelManager>()!=null)
                {
                    GetComponent<PlayerPixelManager>().Bonk(other.mass() > mass(),other.mass()< mass()/20f);
                }

                if ((other.mass() <= mass() && !((other.ConstantMass&&GetComponent<PlayerPixelManager>()==null)|| (other.planetType == PlanetType.BlackHole && planetType != PlanetType.BlackHole)))|| (other.planetType != PlanetType.BlackHole && planetType == PlanetType.BlackHole)||(ConstantMass&&other.GetComponent<PlayerPixelManager>()==null))
                {
                    Vector2 sticky_force = ((collision.transform.position - transform.position).normalized * StickyFactor);
                    collision.gameObject.GetComponent<Rigidbody2D>().velocity -= sticky_force;

                    //   Vector2 sticky_force = ((collision.transform.position - transform.position).normalized * other.StickyFactor);
                    // GetComponent<Rigidbody2D>().velocity += sticky_force;

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
        if (other != null && !other.isKilled && !isKilled && GetComponent<Rigidbody2D>() != null)
        {



            if (other.mass() <= mass())
            {
                Vector2 sticky_force= ((collision.transform.position - transform.position).normalized * StickyFactor);
                collision.rigidbody.velocity -= sticky_force;

             //   Vector2 sticky_force = ((collision.transform.position - transform.position).normalized * other.StickyFactor);
               // GetComponent<Rigidbody2D>().velocity += sticky_force;

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
