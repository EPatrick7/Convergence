using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SetVolume : MonoBehaviour
{
	[SerializeField]
	private AudioMixer mixer;

	[SerializeField]
	private bool Music;

	private Slider slider;

	void Start()
	{
		slider = gameObject.GetComponent<Slider>();
		LoadVolume();
		SetLevel(slider.value);
	}

	public void SaveVolume()
	{
		float val = gameObject.GetComponent<Slider>().value;
		string key;
		if (Music)
		{
			key = "MusicVol";
		} else
		{
			key = "SFXVol";
		}
		PlayerPrefs.SetFloat(key, val);
		LoadVolume();
		
	}

	void LoadVolume()
	{
		string key;
		if (Music)
		{
			key = "MusicVol";
		} else
		{
			key = "SFXVol";
		}
		float val = PlayerPrefs.GetFloat(key);
		slider.value = val;

	}

    public void SetLevel(float sliderVal)
	{
        if (Music)
		{
			mixer.SetFloat("MusicVol", Mathf.Log10(sliderVal) * 20);
			//Debug.Log("SFX");
		} else
		{
			mixer.SetFloat("SFXVol", Mathf.Log10(sliderVal) * 20);
			//Debug.Log("Music");
		}
		SaveVolume();
	}
}
