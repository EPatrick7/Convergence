using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static System.Net.Mime.MediaTypeNames;

public class Cutscene : MonoBehaviour
{
    [Tooltip("Should the cutscene repeat if enabled a second time?")]
    public bool allowRepeats;
    [Tooltip("Where the text will be printed. (Reads text from the TMPRO at runtime)")]
    public TextMeshProUGUI caption;

    [Tooltip("The text that will be printed out in the cutscene. Multiple entries = pick randomly from the list.")]
    public String[] captionText;

    [Tooltip("How long it will take for the cinematic bars to fully load in.")]
    public float CinematicBarDelay= 1;
    [Tooltip("The delay after the entire caption is printed before ending the cutscene.")]
    public float PostPrintDelay = 3f;
    [Tooltip("The delay after each character in the caption is printed.")]
    public float PerCharDelay = 0.01f;
    [Tooltip("The delay after each word in the caption is printed.")]
    public float PerWordDelay = 0.1f;
    [Tooltip("How long it will take for the cinematic bars to fully load out.")]
    public float CinematicBarOutroDelay = 2f;
    [Tooltip("The audio source to play when caption begins to print.")]
    public AudioSource voiceOver;
    String text;
    int timesRun;
    [HideInInspector]
    public bool DisableSetText;
    private void OnEnable()
    {
        if (allowRepeats || timesRun == 0)
        {
            SetText(captionText[UnityEngine.Random.Range(0,captionText.Length)]);
            
            StartCoroutine(MainLoop());
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    public IEnumerator MainLoop()
    {
        caption.text = "";
        timesRun++;
        CinematicBars.Instance.EnterCinematic(CinematicBarDelay);
        yield return new WaitForSeconds(CinematicBarDelay);
        if(voiceOver!=null)
        {
            voiceOver.Play();
        }
        bool onSkipMode = false;

        foreach(char c in text)
        {
            caption.text += c;
            
            if (c == '<') onSkipMode = true;
            if (c == '>') onSkipMode = false;

            if (!onSkipMode)
            {
                AudioManager.Instance?.DialogueSFX();
                if (c == ' ' && PerWordDelay > 0)
                {
                    yield return new WaitForSeconds(PerWordDelay);
                }
                else if (PerCharDelay > 0)
                {
                    yield return new WaitForSeconds(PerCharDelay);
                }
            }
        }
        yield return new WaitForSeconds(PostPrintDelay);
        onSkipMode = false;

        float hopeTime = Time.timeSinceLevelLoad + CinematicBarOutroDelay;
        foreach(char c in text)
        {
            caption.text = caption.text.Remove(caption.text.Length - 1);

            if (c == '>') onSkipMode = true;
            if (c == '<') onSkipMode = false;
            if (!onSkipMode)
                yield return new WaitForSeconds(PerCharDelay/10f);
        }
        CinematicBars.Instance.ExitCinematic(CinematicBarOutroDelay);
        yield return new WaitForSeconds(Mathf.Max(0,hopeTime-Time.timeSinceLevelLoad));


        gameObject.SetActive(false);
    }

    public void SetText(string t)
    {
        if (DisableSetText)
            return;
        text = t.Trim();
    }
}
