using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHud : MonoBehaviour
{
    public static List<PlayerHud> huds;
    private GravityManager gravityManager;

    private PlayerPixelManager player;

    [SerializeField]
    private Slider massSlider;

    [SerializeField]
    private Slider iceSlider;

    [SerializeField]
    private Slider gasSlider;

    [SerializeField]
    private TMP_Text massText;

    [SerializeField]
    private TMP_Text iceText;

    [SerializeField]
    private TMP_Text gasText;

    [HideInInspector]
    public bool Initialized;

    [Range(1,4)]
    public int PlayerID = 1;

    void Awake()
    {
        gravityManager = FindObjectOfType<GravityManager>();
        if(huds==null)
        {
            huds= new List<PlayerHud>();
        }
        huds.Add(this); 
        /*if (gravityManager != null)
        {
            gravityManager.Initialized += Initialize;
        }*/
    }

    public void Initialize(PlayerPixelManager playerPixelManager)
    {
        Initialized = true;
        player = playerPixelManager;

            // FindObjectOfType<PlayerPixelManager>();

        if (player != null)
        {
            player.MassChanged += UpdateMass;
            player.ElementChanged += UpdateElement;
            player.Destroyed += Destroyed;

            UpdateMass(player.mass(), 10000f);
            UpdateElement(PlayerPixelManager.ElementType.Ice, player.Ice, 1000f);
            UpdateElement(PlayerPixelManager.ElementType.Gas, player.Gas, 1000f);
        }
    }

    private void UpdateMass(float value, float cap = -1f)
    {
        UpdateSlider(massSlider, value, cap);
        UpdateText(massText, value, cap);
    }

    private void UpdateElement(PixelManager.ElementType type, float value, float cap = -1f)
    {
        switch (type)
        {
            case PlayerPixelManager.ElementType.Ice:
                UpdateSlider(iceSlider, value, cap);
                UpdateText(iceText, value, cap);
                break;
            case PlayerPixelManager.ElementType.Gas:
                UpdateSlider(gasSlider, value, cap);
                UpdateText(gasText, value, cap);
                break;
        }
    }

    private void UpdateSlider(Slider slider, float value, float cap)
    {
        // TODO: Use a tween to make the change smoother
        cap = Mathf.Max(value, cap, 1f);
        slider.value = value / cap;
    }

    private void UpdateText(TMP_Text text, float value, float cap)
    {
        // If cap is not valid, use the previous cap string instead
        string cap_string = cap >= 0f ? cap.ToString("0") : text.text.Split("/")[1];
        value = Mathf.Min(value, Int32.Parse(cap_string));

        text.text = string.Format("{0}/{1}", value.ToString("0"), cap_string);
    }

    private void Destroyed()
    {
        UpdateMass(0f);
        UpdateElement(PlayerPixelManager.ElementType.Ice, 0f);
        UpdateElement(PlayerPixelManager.ElementType.Gas, 0f);
    }

    private void OnDestroy()
    {
        /*
        if (gravityManager != null)
        {
            gravityManager.Initialized -= Initialize;
        }
        */
        if (huds == null)
        {
            huds = new List<PlayerHud>();
        }
        huds.Remove(this);

        if (player != null)
        {
            player.MassChanged -= UpdateMass;
            player.ElementChanged -= UpdateElement;
            player.Destroyed -= Destroyed;
        }
    }
}
