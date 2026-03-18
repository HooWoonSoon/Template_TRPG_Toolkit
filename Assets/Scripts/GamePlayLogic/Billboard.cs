using UnityEngine;
using System.Collections.Generic;

public class Billboards : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private List<Transform> adjustedUI = new List<Transform>();

    private void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        Vector3 cameraPitch = targetCamera.transform.eulerAngles;

        for (int i = 0; i < adjustedUI.Count; i++)
        {
            var ui = adjustedUI[i];
            if (ui == null) continue;

            ui.rotation = Quaternion.Euler(cameraPitch.x, cameraPitch.y, 0f);

        }
    }
}