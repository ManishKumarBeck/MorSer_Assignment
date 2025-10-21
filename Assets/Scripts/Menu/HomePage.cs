using System;
using System.Collections.Generic;
using UnityEngine;

public class HomePage : MonoBehaviour
{
    
    public enum ToolType
    {
        AngleCalculation,
        Pivot,
        Zoom
    }

    [SerializeField] private List<GameObject> tools; 
    [SerializeField] private ToolManager.ToolType currentToolType; 
    [SerializeField] private GameObject SplashScreenCanvas;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToHome();
        }
    }

    
    public void ActivateTool(ToolManager.ToolType toolType)
    {
        
        foreach (var tool in tools)
        {
            tool.SetActive(false);
        }

        
        if (tools[(int)toolType] != null)
        {
            tools[(int)toolType].SetActive(true);
        }
        
        SplashScreenCanvas.SetActive(false);
    }

    
    public void AngleCalculationTool() => ActivateTool(ToolManager.ToolType.AngleCalculation);
    public void PivotTool() => ActivateTool(ToolManager.ToolType.Pivot);
    public void ZoomTool() => ActivateTool(ToolManager.ToolType.Zoom);

    
    public void SetToolFromInspector()
    {
        ActivateTool(currentToolType);
    }

    public void Quit()
    {
        Application.Quit();
    }
    
    public void ReturnToHome()
    {
        
        foreach (var tool in tools)
        {
            tool.SetActive(false);
        }

        
        SplashScreenCanvas.SetActive(true);
    }

}
