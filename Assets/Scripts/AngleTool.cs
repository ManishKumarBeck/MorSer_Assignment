using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(LineRenderer))]
public class AngleTool : MonoBehaviour
{
    [Header("Core Setup")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask selectableLayers; // The layer that can be selected to place points.
    [SerializeField] private GameObject pointMarkerPrefab; // A prefab to mark selected points.
    [SerializeField] private GameObject pointMarkerParent;
    [SerializeField] private LayerMask pointMarkerLayer; // The layer your PointMarker prefabs are on.
    [SerializeField] private bool snapToVertices = false; // If true, points will snap to the nearest mesh vertex.
    [SerializeField] private Toggle snapToggle;
    
    [Header("Visuals")]
    [SerializeField] private LineRenderer lineRenderer; // The LineRenderer to draw lines A-B and B-C.
    [SerializeField] private GameObject arcVisualObject; // The GameObject that will hold the generated arc mesh.
    [SerializeField] private Material arcMaterial; // The material for the transparent arc mesh.
    [SerializeField] private TMPro.TextMeshPro worldAngleText; // World-space text to display the angle value.

    [Header("UI")]
    [SerializeField] private TMPro.TextMeshProUGUI uiAngleText; // Canvas UI Text to display the angle.

    [Header("Arc Settings")]
    [SerializeField, Range(0.05f, 0.5f)] private float arcRadiusFactor = 0.2f; // How large the arc is, as a factor of the shortest line segment.
    [SerializeField, Range(10, 40)] private int arcResolution = 20; // Number of segments in the arc mesh. More is smoother.
    [SerializeField] private float arcVisualOffset = 0.005f; // Small offset to prevent Z-fighting with the model surface.
    [SerializeField] private float lineVisualOffset = 0.005f; // Small offset to prevent Z-fighting with the model surface.
    [SerializeField, Range(1.0f, 3.0f)] private float worldTextOffsetFactor = 1.4f;  // Controls distance of angle text from the vertex.
    

    // --- Private State ---
    private readonly List<Transform> _selectedPoints = new List<Transform>();
    private Mesh _arcMesh;
    private MeshFilter _arcMeshFilter;
    private MeshRenderer _arcMeshRenderer;
    
    private readonly Dictionary<Transform, Vector3> _pointNormals = new Dictionary<Transform, Vector3>();
    private Transform _draggedPoint = null;
    
    // Struct to return vertex data from our helper function
    private struct SnapData
    {
        public Vector3 Position;
        public Vector3 Normal;
    }

    private void OnEnable()
    {
        Reset();
    }

    private void OnDisable()
    {
        Reset();
    }

    private void Start()
    {
        // 1. Get or add components for the arc visual
        if (arcVisualObject == null)
        {
            Debug.LogError("Arc Visual Object is not assigned!");
            enabled = false;
            return;
        }

        _arcMeshFilter = arcVisualObject.GetComponent<MeshFilter>();
        if (_arcMeshFilter == null) 
            _arcMeshFilter = arcVisualObject.AddComponent<MeshFilter>();

        _arcMeshRenderer = arcVisualObject.GetComponent<MeshRenderer>();
        if (_arcMeshRenderer == null) 
            _arcMeshRenderer = arcVisualObject.AddComponent<MeshRenderer>();

        _arcMeshRenderer.material = arcMaterial;
        
        _arcMesh = new Mesh { name = "AngleArcMesh" };
        _arcMeshFilter.mesh = _arcMesh;

        // 2. Get LineRenderer
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        // 3. Initialize UI and visuals
        Reset();
        Debug.Log("AngleTool Initialized. Ready to select points.");
        
        snapToggle.onValueChanged.AddListener(SnapToVertex);
    }

    private void Update()
    {
        // 1. Handle mouse button release (stops dragging)
        if (Input.GetMouseButtonUp(0) && _draggedPoint != null)
        {
            Debug.Log($"Finished dragging {_draggedPoint.name}.");
            
            // If snapping is on, perform a final snap on mouse release
            if (snapToVertices)
            {
                HandleSnapOnDragEnd();
            }
            
            _draggedPoint = null;
        }

        // 2. Handle active dragging
        if (_draggedPoint != null)
        {
            HandlePointDrag();
        }
        // 3. Handle new click (start drag or place point)
        else if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }

        // 4. Handle Visual Updates (runs every frame)
        if (_selectedPoints.Count == 3)
        {
            UpdateAngleVisuals();
        }
    }

    /// <summary>
    /// Casts a ray from the mouse to select a point.
    /// </summary>
    private void HandleMouseClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.cyan, 1.0f);

        // --- Priority 1: Check if we are clicking an existing marker to DRAG it ---
        if (Physics.Raycast(ray, out RaycastHit markerHit, 100f, pointMarkerLayer))
        {
            _draggedPoint = markerHit.transform;
            Debug.Log($"Started dragging point: {_draggedPoint.name}");
            return; 
        }

        // --- Priority 2: If we aren't dragging, check if we can PLACE a new point ---
        if (_selectedPoints.Count < 3)
        {
            if (Physics.Raycast(ray, out RaycastHit modelHit, 100f, selectableLayers))
            {
                var placementData = GetPlacementData(modelHit);
                PlaceNewPoint(placementData.Position, placementData.Normal);
            }
            else
            {
                Debug.Log("Raycast missed. No point selected.");
            }
        }
    }
    
    /// <summary>
    /// Determines the correct position and normal for a new point,
    /// applying snap logic if enabled.
    /// </summary>
    /// <param name="modelHit">The raycast hit on the selectable model.</param>
    /// <returns>A SnapData struct with the final position and normal.</returns>
    private SnapData GetPlacementData(RaycastHit modelHit)
    {
        if (snapToVertices)
        {
            Debug.Log("Snap mode ON. Finding nearest vertex...");
            return GetNearestVertexData(modelHit); // Use the existing snap function
        }
        else
        {
            Debug.Log("Snap mode OFF. Using exact hit point.");
            return new SnapData { Position = modelHit.point, Normal = modelHit.normal };
        }
    }
    
    /// <summary>
    /// Handles moving the _draggedPoint along the surface of the selectable model.
    /// </summary>
    private void HandlePointDrag()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.magenta, 0.1f);

        // Cast against the selectable model to find the new surface position
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, selectableLayers))
        {
            _draggedPoint.position = hit.point;
            _pointNormals[_draggedPoint] = hit.normal;
            
        }
        else
        {
            Debug.LogWarning("Drag ray is not hitting a selectable surface.");
        }
    }
    
    /// <summary>
    /// Called on MouseButtonUp to snap the dragged point to the nearest vertex.
    /// </summary>
    private void HandleSnapOnDragEnd()
    {
        // Re-cast to find the *final* mouse position
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, selectableLayers))
        {
            Debug.Log("Snap on drag end. Finding nearest vertex...");
            var snapData = GetNearestVertexData(hit);
            
            // Set the final snapped position and normal
            _draggedPoint.position = snapData.Position;
            _pointNormals[_draggedPoint] = snapData.Normal;
        }
    }
    
    /// <summary>
    /// Finds the nearest vertex and its normal on a mesh from a RaycastHit.
    /// </summary>
    /// <returns>A SnapData struct containing the World-Space position and normal.</returns>
    private SnapData GetNearestVertexData(RaycastHit hit)
    {
        // Get the MeshCollider
        MeshCollider meshCollider = hit.collider as MeshCollider;
        
        if (meshCollider == null)
        {
            Debug.LogWarning($"Hit collider '{hit.collider.name}' is not a MeshCollider. Attempting to find one on the same GameObject.");
            meshCollider = hit.collider.GetComponent<MeshCollider>();
        }
        
        if (meshCollider == null || meshCollider.sharedMesh == null)
        {
            Debug.LogWarning("Snap target is not a MeshCollider or has no mesh. Falling back to exact point.");
            return new SnapData { Position = hit.point, Normal = hit.normal };
        }

        // Get the mesh and its transform
        Mesh mesh = meshCollider.sharedMesh;
        Transform meshTransform = hit.transform;

        // Convert hit point from world space to mesh's local space
        Vector3 localPoint = meshTransform.InverseTransformPoint(hit.point);
        
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;

        float minDistanceSqr = Mathf.Infinity;
        int nearestIndex = -1;

        // Loop all vertices to find the nearest one
        for (int i = 0; i < vertices.Length; i++)
        {
            // Using sqrMagnitude is faster than Vector3.Distance
            float distSqr = (vertices[i] - localPoint).sqrMagnitude;
            if (distSqr < minDistanceSqr)
            {
                minDistanceSqr = distSqr;
                nearestIndex = i;
            }
        }

        // Found the nearest vertex
        Vector3 nearestLocalVertex = vertices[nearestIndex];
        Vector3 nearestLocalNormal = normals[nearestIndex];
        
        // Convert the local vertex position and normal back to world space
        Vector3 worldPos = meshTransform.TransformPoint(nearestLocalVertex);
        Vector3 worldNormal = meshTransform.TransformDirection(nearestLocalNormal);

        Debug.Log($"Snapped to vertex {nearestIndex} at {worldPos}");

        return new SnapData { Position = worldPos, Normal = worldNormal.normalized };
    }
    

    /// <summary>
    /// Instantiates and registers a new point marker.
    /// </summary>
    private void PlaceNewPoint(Vector3 pointOnSurface, Vector3 surfaceNormal)
    {
        Debug.Log($"Raycast hit at {pointOnSurface}. Placing new point.");

        if (pointMarkerPrefab == null)
        {
            Debug.LogError("Point Marker Prefab is not assigned!");
            return;
        }
        
        GameObject marker = Instantiate(pointMarkerPrefab, pointOnSurface, Quaternion.identity);
        marker.transform.parent = pointMarkerParent.transform;
        marker.name = $"Point_{_selectedPoints.Count + 1}";
        
        // Ensure the new marker is on the correct layer so it can be dragged
        int layerValue = (int)Mathf.Log(pointMarkerLayer.value, 2);
        if (layerValue >= 0 && layerValue <= 31)
        {
            marker.layer = layerValue;
        }
        else
        {
            Debug.LogError($"Point Marker Layer is not set or invalid! Please assign it in the Inspector.");
        }
        
        _selectedPoints.Add(marker.transform);
        _pointNormals[marker.transform] = surfaceNormal;

        Debug.Log($"Added Point {marker.name}. Total points: {_selectedPoints.Count}");
        
    }

    /// <summary>
    /// Calculates and draws all angle visuals (lines, arc, text).
    /// </summary>
    private void UpdateAngleVisuals()
    {
        // Safety check in case points were destroyed or normals are missing
        if (_selectedPoints[0] == null || _selectedPoints[1] == null || _selectedPoints[2] == null ||
            !_pointNormals.ContainsKey(_selectedPoints[0]) ||
            !_pointNormals.ContainsKey(_selectedPoints[1]) ||
            !_pointNormals.ContainsKey(_selectedPoints[2]))
        {
            Debug.LogWarning("A point marker was destroyed or normal data is missing. Clearing tool.");
            Reset();
            return;
        }

        // Get positions
        Vector3 posA = _selectedPoints[0].position;
        Vector3 posB = _selectedPoints[1].position;
        Vector3 posC = _selectedPoints[2].position;

        // Calculate vectors from the vertex (B)
        Vector3 vecBA = posA - posB;
        Vector3 vecBC = posC - posB;

        // Check for zero-length vectors to prevent errors
        if (vecBA.sqrMagnitude < 0.0001f || vecBC.sqrMagnitude < 0.0001f)
        {
            // Points are overlapping, can't calculate angle
            ClearVisuals();
            UpdateUI("--");
            return;
        }

        // Calculate the angle
        float angle = Vector3.Angle(vecBA, vecBC);
        
        // Get the stored normals
        Vector3 normA = _pointNormals[_selectedPoints[0]];
        Vector3 normB = _pointNormals[_selectedPoints[1]];
        Vector3 normC = _pointNormals[_selectedPoints[2]];

        // Calculate offset positions for the LineRenderer to prevent Z-fighting
        // use a slightly larger offset than the arc to ensure it's on top
        float lineOffset = lineVisualOffset * 1.1f; 

        Vector3 offsetPosA = posA + normA * lineOffset;
        Vector3 offsetPosB = posB + normB * lineOffset;
        Vector3 offsetPosC = posC + normC * lineOffset;
        
        // 1. Update Line Renderer
        lineRenderer.positionCount = 3;
        lineRenderer.SetPositions(new[] { offsetPosA, offsetPosB, offsetPosC });

        // 2. Update Arc Mesh
        GenerateArcMesh(posB, vecBA, vecBC, angle);

        // 3. Update World-space Text
        UpdateWorldText(posB, vecBA, vecBC, angle);

        // 4. Update UI Text
        UpdateUI(angle.ToString("F1"));
    }

    /// <summary>
    /// Generates the "pie slice" mesh for the arc.
    /// </summary>
    private void GenerateArcMesh(Vector3 vertexB, Vector3 vecBA, Vector3 vecBC, float angle)
    {
        _arcMesh.Clear();

        // Determine arc radius
        float radius = Mathf.Min(vecBA.magnitude, vecBC.magnitude) * arcRadiusFactor;
        if (radius < 0.01f) return; // Arc is too small to draw

        // Determine axis of rotation
        Vector3 axis = Vector3.Cross(vecBA, vecBC).normalized;
        
        // Handle collinear case (180 or 0 degrees)
        if (axis.sqrMagnitude < 0.0001f)
        {
            // Try an arbitrary perpendicular axis
            axis = Vector3.Cross(vecBA, Vector3.up).normalized;
            if (axis.sqrMagnitude < 0.0001f)
                axis = Vector3.Cross(vecBA, Vector3.forward).normalized;
        }
        
        // Calculate the small offset to prevent Z-fighting
        Vector3 offset = axis * arcVisualOffset;
        
        int vertexCount = arcResolution + 2; // +2 for center and final edge
        var vertices = new Vector3[vertexCount];
        var triangles = new int[arcResolution * 3];


        // Vertex 0 is the center (Point B), with the offset
        vertices[0] = vertexB + offset; 

        // Starting vector for the arc edge
        Vector3 startVec = vecBA.normalized * radius;

        // Build the "fan"
        for (int i = 0; i <= arcResolution; i++)
        {
            float t = (float)i / arcResolution; // 0.0 to 1.0
            Quaternion rotation = Quaternion.AngleAxis(angle * t, axis);
            
            // Add offset to all edge vertices
            vertices[i + 1] = vertexB + (rotation * startVec) + offset; 
        }

        // Build the triangles
        for (int i = 0; i < arcResolution; i++)
        {
            // We must reverse the winding order because we are looking "down" the axis
            // (or, just swap the last two indices)
            triangles[i * 3 + 0] = 0;       // Center point
            triangles[i * 3 + 1] = i + 2;   
            triangles[i * 3 + 2] = i + 1;   
        }
        
        _arcMesh.vertices = vertices;
        _arcMesh.triangles = triangles;
        _arcMesh.RecalculateNormals(); // Recalculate normals 
        _arcMesh.RecalculateBounds();
    }

    /// <summary>
    /// Positions and updates the world-space angle text.
    /// </summary>
    private void UpdateWorldText(Vector3 vertexB, Vector3 vecBA, Vector3 vecBC, float angle)
    {
        if (worldAngleText == null) return;
        
        // Determine axis (same logic as arc)
        Vector3 axis = Vector3.Cross(vecBA, vecBC).normalized;
        if (axis.sqrMagnitude < 0.0001f)
        {
            axis = Vector3.Cross(vecBA, Vector3.up).normalized;
            if (axis.sqrMagnitude < 0.0001f)
                axis = Vector3.Cross(vecBA, Vector3.forward).normalized;
        }

        // Find the bisection of the angle
        Quaternion halfRotation = Quaternion.AngleAxis(angle / 2f, axis);
        Vector3 textDirection = (halfRotation * vecBA.normalized).normalized;
        float radius = Mathf.Min(vecBA.magnitude, vecBC.magnitude) * arcRadiusFactor;

        // Position text just outside the arc
        worldAngleText.transform.position = vertexB + textDirection * (radius * worldTextOffsetFactor);
        
        // Billboard text to face the camera
        worldAngleText.transform.rotation = Quaternion.LookRotation(
            worldAngleText.transform.position - mainCamera.transform.position
        );
        
        worldAngleText.text = $"{angle:F1}°";
    }

    /// <summary>
    /// Updates the main UI text element.
    /// </summary>
    private void UpdateUI(string angleValue)
    {
        if (uiAngleText != null)
        {
            uiAngleText.text = $"{angleValue}°";
        }
    }

    /// <summary>
    /// Hides all active visuals.
    /// </summary>
    private void ClearVisuals()
    {
        if (lineRenderer != null) 
            lineRenderer.positionCount = 0;
        
        if (_arcMesh != null) 
            _arcMesh.Clear();
        
        if (worldAngleText != null) 
            worldAngleText.text = "";
    }

    /// <summary>
    /// Public method for the UI "Reset" button.
    /// </summary>
    public void Reset()
    {
        if(!this.transform.gameObject.activeInHierarchy)
            return;
        
        
        
        // Destroy marker GameObjects
        foreach (Transform point in _selectedPoints)
        {
            if (point != null)
            {
                Destroy(point.gameObject);
            }
        }
        _selectedPoints.Clear();
        _pointNormals.Clear();
        
        _draggedPoint = null; 
        
        ClearVisuals();
        UpdateUI("--");
    }
    
    public void SnapToVertex(bool value)
    {
        snapToVertices = value;
    }
}