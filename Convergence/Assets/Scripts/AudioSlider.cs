using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AudioSlider : MonoBehaviour
{
    public enum AudioSliderType {None,Music,SFX};  
    Slider slider;
    public AudioSliderType type;    
    void Start()
    {
        slider=gameObject.GetComponent<Slider>();

        slider.value = PlayerPrefs.GetFloat(gameObject.name, 1f);

        ShareValue();
    }
    public void UpdateValue(float val)
    {
        PlayerPrefs.SetFloat(gameObject.name, val);
        ShareValue();
    }

    public void ShareValue()
    {
        if(type==AudioSliderType.Music)
        {

        }
        else if (type == AudioSliderType.SFX)
        {

        }
    }
}
