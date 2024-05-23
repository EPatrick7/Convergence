using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;
    private void Start()
    {
        instance = this;
    }

    public void LoadedTutorial(int id)
    {

    }

}
