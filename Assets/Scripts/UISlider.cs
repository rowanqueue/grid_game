using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISlider : MonoBehaviour
{
    public Vector2 startPoint;
    public Vector2 endPoint;
    bool hover = false;
    bool held = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (held)
        {
            float x = Camera.main.ScreenToWorldPoint(Input.mousePosition).x;
            x -= transform.parent.position.x;
            x = Mathf.Clamp(x, startPoint.x, endPoint.x);
            transform.localPosition = new Vector2(x, startPoint.y);
            if(Input.GetMouseButtonUp(0))
            {
                held = false;
            }
        }
        else
        {
            if (hover)
            {
                if(Input.GetMouseButtonDown(0))
                {
                    held = true;
                }
            }
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
