using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelManager : MonoBehaviour
{
    [Tooltip("The size of this pixel = mass/density, Density is sampled from this curve via mass")]
    public AnimationCurve Density;
    [Tooltip("How much the density curve is at val=1")]
    public float DensityScale;
    [Tooltip("How much the density curve is offset by (equal to this when val=0)")]
    public float DensityOffset;
    [Tooltip("How much mass to increase the animation curve to val =1 (Real Mass / Mass Scale)")]
    public float MassScale;
    [Tooltip("How much mass is offset before being scaled (Real Mass - MassOffset)/Mass Scale")]
    public float MassOffset;
    [Tooltip("How fast this pixel will absorb the mass from other pixels it touches."),Range(0,1)]
    public float AbsorptionSpeed=0.25f;


    [Tooltip("How strong the force of attraction is when two pixels collide.")]
    public float StickyFactor=0.5f;

    [Tooltip("If the mass of the particle should not increase ever.")]
    public bool ConstantMass;
    [Tooltip("How much mass is needed to consume this object = (Real Mass + OverrideMass)")]
    public int MassOverride;


    [Tooltip("How much terra element this pixel has"), Min(0)]
    [SerializeField]
    private float terra;


    public float Terra
    {
        get{ return terra; }
        set
        {
            terra = value;

            // TODO: Pass actual mass
            ElementChanged?.Invoke(ElementType.Terra, Terra, 1000f);
        }
    }

    [Tooltip("How much ice element this pixel has"), Min(0)]
    [SerializeField]
    private float ice;

    public float Ice
    {
        get { return ice; }
        set
        {
            ice = value;

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
            gas = value;

            ElementChanged?.Invoke(ElementType.Gas, Gas, 1000f);
        }
    }

    bool isKilled;

    public event Action<ElementType, float, float> ElementChanged;

    public event Action Destroyed;
    public enum ElementType {Terra,Ice,Gas };
    //Steals elements from other
    public void StealElement(PixelManager other,float percentage,ElementType target)
    {
        if (target == ElementType.Terra)
        {
            float damage = other.Terra * Mathf.Clamp(percentage, 0, 1);

            damage = Mathf.Round(damage * 64) / 64f;

            if (!ConstantMass)
                Terra += damage;
            other.Terra -= damage;
        }
        else if (target == ElementType.Ice)
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
        float damage = other.mass() * Mathf.Clamp(percentage,0,1);
        
        damage = Mathf.Round(damage * 64) / 64f;
        
        if(!ConstantMass)
            GetComponent<Rigidbody2D>().mass += damage;
        other.GetComponent<Rigidbody2D>().mass -=damage;
        if (other.mass() <=1)
        {
            //Floating point artithmetic means we loose some net mass overall here :(
            if(!ConstantMass)
                GetComponent<Rigidbody2D>().mass += other.mass();
            other.isKilled = true;
            Destroy(other.gameObject);
        }
    }

    public float density()
    {
        return Density.Evaluate(mass()/MassScale)* DensityScale+DensityOffset;
    }
    public Vector3 elements()
    {
        return new Vector3(Terra, Ice, Gas);
    }
    public float mass()
    {
        return GetComponent<Rigidbody2D>().mass+ MassOverride;
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        PixelManager other = collision.gameObject.GetComponent<PixelManager>();
        if (other != null && !other.isKilled && !isKilled && GetComponent<Rigidbody2D>() != null)
        {



            if (other.mass() <= mass())
            {
                collision.rigidbody.AddForce((transform.position - collision.transform.position).normalized * StickyFactor);
                GetComponent<Rigidbody2D>().AddForce((collision.transform.position - transform.position).normalized * other.StickyFactor);

                StealMass(other, AbsorptionSpeed);
                StealElement(other, AbsorptionSpeed, ElementType.Terra);
                StealElement(other, AbsorptionSpeed, ElementType.Ice);
                StealElement(other, AbsorptionSpeed, ElementType.Gas);
            }
        }
    }

    protected virtual void OnDestroy()
    {
        Destroyed?.Invoke();
    }
}
