using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialScene : MonoBehaviour
{
    public int TutorialID;
    bool hasLoaded;
    public GameObject[] FreeOnLoad;
    public bool Cleared;
    private void FixedUpdate()
    {
        Cleared = transform.childCount == 0;
    }
    private void OnEnable()
    {
        if(Time.timeSinceLevelLoad<0.1f||TutorialManager.instance==null)
        {
            hasLoaded = true;
        }
        if (!hasLoaded)
        {
            TutorialManager.instance.LoadedTutorial(TutorialID);
            GravityManager.Instance.PreRegister_Block(this.transform);
            hasLoaded = true;
            foreach(GameObject freeG in FreeOnLoad)
            {
                freeG.transform.parent = transform.parent;
            }

        }
    }
}
