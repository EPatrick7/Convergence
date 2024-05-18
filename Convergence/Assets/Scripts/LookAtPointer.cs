using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtPointer : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 mPos = Input.mousePosition;
        //mPos.z = 5.23f;

        Vector3 objPos = Camera.main.WorldToScreenPoint(transform.position);
        mPos.x -= objPos.x;
        mPos.y -= objPos.y;

        float angle = Mathf.Atan2(mPos.y, mPos.x) * Mathf.Rad2Deg;
        angle -= 90;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }
}
