using UnityEngine;
using System;

public class SmartNPCCameraTarget : MonoBehaviour
{
    private new Camera camera;
    
    void Awake()
    {
        camera = GetComponent<Camera>();

        if (!camera) throw new Exception("SmartNPCCameraTarget must be placed on a Camera");
    }

    void Update()
    {
        // GetTarget();
    }

    private void GetTarget()
    {
        RaycastHit hit;

        Vector3 cameraCenter = camera.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, camera.nearClipPlane));

        if (Physics.Raycast(cameraCenter, transform.forward, out hit, 1000))
        {
            GameObject target = hit.transform.gameObject;

            Debug.Log("target: " + target);
        }
    }
}
