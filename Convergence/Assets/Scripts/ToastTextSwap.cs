using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ToastTextSwap : MonoBehaviour
{
    public string ToSwapIfGamePad;
    [HideInInspector]
    public string DefaultText;
    private void Start()
    {
        DefaultText = GetComponent<TextMeshProUGUI>().text;
    }
    private void FixedUpdate()
    {
        if (InputManager.inputManagers.Count > 0 && InputManager.inputManagers[0].playerInput.devices[0].GetType().ToString().Contains("Gamepad"))
        {
            GetComponent<TextMeshProUGUI>().text = ToSwapIfGamePad;
        }
        else
        {
            GetComponent<TextMeshProUGUI>().text = DefaultText;
        }
    }
}
