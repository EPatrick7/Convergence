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
    private AudioSource sfxSource, musicSource, playerSFXSource;

    [SerializeField]
    private AudioMixer musicMixer, sfxMixer;

    [SerializeField]
    private float fadeINTime, fadeOUTTime,fadeCHANGETime, maxVol;

    [SerializeField]
    private List<AudioClip> sfx = new List<AudioClip>();

    [SerializeField]
    private List<AudioClip> music = new List<AudioClip>();

    [SerializeField]
    private List<AudioClip> playersfx = new List<AudioClip>();

    public static float MusicVolume;
    public static float SFXVolume;

    private Tween propelTween;

    private bool soloSelected;

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
        PlayerPrefs.SetInt("lastLevel", SceneManager.GetActiveScene().buildIndex);
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
        soloSelected = false;
        musicSource.clip = music[(int)gameMode];
        if (SceneManager.GetActiveScene().name != "Main Menu")
		{
            if (PlayerPrefs.GetInt("lastLevel") != scene.buildIndex)
			{
                sfxSource.Stop();
            }
		}
        PlayerPrefs.SetInt("lastLevel", SceneManager.GetActiveScene().buildIndex);
        //FadeOutSFX();
        //sfxSource.Stop();
        //FadeInSFX();
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
        sfxSource.PlayOneShot(sfx[0]);
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
        BackSelect();
        FadeOutMusic();
        gameMode = Mode.Menu;
	}

    public void SoloSelect()
	{
        soloSelected = true;
        StartCoroutine(FirstPopWait());
        sfxSource.PlayOneShot(sfx[5]);
        MenuSelect();
        gameMode = Mode.Solo;
	}

    public void MultiSelect()
	{
        soloSelected = true;
        StartCoroutine(FirstPopWait());
        sfxSource.PlayOneShot(sfx[5]);
        MenuSelect();
        gameMode = Mode.Multiplayer;
	}

    public void TutorialSelect()
	{
        MenuSelect();
        gameMode = Mode.Tutorial;
	}

    public void StartPlayerJet()
	{
        if (propelTween != null)
		{
            playerSFXSource.volume = 1;
            propelTween.Kill();
		}
        playerSFXSource.clip = playersfx[0];
        playerSFXSource.Play();
	}

    public void StopPlayerJet()
	{
     //   Debug.Log("StopPlayerJet");
        //sfxSource.PlayOneShot(playersfx[1]);
        //playerSFXSource.time = playerSFXSource.clip.length * .975f;
        propelTween?.Kill();
        propelTween = playerSFXSource.DOFade(0, .5f);
        propelTween.OnComplete(PlayerSFXSourceReset);
        propelTween.Play();

        //Debug.Log("Stopping");
    }

    private void PlayerSFXSourceReset()
	{
        playerSFXSource.Stop();
        playerSFXSource.volume = 1;
	}

    public void PlayerEject()
	{
        playerSFXSource.PlayOneShot(playersfx[2]);
	}

    public void PlayerShieldUp()
	{
        playerSFXSource.PlayOneShot(playersfx[3]);
	}

    IEnumerator FirstPopWait()
    {
        yield return new WaitForSeconds(2f);
        if (SceneManager.GetActiveScene().name != "Main Menu" || !soloSelected) //check for soloSelected to fix weird bug where FirstPopWait() was being called when switching back to main menu really quick after skipping cutscene
		{
            yield break;
		}
        sfxSource.PlayOneShot(sfx[1]);
    }
}
