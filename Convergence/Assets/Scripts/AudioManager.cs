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

    [SerializeField]
    private List<Button> buttons = new List<Button>();

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
        sfxSource.Stop();
        FadeInSFX();
        FadeInMusic();
        
        /*
        buttons.Clear();
        if (scene.name == "Main Menu")
		{
            buttons.Add(GameObject.Find("startButton").GetComponent<Button>());
            buttons.Add(GameObject.Find("multiButton").GetComponent<Button>());
            buttons.Add(GameObject.Find("tutorialButton").GetComponent<Button>());
            buttons[0].onClick.AddListener(delegate { SoloSelect(); });
            buttons[1].onClick.AddListener(delegate { MultiSelect(); });
            buttons[2].onClick.AddListener(delegate { TutorialSelect(); });
		}
        */
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
        FadeOutSFX();
	}

    public void MainSelect()
	{
        MenuSelect();
        gameMode = Mode.Menu;
	}

    public void SoloSelect()
	{
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
}
