using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.Mathematics;

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

    public Vector2 InitialVelocity;

    [HideInInspector]
    public Rigidbody2D rigidBody;
    [HideInInspector]
    public PlayerPixelManager playerPixel;
    [HideInInspector]
    public bool isPlayer;

    private PixelSpriteTransitioner spriteTransitioner;

    [HideInInspector]
    public Vector3 startPos;
    [HideInInspector]
    public bool isInitialized;
    [HideInInspector]
    public bool RunCutscene=true;
    [HideInInspector]
    public double lastTime;
    public void Initialize()
    {
        RunCutscene = true;
        isInitialized = true;
        startPos = transform.position;
        rigidBody = GetComponent<Rigidbody2D>();
        playerPixel = GetComponent<PlayerPixelManager>();
        isPlayer = playerPixel != null;
        indManagers = FindObjectsOfType<IndicatorManager>();

        spriteTransitioner = GetComponentInChildren<PixelSpriteTransitioner>();
    }
    private void Start()
    {
        Initialize();
        
        rigidBody.velocity += InitialVelocity;
        
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
    public float SunTransition_MassReq=750;
    [HideInInspector]
    public float SunTransition_GasReq=750;

    public IndicatorManager[] indManagers;
    private bool indicating = false;
    public bool spawnBhole = false;



    [HideInInspector]
    public float BlackHoleTransition_MassReq=7500;

    public float GasMultiplier()
    {
        return 1;
    }

    //Check if a body should transition between Planet Types.

    //Gets a point on the surface of the GravityBody origin that points towards target.
    Vector2 pointTowards(PixelManager targ)
    {
        float radius = this.radius()/2f;
        Vector2 origin = transform.position;
        Vector2 target=targ.transform.position;


        return new Vector2(origin.x, origin.y) + ((new Vector2(target.x, target.y) - new Vector2(origin.x, origin.y)).normalized * radius);
    }
    public bool AboutToHitMutual(PixelManager target)
    {
        return (AboutToHit(target) || target.AboutToHit(this))|| (AboutToHit(target,0.5f) || target.AboutToHit(this,0.5f));
    }
    //Returns true if with current velocities this gameobject is about to hit the target gameobject should nothing change.
    public bool AboutToHit(PixelManager target,float timeStep=1.5f)
    {
        Vector2 thisPos = pointTowards(target);
        Vector2 targPos = target.pointTowards(this);

        Vector2 targFuturePos = targPos + (target.rigidBody.velocity * timeStep);
        Vector2 futurePos = thisPos + (rigidBody.velocity * timeStep);
        float dot = (Vector2.Dot(targFuturePos - thisPos, futurePos - thisPos) / (futurePos - thisPos).sqrMagnitude);
        Vector2 projected_targPos = dot* (futurePos- thisPos);
        if(dot<0)
        {//Pointing Backwards
            projected_targPos = Vector2.zero;
        }
        projected_targPos=thisPos+ projected_targPos.normalized * Mathf.Max(0, Mathf.Min((projected_targPos).magnitude, (futurePos-thisPos).magnitude));

        //Debug.DrawLine(thisPos, futurePos,Color.blue);
       // Debug.DrawLine(thisPos, targFuturePos, Color.gray);
        //Debug.DrawLine(thisPos, projected_targPos,Color.red);

        if(dot>0&& Vector2.Distance(projected_targPos, targFuturePos) < radius()/2f)
        {


          //  Debug.DrawLine(thisPos, projected_targPos, Color.green);
            //Debug.DrawLine(projected_targPos, targFuturePos, Color.green);

            //The projection point is almost the same as the targPoint so it means that this object will hit targPos on route to futurePos
            return true;
        }
        return false;
    }

    private bool BlueSunExpand = false;
    private bool GameEndBlackHole = false;
    private float BlueSunTransition_MassReq = 5000;

    public void CheckTransitions()
    {
        PlanetType last = planetType;
        if (planetType == PlanetType.Planet)
        {
            if (mass() > SunTransition_MassReq && Gas >= SunTransition_GasReq)
            {
                planetType = PlanetType.Sun;
                if (isPlayer)
				{
                    AudioManager.Instance?.PlayerExpandSFX();
                }
            }
        }
        else if (planetType == PlanetType.Sun)
        {
            if (mass() > BlueSunTransition_MassReq && !BlueSunExpand)
			{
                if (isPlayer)
                {
                    BlueSunExpand = true;
                    AudioManager.Instance?.PlayerExpandSFX();
                }
            }
            if (mass() > BlackHoleTransition_MassReq)
            {
                //if(isPlayer)  
                //transform.gameObject.layer = LayerMask.NameToLayer("Black Hole");

                planetType = PlanetType.BlackHole;
                if (isPlayer)
                {
                    AudioManager.Instance?.PlayerExpandSFX();
                }
            }
            else if (isPlayer && mass() < (BlueSunTransition_MassReq * 0.9f) && mass() > SunTransition_MassReq*0.9f)
			{
                BlueSunExpand = false;
			}
            else if(mass() < SunTransition_MassReq*0.9f)
            {
                planetType = PlanetType.Planet;
            }
        }
        else if (planetType == PlanetType.BlackHole)
		{
            if (mass() >= 10000 && !GameEndBlackHole)
			{
                if (isPlayer)
				{
                    GameEndBlackHole = true;
                    AudioManager.Instance?.PlayerWinSFX();
                }
			} else if (mass() < 10000 && GameEndBlackHole)
			{
                GameEndBlackHole = false;
                AudioManager.Instance?.PlayerWinFailSFX();
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
                if (gameObject != null && indManagers.Length > 0)
                {
                    for (var i = 0; i < indManagers.Length; i++)
					{
                        if (isPlayer && playerPixel.PlayerID == indManagers[i].PlayerID) continue;

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
                else if (mass() > BlueSunTransition_MassReq)
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
                Gas += damage* GasMultiplier();
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

        if (playerPixel != null)
        {
            if(RunCutscene)
                CutsceneManager.Instance?.ConsumeMass(mass());
            if (other.Ice > 0 && other.Ice > other.Gas)
            {
                if (RunCutscene)
                    CutsceneManager.Instance?.ElementConsumed(ElementType.Ice);
            }
            else if (other.Gas > 0 && other.Gas > other.Ice)
            {
                if (RunCutscene)
                    CutsceneManager.Instance?.ElementConsumed(ElementType.Gas);
            }
            if (GravityManager.GameWinner==null&& other.ConstantMass && other.planetType == PlanetType.BlackHole)
            {
                playerPixel.WinGame(other);
                
                damage = 0;

            }
        }
        if (other.spawnBhole)
            damage /= 3f;
        other.rigidBody.mass -= damage;
        if (other.mass()-other.MassOverride <= 1)
        {
            //Floating point artithmetic means we loose some net mass overall here :(
            if(!ConstantMass)
                rigidBody.mass += other.mass();
            other.isKilled = true;

            if(other.isPlayer)
            {
                other.playerPixel.RunDeath();

                if (other.RunCutscene)
                    CutsceneManager.Instance?.PlayerConsumed(playerPixel);

               // Debug.LogFormat("{0} | {1} | {2} | {3}", other.playerPixel, other.playerPixel.PlayerID, PlayerKillNotifier.GetNotifier(other.playerPixel.PlayerID), playerPixel);
                if(other.playerPixel!=null&& playerPixel!=null&&GravityManager.GameWinner==null)
                    PlayerKillNotifier.GetNotifier(other.playerPixel.PlayerID)?.Notify(playerPixel,playerPixel.pInput.GetComponent<InputManager>().PlayerNames[playerPixel.PlayerID-1]+ " Player");
            }
            Destroy(other.gameObject);
            /*
            if (isPlayer && other.mass() > (mass()/10))
			{
                AudioManager.Instance?.PlayerAbsorbSFX();
            }
            */
        }

        InvokeMassChanged();
        other.InvokeMassChanged();
    }
    private void OnDrawGizmosSelected()
    {
        if(!Application.isPlaying&&Application.isEditor)
        {
            transform.localScale = Vector3.one * radius(GetComponent<Rigidbody2D>().mass-MassOverride);
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
        PixelManager other = collision.gameObject.GetComponent<PixelManager>();

        if (other != null && !other.isKilled && !isKilled && rigidBody != null)
        {
            if (!ShieldIsActive())
            {


                if (isPlayer)
                {
                    playerPixel.Bonk(other.mass() > mass(), other.mass() < mass() / 20f, other.mass() > mass() / 2f);
                }

                if ((other.mass() <= mass() && !((other.ConstantMass && playerPixel == null) || (other.planetType == PlanetType.BlackHole && planetType != PlanetType.BlackHole))) || (other.planetType != PlanetType.BlackHole && planetType == PlanetType.BlackHole) || (ConstantMass && other.playerPixel == null))
                {
                    if (!other.ShieldIsActive())
                    {
                        Vector2 sticky_force = ((collision.transform.position - transform.position).normalized * StickyFactor);
                        other.rigidBody.velocity -= sticky_force;

                        //   Vector2 sticky_force = ((collision.transform.position - transform.position).normalized * other.StickyFactor);
                        // rigidBody.velocity += sticky_force;

                        StealMass(other, AbsorptionSpeed);
                        StealElement(other, AbsorptionSpeed, ElementType.Ice);
                        StealElement(other, AbsorptionSpeed, ElementType.Gas);
                    }
                    else
                    {

                        if (mass()*0.5f > other.mass())
                        {
                            if (ConstantMass)
                            {
                                Vector2 sticky_force = ((collision.transform.position - transform.position).normalized * 10);
                                other.rigidBody.velocity += sticky_force;
                            }
                            other.playerPixel.Shield.Bonk();
                        }
                    }
                }
                
            }
            else if(playerPixel!=null)
            {
                if(GravityManager.GameWinner!=null)
                {
                    Ice /= 2f;
                }
                if(other.mass()>mass()*0.5f)
                {
                    playerPixel.Shield.Bonk();
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

    public void UpdateTexture(Sprite target,Color targColor)
    {
        spriteTransitioner?.UpdateTexture(target,targColor);
    }

    public virtual bool ShieldIsActive()
    {
        return false;
    }

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
