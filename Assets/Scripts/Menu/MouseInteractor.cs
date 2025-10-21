using System;
using UnityEngine;
using Xamin;


public class MouseInteractor : MonoBehaviour
{
    [SerializeField] private CircleSelector menu;
    [SerializeField] private Camera _cam;

    private void Start()
    {
        _cam = GetComponent<Camera>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(2))
        {
            Vector3 mousePosition = Input.mousePosition;
            menu.Open(mousePosition);
        }
    }
}
