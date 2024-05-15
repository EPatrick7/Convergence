using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunRotate : MonoBehaviour
{

    private Transform sun;

    [SerializeField]
    private float angleIncrement = 0;

    private float angle;

    // Start is called before the first frame update
    void Start()
    {
        sun = gameObject.transform;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        sun.eulerAngles = new Vector3(sun.eulerAngles.x, sun.eulerAngles.y, sun.eulerAngles.z + angleIncrement);
        angle += angleIncrement;
        
    }
}
