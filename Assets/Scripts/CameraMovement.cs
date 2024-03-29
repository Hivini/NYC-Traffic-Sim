using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float dragSpeed = 0.2f;
    private Vector3 dragOrigin;
    private Camera cam;


    void Start()
    {
        cam = Camera.main;
    }


    void Update()
    {
        if (Input.GetAxis("Mouse ScrollWheel") != 0f) // forward
        {
            var newValue = cam.orthographicSize + Input.GetAxis("Mouse ScrollWheel") * 2;
            cam.orthographicSize = Mathf.Clamp(newValue, 0.20f, 15);
        }

        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = Input.mousePosition;
        }

        if (!Input.GetMouseButton(0)) return;

        Vector3 pos = cam.ScreenToViewportPoint(Input.mousePosition - dragOrigin);
        Vector3 move = new Vector3(pos.x * dragSpeed, pos.y * dragSpeed, 0);

        transform.Translate(-move, Space.World);
    }
}
