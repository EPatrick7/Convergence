using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialScene : MonoBehaviour
{
    public int TutorialID;
    bool hasLoaded;
    public GameObject[] FreeOnLoad;
    public bool Cleared;
    public PixelManager ListenedObject;
    public float Requirement = -1;
    public GameObject RelativeToPos;
    public enum ClearType {ConsumedAll,MassMin,KillCount};
    public ClearType clearType;
    public TutorialScene LoadOnClear;
    private void OnDrawGizmosSelected()
    {
        if(Application.isEditor&&!Application.isPlaying&& RelativeToPos!=null)
        {
            transform.position = RelativeToPos.transform.position;
        }
    }
    private void FixedUpdate()
    {
        if (!Cleared)
        {
            if (transform.childCount == 0)
            {
                Clear();
            }
            if (clearType == ClearType.MassMin)
            {
                if (ListenedObject != null && ListenedObject.mass() > Requirement)
                {
                    Clear();
                }
            }
            else if(clearType== ClearType.KillCount&&transform.childCount<=Requirement) {
                Clear();
            }
        }
    }
    public void Clear()
    {
        Cleared = true;
        if (LoadOnClear != null)
        {
            LoadOnClear.gameObject.SetActive(true);
            if (LoadOnClear.RelativeToPos != null)
            {
                LoadOnClear.transform.position = LoadOnClear.RelativeToPos.transform.position;
            }
        }
    }
    private void OnEnable()
    {
        if(Time.timeSinceLevelLoad<0.1f||TutorialManager.instance==null)
        {
            hasLoaded = true;

            foreach (GameObject freeG in FreeOnLoad)
            {
                freeG.transform.parent = transform.parent;
            }
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
