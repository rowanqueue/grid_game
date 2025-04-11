using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class Button : MonoBehaviour
{
    public UnityEvent _event;
    public SpriteRenderer display;
    public TextMeshPro words;
    public Color hoverColor;
    bool hover = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(display != null)
        {
            display.color = (hover ? hoverColor : Color.white);
        }
        
        if(hover && Input.GetMouseButtonDown(0))
        {
            _event.Invoke();
        }
    }

    private void OnMouseEnter()
    {
        hover = true;
    }
    private void OnMouseExit()
    {
        hover = false;
    }
}
