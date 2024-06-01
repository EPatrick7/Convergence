using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GravityManager;

public class TutorialScene : MonoBehaviour
{
    private void OnEnable()
    {
        if(Time.timeSinceLevelLoad>0.1f)
        {
            GravityManager.Instance.PreRegister_Block(transform);
        }
    }
}
