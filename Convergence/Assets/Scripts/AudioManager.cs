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
    private AudioSource sfxSource, musicSource, playerSFXSource, absorbSFXSource, absorb2SFXSource, indicatorSFXSource, jetSFXSource;

    [SerializeField]
    private AudioMixer musicMixer, sfxMixer;

    [SerializeField]
    private float fadeINTime, fadeOUTTime, fadeCHANGETime, maxVol;

    [SerializeField]
    private List<AudioClip> sfx = new List<AudioClip>();

    [SerializeField]
    private List<AudioClip> music = new List<AudioClip>();

    [SerializeField]
    private List<AudioClip> playersfx = new List<AudioClip>();

    [SerializeField]
    private List<AudioClip> absorbsfx = new List<AudioClip>();

    public static float MusicVolume;
    public static float SFXVolume;

    private Tween propelTween, musicTween, dangerTween, sfxTween;

    private bool soloSelected, restartingMusic;
    private bool gameEnd = false;
    private bool tutorialRestart = false;
    private float transitionNum = 0;
    private Mode savedMode;

    private enum Mode
    {
        Menu,
        Solo,
        Multiplayer,
        Tutorial,
        Win
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
        savedMode = gameMode;
        transitionNum++;
        soloSelected = false;
        switch (SceneManager.GetActiveScene().name)
		{
            case "Main":
                gameMode = Mode.Solo;
                break;

            case "Multi":
                gameMode = Mode.Multiplayer;
                break;

            case "Tutorial":
                tutorialRestart = true;
                gameMode = Mode.Tutorial;
                break;

            case "Main Menu":
                gameMode = Mode.Menu;
                break;
		}
        if (!tutorialRestart)
        {
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
            FadeInSFX();
            FadeInMusic();
        }

        /*
        switch (gameMode)
		{
            case Mode.Solo:
                audioSFX.clip = music[(int)Mode.Solo];
                break;
		}
        */
    }

    #region Core Functions

    private float ConvertToMixer(float input)
    {
        return Mathf.Log10(input) * 20;
    }

    public void AdjustVolume()
    {
        musicTween?.Kill();
        sfxTween?.Kill();
        SFXVolume = PlayerPrefs.GetFloat("Volume_SFX", 1f);
        MusicVolume = PlayerPrefs.GetFloat("Volume_Music", 1f);
        sfxMixer.SetFloat("SFXVol", Mathf.Log10(SFXVolume) * 20);
        musicMixer.SetFloat("MusicVol", Mathf.Log10(MusicVolume) * 20);
    }

    public void FadeOutSFX()
    {
        sfxTween?.Kill();
        sfxTween = sfxMixer.DOSetFloat("SFXVol", ConvertToMixer(0.001f), fadeOUTTime);
        sfxTween.OnComplete(absorbSFXSource.Stop);
        sfxTween.Play();
    }

    public void FadeInSFX()
    {
        if (sfxTween != null && !gameEnd)
        {
            StartCoroutine(CheckIfFading());
        }
        else
        {
            sfxTween?.Kill();
            sfxTween = sfxMixer.DOSetFloat("SFXVol", ConvertToMixer(SFXVolume), fadeOUTTime);
            sfxTween.Play();
        }
    }

    IEnumerator CheckIfFading()
    {
        while (sfxTween != null)
        {
            if (!absorbSFXSource.isPlaying)
            {
                absorbSFXSource.clip = null;
                sfxTween = sfxMixer.DOSetFloat("SFXVol", ConvertToMixer(SFXVolume), fadeOUTTime);
                sfxTween.Play();
            }
            //Debug.Log("Checking Music");
            yield return new WaitForSeconds(.5f); // Check every second
        }
    }

    public void FadeOutMusic()
    {
        musicTween?.Kill();
        musicTween = musicMixer.DOSetFloat("MusicVol", ConvertToMixer(0.001f), fadeOUTTime);
        musicTween.OnComplete(musicSource.Stop);
        musicTween.Play();
        //Debug.Log("FadeOutMusic called");
    }

    public void FadeInMusic()
    {
        if (gameEnd)
        {
            //FadeInSFX();
        }
        musicTween.Kill();
        musicSource.Play();
        musicTween = musicMixer.DOSetFloat("MusicVol", ConvertToMixer(MusicVolume), fadeOUTTime);
        musicTween.Play();
        StartCoroutine(CheckIfPlaying());
        //audioMusic.Play();
        //audioMusic.DOFade(maxVol* MusicVolume, fadeINTime);
    }

    private void RestartMusic()
    {
        //Debug.LogWarning("Fading Out Music and waiting to restart");
        gameMode = savedMode;
        FadeOutMusic();
        StartCoroutine(RestartWait());

    }

    IEnumerator RestartWait()
    {
        yield return new WaitForSeconds(fadeOUTTime + 1f);
        //Debug.LogWarning("Playing music again and fading back in");
        FadeInMusic();
        restartingMusic = false;
    }

    IEnumerator CheckIfPlaying()
    {
        while (musicSource.isPlaying)
        {
            if (musicSource.time > musicSource.clip.length * .99f && !restartingMusic)
            {
                restartingMusic = true;
                RestartMusic();
            }
            //Debug.Log("Checking Music");
            yield return new WaitForSeconds(1f); // Check every second
        }
    }

    #endregion

    #region Menu Buttons

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
        if (!soloSelected)
        {
            sfxSource.PlayOneShot(sfx[3]);
        }
    }

    public void TutorialRestartSelect()
    {
        sfxSource.PlayOneShot(sfx[4]);
        tutorialRestart = true;
    }

    public void MainSelect()
    {
        tutorialRestart = false;
        playerSFXSource.Stop();
        absorbSFXSource.Stop();
        indicatorSFXSource.Stop();
        jetSFXSource.Stop();
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

    #endregion

    #region SFX

    public void InDangerSpeed()
    {
        if (!indicatorSFXSource.isPlaying)
        {
            indicatorSFXSource.volume = 0f;
            indicatorSFXSource.DOFade(1f, 1f);
            indicatorSFXSource.PlayOneShot(sfx[7]);
        }
        /*
        if (musicSource.pitch == 1)
		{
            musicMixer.SetFloat("PitchMaster", 1.1f);
            //musicMixer.SetFloat("PitchBend", .9f);
        }
        */

    }

    public void NormalSpeed()
    {
        Tween tween = indicatorSFXSource.DOFade(0f, fadeOUTTime);
        tween.OnComplete(indicatorSFXSource.Stop);
        tween.Play();
        //musicMixer.SetFloat("PitchMaster", 1f);
        //musicMixer.SetFloat("PitchBend", 1f);
    }

    public void DialogueSFX()
    {
        sfxSource.PlayOneShot(sfx[6]);
    }

    public void StartPlayerJet()
    {
        if (propelTween != null)
        {
            jetSFXSource.volume = 1;
            propelTween.Kill();
        }
        jetSFXSource.clip = playersfx[0];
        jetSFXSource.Play();
    }

    public void StopPlayerJet()
    {
        // Debug.Log("StopPlayerJet");
        //sfxSource.PlayOneShot(playersfx[1]);
        //playerSFXSource.time = playerSFXSource.clip.length * .975f;
        propelTween?.Kill();
        propelTween = jetSFXSource.DOFade(0, .5f);
        propelTween.OnComplete(PlayerSFXSourceReset);
        propelTween.Play();

        //Debug.Log("Stopping");
    }

    private void PlayerSFXSourceReset()
    {
        jetSFXSource.Stop();
        jetSFXSource.volume = 1;
    }

    public void PlayerEject()
    {
        absorbSFXSource.PlayOneShot(playersfx[2]);
    }

    public void PlayerEjectBig()
    {
        absorbSFXSource.PlayOneShot(playersfx[5]); //so PlayerJet sfx doesn't fade it out
    }

    public void PlayerShieldUp()
    {
        playerSFXSource.PlayOneShot(playersfx[3]);
    }

    IEnumerator FirstPopWait()
    {
        float sceneNum = transitionNum;
        yield return new WaitForSeconds(2f);
        if (SceneManager.GetActiveScene().name != "Main Menu" || !soloSelected || sceneNum != transitionNum) //check for soloSelected to fix weird bug where FirstPopWait() was being called when switching back to main menu really quick after skipping cutscene
        {
            yield break;
        }
        sfxSource.PlayOneShot(sfx[1]);
    }

    public void PlayerAbsorbBigSFX()
    {
        absorb2SFXSource.PlayOneShot(absorbsfx[0]);
        //absorbSFXSource.PlayOneShot(absorbsfx[Random.Range(0, absorbsfx.Count)]);
    }

    public void PlayerAbsorbSmallSFX()
    {
        absorb2SFXSource.PlayOneShot(absorbsfx[1]);
    }

    public void PlayerExpandSFX()
    {
        playerSFXSource.PlayOneShot(playersfx[4]);
    }

    public void PlayerWinSFX()
    {
        absorbSFXSource.PlayOneShot(sfx[7]);
        absorbSFXSource.PlayOneShot(sfx[8]);
        FadeOutMusic();
    }

    public void PlayerWinFailSFX()
    {
        absorbSFXSource.Stop();
        FadeInMusic();
    }

    public void PlayerWinSucceedSFX()
    {
        gameEnd = true;
        FadeOutSFX();
        gameMode = Mode.Win;
        musicSource.clip = music[(int)gameMode];
        FadeInMusic();
    }

    #endregion

}
