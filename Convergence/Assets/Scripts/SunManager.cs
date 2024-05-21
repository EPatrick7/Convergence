using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunManager : MonoBehaviour
{

    [SerializeField]
    private float angleIncrement = 0;

    private float angle;

    private ParticleSystem ps;

    private SpriteRenderer sprite;

    // Start is called before the first frame update
    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        ps = GetComponentInChildren<ParticleSystem>();
        ps.Stop();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Transform pos = gameObject.transform;
        pos.eulerAngles = new Vector3(pos.eulerAngles.x, pos.eulerAngles.y, pos.eulerAngles.z + angleIncrement);
        angle += angleIncrement;
        
    }

    public void sceneStart()
	{
        sprite.enabled = false;
        ps.Play();
	}

}
