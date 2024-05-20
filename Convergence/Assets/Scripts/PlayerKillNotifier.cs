using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using Unity.VisualScripting;
using System.Linq;
using UnityEngine.UIElements;

public class PlayerKillNotifier : MonoBehaviour
{
    public static List<PlayerKillNotifier> PlayerKillNotifiers;

    [Range(1, 4)]
    public int PlayerID = 1;

    [SerializeField]
    private TMP_Text text;

    [SerializeField]
    private float perCharDelay = 0.1f;

    private Coroutine notification;

    private List<string> displayText;

    private int col = 0;
    private int eater = 1;
    private int ending = 3;

    private const string defaultEater = "Player";
    private const string defaultEnding = " Killed You";

    private void Start()
    {
        if (PlayerKillNotifiers == null)
        {
            PlayerKillNotifiers = new List<PlayerKillNotifier>();
        }

        PlayerKillNotifiers.Add(this);
        
    /*    foreach (PlayerKillNotifier notifier in PlayerKillNotifiers)
        {
            Debug.LogFormat("{0} | {1}", notifier.PlayerID, notifier);
        }
    */
        text.text = string.Empty;

        displayText = new List<string> { "", defaultEater, "</color>", defaultEnding };
    }

    public static PlayerKillNotifier GetNotifier(int ID)
    {
        if (PlayerKillNotifiers == null || PlayerKillNotifiers.Count == 0) return null;

        foreach (PlayerKillNotifier notifier in PlayerKillNotifiers)
        {
            if (notifier.PlayerID == ID) return notifier;
        }

        return null;
    }
    public void Notify(PlayerPixelManager eater, string eaterText = defaultEater, string endingText = defaultEnding, float duration = 2.5f)
    {
        if (eater == null) return;

        if (notification != null)
        {
            StopCoroutine(notification);
        }

        notification = StartCoroutine(DisplayText(eater, eaterText, endingText, duration));
    }

    private IEnumerator DisplayText(PlayerPixelManager e, string eaterName, string endingText, float duration)
    {
        SetColor(InputManager.GetManager(e.PlayerID).PlayerColors[e.PlayerID-1]);
        SetEater(string.Empty);
        SetEnding(string.Empty);

        string displayedName = string.Empty;

        foreach (char c in eaterName)
        {
            displayedName += c;
            SetEater(displayedName);

            text.text = string.Join(string.Empty, displayText);

            yield return new WaitForSeconds(perCharDelay);
        }

        string displayedEnding = string.Empty;

        foreach (char c in endingText)
        {
            displayedEnding += c;
            SetEnding(displayedEnding);

            text.text = string.Join(string.Empty, displayText);

            yield return new WaitForSeconds(perCharDelay);
        }

        yield return new WaitForSeconds(duration);

        notification = StartCoroutine(Hide());
    }

    public IEnumerator Hide()
    {
        while (displayText[ending] != string.Empty)
        {
            displayText[ending] = displayText[ending].Remove(displayText[ending].Length - 1, 1);

            text.text = string.Join(string.Empty, displayText);

            yield return new WaitForSeconds(perCharDelay / 2);
        }

        while (displayText[eater] != string.Empty)
        {
            displayText[eater] = displayText[eater].Remove(displayText[eater].Length - 1 , 1);

            text.text = string.Join(string.Empty, displayText);

            yield return new WaitForSeconds(perCharDelay / 2);
        }

        text.text = string.Empty;
    }
    private void SetColor(UnityEngine.Color color)
    {
        displayText[col] = string.Format("<color=#{0}>", UnityEngine.ColorUtility.ToHtmlStringRGBA(color));
    }

    private void SetEater(string e)
    {
        displayText[eater] = e;
    }

    private void SetEnding(string e)
    {
        displayText[ending] = e;
    }

    private void OnDestroy()
    {
        PlayerKillNotifiers.Remove(this);
    }
}
