using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelManager : MonoBehaviour
{
    [Tooltip("The size of this pixel = mass/density")]
    public float Density=1;
    [Tooltip("How fast this pixel will absorb the mass from other pixels it touches."),Range(0,1)]
    public float AbsorptionSpeed=0.25f;


    [Tooltip("How strong the force of attraction is when two pixels collide.")]
    public float StickyFactor=0.5f;


    [Tooltip("How much terra element this pixel has"), Min(0)]
    public float Terra;
    [Tooltip("How much ice element this pixel has"), Min(0)]
    public float Ice;
    [Tooltip("How much gas element this pixel has"), Min(0)]
    public float Gas;

    bool isKilled;

    public enum ElementType {Terra,Ice,Gas };
    //Steals elements from other
    public void StealElement(PixelManager other,float percentage,ElementType target)
    {
        if (target == ElementType.Terra)
        {
            float damage = other.Terra * Mathf.Clamp(percentage, 0, 1);

            damage = Mathf.Round(damage * 64) / 64f;

            Terra += damage;
            other.Terra -= damage;
        }
        else if (target == ElementType.Ice)
        {
            float damage = other.Ice * Mathf.Clamp(percentage, 0, 1);

            damage = Mathf.Round(damage * 64) / 64f;

            Ice += damage;
            other.Ice -= damage;
        }
        else if (target == ElementType.Gas)
        {
            float damage = other.Gas * Mathf.Clamp(percentage, 0, 1);

            damage = Mathf.Round(damage * 64) / 64f;

            Gas += damage;
            other.Gas -= damage;
        }
    }

    //Transfers mass from other and then kills other pixel if its mass drops <=0
    public void StealMass(PixelManager other,float percentage)
    {
        float damage = other.mass() * Mathf.Clamp(percentage,0,1);
        
        damage = Mathf.Round(damage * 64) / 64f;
        
        GetComponent<Rigidbody2D>().mass += damage;
        other.GetComponent<Rigidbody2D>().mass -=damage;
        if (other.mass() <=0.1)
        {
            //Floating point artithmetic means we loose some net mass overall here :(
            GetComponent<Rigidbody2D>().mass += other.mass();
            other.isKilled = true;
            Destroy(other.gameObject);
        }
    }

    public Vector3 elements()
    {
        return new Vector3(Terra, Ice, Gas);
    }
    public float mass()
    {
        return GetComponent<Rigidbody2D>().mass;
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
}
