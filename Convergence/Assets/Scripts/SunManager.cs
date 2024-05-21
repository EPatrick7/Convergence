using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class SunManager : MonoBehaviour
{

    [SerializeField]
    private float angleIncrement = 0;

    private float angle;

    private ParticleSystem ps;

    private SpriteRenderer sprite;

    [SerializeField]
    private List<TextMeshProUGUI> buttons = new List<TextMeshProUGUI>();

    [SerializeField]
    private TextMeshProUGUI title;
    private Image icon;

    //private Camera camera;

    private Tween TcamSize;

    // Start is called before the first frame update
    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        ps = GetComponentInChildren<ParticleSystem>();
        ps.Stop();
        icon = title.GetComponentInChildren<Image>();
        //camera = Camera.main;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Transform pos = gameObject.transform;
        pos.eulerAngles = new Vector3(pos.eulerAngles.x, pos.eulerAngles.y, pos.eulerAngles.z + angleIncrement);
        angle += angleIncrement;
    }

    public void sceneStart()
	{
        TcamSize?.Kill();

        for (var i = 0; i < buttons.Count; i++) //fade out buttons
		{
            var tempColor = buttons[i].color;
            tempColor.a = 0;
            var text = buttons[i].DOColor(tempColor, 1);
            text.Play();
		}

        Vector3 newPos = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, Camera.main.transform.position.z); //set z to 0 so no clipping out
        
        Color newTCol = title.color;
        newTCol.a = 0;

        Color newICol = icon.color;
        newICol.a = 0;

        Sequence cam = DOTween.Sequence();
        cam.Append(Camera.main.DOOrthoSize(1000, 3)); //zoom out
        cam.Insert(0, Camera.main.transform.DOMove(newPos, 3, true)); //center on sun
        cam.Insert(0, title.transform.DOMoveY(title.transform.position.y + 100, 3, true)); //move title up
        cam.OnComplete(StartPS);
        cam.Play();

        Sequence camShake = DOTween.Sequence();
        camShake.Insert(3, Camera.main.transform.DOShakePosition(15, 20, 20, 45, true, false));
        camShake.Insert(6, title.DOColor(newTCol, 2));
        camShake.Insert(6, icon.DOColor(newICol, 3));
        camShake.Insert(9, Camera.main.DOOrthoSize(50, 1));
        camShake.Play();
    }

    public void StartPS()
	{
        sprite.enabled = false;
        ps.Play();
	}

    void OnDestroy()
	{
        DOTween.KillAll();
	}

}
