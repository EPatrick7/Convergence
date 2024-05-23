using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunParallax : MonoBehaviour
{
    [SerializeField]
    private float pFactor = 3f;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //gameObject.transform.position = new Vector3((Camera.main.transform.position.x) - (Camera.main.transform.position.x / Camera.main.orthographicSize) * pFactor, Camera.main.transform.position.y - (Camera.main.transform.position.y / Camera.main.orthographicSize) * pFactor, 1);
    }
}
