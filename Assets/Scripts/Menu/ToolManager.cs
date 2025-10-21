using System.Collections.Generic;
using UnityEngine;

public class ToolManager : MonoBehaviour
{
    
    public enum ToolType
    {
        AngleCalculation,
        Pivot,
        Zoom
    }

    [SerializeField] private List<GameObject> tools; // List of tools in the Inspector
    [SerializeField] private ToolType currentToolType; // Current selected tool type in the Inspector

    
    public void ActivateTool(ToolType toolType)
    {
        // Deactivate all tools first
        foreach (var tool in tools)
        {
            tool.SetActive(false);
        }

        // Activate the selected tool
        if (tools[(int)toolType] != null)
        {
            tools[(int)toolType].SetActive(true);
        }
    }
    
    public void AngleCalculationTool() => ActivateTool(ToolType.AngleCalculation);
    public void PivotTool() => ActivateTool(ToolType.Pivot);
    public void ZoomTool() => ActivateTool(ToolType.Zoom);

    
    public void SetToolFromInspector()
    {
        ActivateTool(currentToolType);
    }
}