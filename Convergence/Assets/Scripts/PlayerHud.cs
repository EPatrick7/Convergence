using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHud : MonoBehaviour
{
    private GravityManager gravityManager;

    private PlayerPixelManager player;

    public float sliderTimeScale = 0.1f;

    [SerializeField]
    private Slider terraSlider;

    [SerializeField]
    private Slider iceSlider;

    [SerializeField]
    private Slider gasSlider;
    
    void Start()
    {
        gravityManager = FindObjectOfType<GravityManager>();
        
        if (gravityManager != null)
        {
            gravityManager.Initialized += Initialize;
        }
    }

    private void Initialize()
    {
        player = FindObjectOfType<PlayerPixelManager>();

        if (player != null)
        {
            player.ElementsChanged += UpdateSliders;
            player.Destroyed += Destroyed;

            UpdateSliders(player.Terra, player.Ice, player.Gas);
        }
    }

    private void UpdateSliders(float terra, float ice, float gas)
    {
        float total = terra + ice + gas;

        if (total <= 0)
        {
            terraSlider.gameObject.SetActive(false);
            iceSlider.gameObject.SetActive(false);
            gasSlider.gameObject.SetActive(false);
        }
        else
        {
            terraSlider.value = terra / total;
            iceSlider.value = ice / total;
            gasSlider.value = gas / total;
        }
    }

    private void Destroyed()
    {
        UpdateSliders(0, 0, 0);
    }

    private void OnDestroy()
    {
        if (gravityManager != null)
        {
            gravityManager.Initialized -= Initialize;
        }

        if (player != null)
        {
            player.ElementsChanged -= UpdateSliders;
            player.Destroyed -= Destroyed;
        }


    }
}
