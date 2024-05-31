using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GravityManager;

public class TutorialScene : MonoBehaviour
{
    bool hasLoaded;
    public GameObject[] FreeOnLoad;
    public bool Cleared;
    public PixelManager ListenedObject;
    public float Requirement = -1;
    public GameObject RelativeToPos;
    public enum ClearType {ConsumedAll,MassMin,KillCount,Bonk};
    public ClearType clearType;
    public TutorialScene LoadOnClear;
    public enum LoafLink {None,MassIntro,IceIntro,GasIntro,TutorialEnd};

    public LoafLink loafLink;

    public float GiftGasonLoad;
    public float GiftIceonLoad;
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
            else if(clearType==ClearType.Bonk&&TutorialManager.instance.hasBonked)
            {
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
            if(loafLink==LoafLink.TutorialEnd)
            {
                TutorialManager.instance.LoadOutTutorial();
            }
        }
    }
    public IEnumerator DelayLoafLoad()
    {
        yield return new WaitForFixedUpdate();
        switch (loafLink)
        {
            case LoafLink.MassIntro:
                TutorialManager.instance?.TriggerLoaf(TutorialManager.instance?.Loaf_MassIntro, this);
                break;
            case LoafLink.GasIntro:
                TutorialManager.instance?.TriggerLoaf(TutorialManager.instance?.Loaf_GasIntro, this);
                break;
            case LoafLink.IceIntro:
                TutorialManager.instance?.TriggerLoaf(TutorialManager.instance?.Loaf_IceIntro, this);
                break;
            case LoafLink.TutorialEnd:
                GravityManager.Instance.is_tutorial_ending = true;
                //ListenedObject.rigidBody.gravityScale = 1f;
                break;
            case LoafLink.None:
                break;
        }
    }
    private void OnEnable()
    {
        if (!hasLoaded)
        {
            StartCoroutine(DelayLoafLoad());
        }

        if (ListenedObject!= null)
        {
            ListenedObject.Gas += GiftGasonLoad;
            ListenedObject.Ice += GiftIceonLoad;

            if (ListenedObject.rigidBody != null)
            {
                ListenedObject.CheckTransitions();
                GravityManager.Instance.UpdateTexture(ListenedObject);
            }
        }
        if (Time.timeSinceLevelLoad<0.1f||TutorialManager.instance==null)
        {
            hasLoaded = true;

            foreach (GameObject freeG in FreeOnLoad)
            {
                freeG.transform.parent = transform.parent;
            }
        }
        if (!hasLoaded)
        {
            GravityManager.Instance.PreRegister_Block(this.transform);
            hasLoaded = true;
            foreach(GameObject freeG in FreeOnLoad)
            {
                freeG.transform.parent = transform.parent;
            }

        }
    }
}
