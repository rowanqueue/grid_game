using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Screen : MonoBehaviour
{
    public bool findChildren;
    public List<AnchorGameObject> anchoreds = new List<AnchorGameObject>();
    public bool setAnchor;
    // Start is called before the first frame update
    void Start()
    {

    }
    public void SetAnchor()
    {
        foreach(AnchorGameObject a in anchoreds)
        {
            a.SetAnchor();
        }
    }


#if UNITY_EDITOR
    // Update is called once per frame
    void Update()
    {
        if (findChildren)
        {
            anchoreds.Clear();
            GetChildren(transform);
            findChildren = false;
        }
        if (setAnchor)
        {
            SetAnchor();
            setAnchor = false;
        }
    }
    void GetChildren(Transform _t)
    {
        foreach(Transform child in _t)
        {
            AnchorGameObject a = child.GetComponent<AnchorGameObject>();
            if(a != null)
            {
                anchoreds.Add(a);
            }
            GetChildren(child);
        }
    }
#endif
}
