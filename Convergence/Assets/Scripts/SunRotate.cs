using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunRotate : MonoBehaviour
{
    
    [SerializeField]
    private float angleIncrement = .02f;

    private float angle;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Transform pos = gameObject.transform;
        pos.eulerAngles = new Vector3(pos.eulerAngles.x, pos.eulerAngles.y, pos.eulerAngles.z + angleIncrement);
        angle += angleIncrement;
    }
}
