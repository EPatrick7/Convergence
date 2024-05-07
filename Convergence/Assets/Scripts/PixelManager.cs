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

            ElementChanged?.Invoke(ElementType.Ice, Ice, 1000f);
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

            ElementChanged?.Invoke(ElementType.Gas, Gas, 1000f);
        }
    }

    bool isKilled;

    public event Action<float, float> MassChanged;
    public event Action<ElementType, float, float> ElementChanged;

    public event Action Destroyed;
    public enum ElementType {Ice,Gas };

    public enum PlanetType {Planet,Sun,BlackHole };
    public PlanetType planetType = PlanetType.Planet;


    [HideInInspector]
    public bool isShielding = false;

   
    //Check if a body should transition between Planet Types.
    public void CheckTransitions()
    {
        if (planetType == PlanetType.Planet)
        {
            if (mass() > 750 && Gas >= 1000)
            {
                planetType = PlanetType.Sun;
            }
        }
        else if (planetType == PlanetType.Sun)
        {
            if (mass() > 7500)
            {
                planetType = PlanetType.BlackHole;
            }
            else if(mass() < 700)
            {
                planetType = PlanetType.Planet;
            }
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
        if (other.mass()-other.MassOverride <= 1)
        {
            //Floating point artithmetic means we loose some net mass overall here :(
            if(!ConstantMass)
                GetComponent<Rigidbody2D>().mass += other.mass();
            other.isKilled = true;
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
        float modifier = 0;
        if (mass / MassScale > 1)
        {
            modifier = Mathf.Round(mass / MassScale);
            mass %= MassScale;
        }
        return (modifier * Radius.Evaluate(1) * RadiusScalar + Radius.Evaluate((mass) / MassScale) * RadiusScalar + RadiusOffset);
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



                if ((other.mass() <= mass() && !(other.planetType == PlanetType.BlackHole && planetType != PlanetType.BlackHole))|| (other.planetType != PlanetType.BlackHole && planetType == PlanetType.BlackHole))
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
