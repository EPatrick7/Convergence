using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelManager : MonoBehaviour
{
    [Tooltip("The size of this pixel = mass/density")]
    public float Density=1;
    [Tooltip("How fast this pixel will absorb the mass from other pixels it touches."),Range(0,1)]
    public float AbsorptionSpeed=0.25f;
    bool isKilled;
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
    public float mass()
    {
        return GetComponent<Rigidbody2D>().mass;
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        PixelManager other = collision.gameObject.GetComponent<PixelManager>();
        if (other != null && !other.isKilled && !isKilled && GetComponent<Rigidbody2D>() != null)
        {
            if(other.mass()<=mass())
                StealMass(other, AbsorptionSpeed);
        }
    }
}
