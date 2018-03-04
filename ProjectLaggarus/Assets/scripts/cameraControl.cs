using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraControl : MonoBehaviour {

    [Header("Коэффициент перемещения камеры")]
    public float MoveMult = 1f;

    [Header("Коэффициент приближения/удаления камеры")]
    public float ScaleMult = 1f;

    [Header("Минимальная высота камеры")]
    public float MinHeight = 4.0f;

    [Header("Максимальная высота камеры")]
    public float MaxHeight = 15.0f;

    void Start () {
		
	}
	
	void Update () {
        Vector3 currCamPos = transform.position;

        if (Input.GetKey(KeyCode.UpArrow))
        {
            currCamPos.z +=  MoveMult;          
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            currCamPos.z -= MoveMult;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            currCamPos.x += MoveMult;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            currCamPos.x -= MoveMult;
        }

        float mouseWheel = Input.GetAxis("Mouse ScrollWheel");
        currCamPos.y -= mouseWheel * ScaleMult;

        if (currCamPos.y < MinHeight) currCamPos.y = MinHeight;
        else if (currCamPos.y > MaxHeight) currCamPos.y = MaxHeight;

        // Будет 1 если камера на максимуме и 0 если в минимуме
        float c = (currCamPos.y - MinHeight) / (MaxHeight - MinHeight);
        c = 1f - c;

        transform.rotation = Quaternion.Euler((1f - (c * c)) * 40f + 50f, 0f, 0f);

        transform.position = currCamPos;
    }
}
