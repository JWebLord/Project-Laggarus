using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class raycast : MonoBehaviour {

    Material groundMaterial;
    public Text text;
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
                text.text = hit.transform.position.ToString() + worldGen.LocalToCube(hit.transform.position).ToString();
                //hit.transform.gameObject.GetComponent<Renderer>().materials[1] = hit.transform.gameObject.GetComponent<Renderer>().materials[0];
            }
        }
	}
}
