using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{

    [SerializeField]
    private AudioSource sfxSource, musicSource;

    [SerializeField]
    private AudioMixer musicMixer, sfxMixer;

    [SerializeField]
    private float fadeINTime, fadeOUTTime,fadeCHANGETime, maxVol;

    [SerializeField]
    private List<AudioClip> sfx = new List<AudioClip>();

    [SerializeField]
    private List<AudioClip> music = new List<AudioClip>();

    public static float MusicVolume;
    public static float SFXVolume;

    private enum Mode
	{
        Menu,
        Solo,
        Multiplayer,
        Tutorial
	}
    private Mode gameMode;

    private static AudioManager instance = null;

    public static AudioManager Instance { get { return instance; } }

    void Awake()
    {
        AdjustVolume();
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        else
        {
            instance = this;
        }
        DontDestroyOnLoad(this.gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        musicSource.clip = music[(int)gameMode];
        sfxSource.Stop(); //stop transition SFX
        FadeInSFX();
        FadeInMusic();
        
        /*
        switch (gameMode)
		{
            case Mode.Solo:
                audioSFX.clip = music[(int)Mode.Solo];
                break;
		}
        */
    }

    private float ConvertToMixer(float input)
	{
        return Mathf.Log10(input) * 20;
	}

    public void AdjustVolume()
    {
        SFXVolume = PlayerPrefs.GetFloat("Volume_SFX", 1f);
        MusicVolume = PlayerPrefs.GetFloat("Volume_Music", 1f);
        sfxMixer.SetFloat("SFXVol", Mathf.Log10(SFXVolume) * 20);
        musicMixer.SetFloat("MusicVol", Mathf.Log10(MusicVolume) * 20);
    }
	
    public void FadeOutSFX()
	{
        sfxMixer.DOSetFloat("SFXVol", ConvertToMixer(0.001f), fadeOUTTime);
	}

    public void FadeInSFX()
	{
        sfxMixer.DOSetFloat("SFXVol", ConvertToMixer(SFXVolume), fadeOUTTime);
    }

    public void FadeOutMusic()
	{
        musicMixer.DOSetFloat("MusicVol", ConvertToMixer(0.001f), fadeOUTTime);
	}

    public void FadeInMusic()
	{
        musicMixer.DOSetFloat("MusicVol", ConvertToMixer(MusicVolume), fadeOUTTime);
        musicSource.Play();
        //audioMusic.Play();
        //audioMusic.DOFade(maxVol* MusicVolume, fadeINTime);
	}

    public void MenuSelect()
	{
        sfxSource.clip = sfx[0];
        sfxSource.Play();
        FadeOutMusic();
	}

    public void GeneralSelect()
	{
        sfxSource.PlayOneShot(sfx[2]);
	}

    public void BackSelect()
	{
        sfxSource.PlayOneShot(sfx[4]);
	}

    public void HoverClick()
	{
        sfxSource.PlayOneShot(sfx[3]);
	}

    public void MainSelect()
	{
        MenuSelect();
        gameMode = Mode.Menu;
	}

    public void SoloSelect()
	{
        StartCoroutine(FirstPopWait());
        sfxSource.PlayOneShot(sfx[5]);
        //StartCoroutine(SecondPopWait());
        MenuSelect();
        gameMode = Mode.Solo;
	}

    public void MultiSelect()
	{
        MenuSelect();
        gameMode = Mode.Multiplayer;
	}

    public void TutorialSelect()
	{
        MenuSelect();
        gameMode = Mode.Tutorial;
	}

    IEnumerator FirstPopWait()
    {
        yield return new WaitForSeconds(2f);
        if (SceneManager.GetActiveScene().name != "Main Menu")
		{
            yield break;
		}
        sfxSource.PlayOneShot(sfx[1]);
    }
}
