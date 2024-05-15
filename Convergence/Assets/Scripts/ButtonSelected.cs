using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonSelected : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler//ISelectHandler
{

    [SerializeField]
    private float hoverScale;

    [SerializeField]
    private float normalScale;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Update()
    {

    }

    public void OnPointerEnter(PointerEventData eventData)
	{
        gameObject.transform.localScale = new Vector3(hoverScale, hoverScale, hoverScale);
	}

    public void OnPointerExit(PointerEventData eventData)
	{
        gameObject.transform.localScale = new Vector3(normalScale, normalScale, normalScale);
	}

    /*
    public void OnSelect(BaseEventData eventData)
	{

	}
    */

}
