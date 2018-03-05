using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class raycast : MonoBehaviour {

    public Material[] GexMaterial;

    public Text text;

    private GameObject lastObj;
	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f))
            {
                if(lastObj != hit.transform.gameObject)
                {
                    if(lastObj != null)
                    {
                        lastObj.GetComponent<Renderer>().material = GexMaterial[0];
                    }
                    lastObj = hit.transform.gameObject;
                    text.text = hit.transform.position.ToString() + worldGen.LocalToCube(hit.transform.position).ToString();
                    hit.transform.gameObject.GetComponent<Renderer>().material = GexMaterial[1];
                }
                    
            }
        }
	}
}
