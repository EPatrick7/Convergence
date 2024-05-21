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
    private List<Sprite> stageIcons = new List<Sprite>();

    [SerializeField]
    private Image massIcon;

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
        massIcon.sprite = stageIcons[0]; //sun sprite
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
        UpdateIcon(value);
    }

    private void UpdateIcon(float value)
	{
        if (player.planetType != PixelManager.PlanetType.Sun && value < 750)
		{
            massIcon.sprite = stageIcons[0];
		} else if (player.planetType == PixelManager.PlanetType.Sun && value < 5000)
		{
            massIcon.sprite = stageIcons[1];
		} else if (player.planetType == PixelManager.PlanetType.Sun && value > 5000 && value < 7500)
		{
            massIcon.sprite = stageIcons[2];
		} else if (player.planetType == PixelManager.PlanetType.BlackHole && value > 9999)
		{
            massIcon.sprite = stageIcons[3];
		}
	}

    private float CalcMax()
	{
        if (player.planetType != PixelManager.PlanetType.Sun && player.mass() < 750)
        {
            return 750;
        }
        else if (player.planetType == PixelManager.PlanetType.Sun && player.mass() < 5000)
        {
            return 5000;
        }
        else if (player.planetType == PixelManager.PlanetType.Sun && player.mass() > 5000 && player.mass() < 7500)
        {
            return 7500;
        }
        else if (player.planetType == PixelManager.PlanetType.BlackHole && player.mass() > 9999)
        {
            return 10000;
        } else
		{
            return 10000;
		}
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
        cap = Mathf.Max(value, CalcMax(), 1f);
        slider.value = value / CalcMax();
    }

    private void UpdateText(TMP_Text text, float value, float cap)
    {
        // If cap is not valid, use the previous cap string instead
        if (player.mass() < 10000)
        {
            string cap_string = CalcMax() >= 0f ? CalcMax().ToString("0") : text.text.Split("/")[1];
            value = Mathf.Min(value, Int32.Parse(cap_string));

            text.text = string.Format("{0}/{1}", value.ToString("0"), cap_string);
        } else
		{
            text.text = "CONVERGE";
		}
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
