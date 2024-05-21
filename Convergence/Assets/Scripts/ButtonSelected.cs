using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class ButtonSelected : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler//ISelectHandler
{

    [SerializeField]
    private float hoverScale;

    [SerializeField]
    private float normalScale;


    private void FixedUpdate()
    {
        if(pointerOver||(EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject==gameObject))
        {
            if (Input.GetKey(KeyCode.Return) && GravityManager.Instance.PlayerCount > 1&& (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject))
            {
                GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            }
            gameObject.transform.localScale = new Vector3(hoverScale, hoverScale, hoverScale);
        }
        else
        {
            gameObject.transform.localScale = new Vector3(normalScale, normalScale, normalScale);
        }
    }
    bool pointerOver;
    public void OnPointerEnter(PointerEventData eventData)
	{
        pointerOver = true;
	}

    public void OnPointerExit(PointerEventData eventData)
	{
        pointerOver = false;
	}


}
