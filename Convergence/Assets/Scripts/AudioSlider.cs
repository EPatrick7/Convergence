using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioSlider : MonoBehaviour
{
    public enum AudioSliderType {None,Music,SFX};
    UnityEngine.UI.Slider slider;
    public AudioSliderType type;
    private bool init;
    void Start()
    {
        init = true;
        slider = GetComponent<UnityEngine.UI.Slider>();

        if(type == AudioSliderType.Music)
		{
            slider.value = PlayerPrefs.GetFloat("Volume_Music", slider.value);
        } else if (type == AudioSliderType.SFX)
		{
            slider.value = PlayerPrefs.GetFloat("Volume_SFX", slider.value);
        }
        UpdateValue();
        init = false;
    }

    public void UpdateValue()
    {
        if (type == AudioSliderType.Music)
		{
            PlayerPrefs.SetFloat("Volume_Music", slider.value);
            AudioManager.Instance?.AdjustVolume();
        } else if (type == AudioSliderType.SFX)
		{
            PlayerPrefs.SetFloat("Volume_SFX", slider.value);
            AudioManager.Instance?.AdjustVolume();
            if (!init)
            {
                AudioManager.Instance?.HoverClick();
            }
        }

    }

    /*
    public void ShareValue()
    {
        if(type==AudioSliderType.Music)
        {
            AudioManager.MusicVolume = slider.value;
            AudioManager.Instance.AdjustVolume();
        }
        else if (type == AudioSliderType.SFX)
        {
            AudioManager.SFXVolume = slider.value;
        }
    }
    */
}
