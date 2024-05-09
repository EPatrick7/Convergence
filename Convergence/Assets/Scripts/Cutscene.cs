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
    private void OnEnable()
    {
        if (allowRepeats || timesRun == 0)
        {
            if(timesRun == 0)
            {
                text = caption.text + "";
            }
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
        foreach(char c in text)
        {
            caption.text += c;
            
            if (c == ' '&&PerWordDelay>0)
            {
                yield return new WaitForSeconds(PerWordDelay);
            }
            else if (PerCharDelay>0)
            {
                yield return new WaitForSeconds(PerCharDelay);
            }
        }
        yield return new WaitForSeconds(PostPrintDelay);

        CinematicBars.Instance.ExitCinematic(CinematicBarOutroDelay);

        float hopeTime = Time.timeSinceLevelLoad + CinematicBarOutroDelay;
        foreach(char c in text)
        {
            caption.text = caption.text.Remove(caption.text.Length - 1);
            yield return new WaitForSeconds(PerCharDelay/10f);
        }
        yield return new WaitForSeconds(Mathf.Max(0,hopeTime-Time.timeSinceLevelLoad));


        gameObject.SetActive(false);
    }
}
