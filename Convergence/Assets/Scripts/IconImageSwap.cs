using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IconImageSwap : MonoBehaviour
{

    public Sprite ToSwapIfSwitch;
    public Sprite ToSwapIfXBox;
    public Sprite ToSwapIfGamePad;
    public Sprite DefaultSprite;
    [Range(1,4)]
    public int PlayerId = 1;
    private void OnDrawGizmosSelected()
    {
        if(!Application.isPlaying&&Application.isEditor)
            DefaultSprite = GetComponent<Image>().sprite;
    }
    private void Start()
    {
        if (PlayerId <= 0)
            PlayerId = 1;
    }
    private void OnEnable()
    {
        StartCoroutine(DelayedCheck());
    }
    public IEnumerator DelayedCheck()
    {
        while (true)
        {

            if (InputManager.inputManagers!=null&&InputManager.inputManagers.Count > 0&& InputManager.GetManager(PlayerId)!=null && InputManager.GetManager(PlayerId).playerInput.devices.Count > 0 && InputManager.GetManager(PlayerId).playerInput!=null&& InputManager.GetManager(PlayerId).HasGamepad())
            {

                if (InputManager.GetManager(PlayerId).HasXBox())
                    GetComponent<Image>().sprite = ToSwapIfXBox;
                else if (InputManager.GetManager(PlayerId).HasSwitch())
                    GetComponent<Image>().sprite = ToSwapIfSwitch;
                else
                    GetComponent<Image>().sprite = ToSwapIfGamePad;
            }
            else
            {
                GetComponent<Image>().sprite= DefaultSprite;
            }
            yield return new WaitForSeconds(0.25f);
        }
    }
}
