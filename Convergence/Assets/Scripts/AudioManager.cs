using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class AudioManager : MonoBehaviour
{

    [SerializeField]
    private AudioSource audioSFX;

    [SerializeField]
    private AudioSource audioMusic;

    [SerializeField]
    private float fadeINTime, fadeOUTTime, maxVol;

    [SerializeField]
    private List<AudioClip> sfx = new List<AudioClip>();

    [SerializeField]
    private List<AudioClip> music = new List<AudioClip>();

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
        audioMusic.clip = music[(int)gameMode];
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

    public void FadeOutSFX()
	{
        audioSFX.DOFade(0f, fadeOUTTime);
	}

    public void FadeOutMusic()
	{
        audioMusic.DOFade(0f, fadeOUTTime);
	}

    public void FadeInMusic()
	{
        audioMusic.Play();
        audioMusic.DOFade(maxVol, fadeINTime);
	}

    public void MenuSelect()
	{
        audioSFX.Play();
        audioMusic.DOFade(0f, fadeOUTTime);
	}

    public void SoloSelect()
	{
        MenuSelect();
        gameMode = Mode.Solo;
	}
}
