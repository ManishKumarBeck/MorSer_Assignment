using System;
using UnityEngine;
using UnityEngine.UI;

public class PivotTool : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private GameObject targetObject; // Object to rotate
    [SerializeField] private Camera mainCamera; // Reference to the camera
    [SerializeField] private GameObject pivotMarkerPrefab; // The marker to show the pivot point
    [SerializeField] private GameObject pivotMarkerParent;

    [Header("Rotation Parameters")]
    [SerializeField] private float rotationSpeed = 0.2f; // Rotation speed
    [SerializeField, Range(0.1f, 1f)] private float smoothness = 0.5f; // Rotation smoothness factor
    [SerializeField] private bool rotateX = true; // Rotate around X-axis?
    [SerializeField] private bool rotateY = true; // Rotate around Y-axis?
    [SerializeField] private bool rotateZ = false; // Rotate around Z-axis?
    
    [SerializeField] private Toggle rotateXToggle;
    [SerializeField] private Toggle rotateYToggle;
    [SerializeField] private Toggle rotateZToggle;

    [Header("Reset Settings")]
    [SerializeField] private float resetSpeed = 1f; // Speed of the reset (overall time)
    [SerializeField] private float resetSmoothness = 0.5f; // Smoothness of the reset transition

    private GameObject pivotMarker; // The current pivot marker
    private Vector3 pivotPoint; // Current pivot point of the model
    private bool isPivotSet = false; // Flag to check if the pivot has been set
    private bool isDragging = false; // Flag to check if the user is dragging
    private Vector3 previousMousePosition; // Previous mouse position for rotation calculation

    private Vector3 initialRotation; // Store the initial rotation
    private Vector3 initialPivotPoint; // Store the initial pivot point
    private Vector3 initialPosition; // Store the initial position

    private float resetTimer = 0f; // Timer to track the reset progress
    private bool isResetting = false; // Flag to track if the reset is in progress

    private void OnEnable()
    {
        StartSmoothReset();
    }

    private void OnDisable()
    {
        StartSmoothReset();
    }

    void Start()
    {
        // Store initial values
        if (targetObject != null)
        {
            initialRotation = targetObject.transform.rotation.eulerAngles;
            initialPosition = targetObject.transform.position;
            initialPivotPoint = Vector3.zero;
        }
        
        rotateXToggle.onValueChanged.AddListener(SetRotateX);
        rotateYToggle.onValueChanged.AddListener(SetRotateY);
        rotateZToggle.onValueChanged.AddListener(SetRotateZ);
    }

    void Update()
    {
        HandlePivotChange();
        HandleRotation();

        // If the reset is in progress, update the smooth reset
        if (isResetting)
        {
            SmoothReset();
        }
    }

    // Handle the tap to change pivot point
    void HandlePivotChange()
    {
        if (Input.GetMouseButtonDown(0)) // Left-click  on the screen
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider != null)
                {
                    // Set the new pivot point where the model was clicked
                    pivotPoint = hit.point;

                    // Instantiate the pivot marker at the clicked point
                    if (pivotMarker != null) Destroy(pivotMarker);
                    pivotMarker = Instantiate(pivotMarkerPrefab, pivotPoint, Quaternion.identity);
                    pivotMarker.transform.parent = pivotMarkerParent.transform;
                    isPivotSet = true;
                }
            }

            // Start dragging if a valid pivot is set
            if (isPivotSet)
            {
                isDragging = true;
                previousMousePosition = Input.mousePosition;
            }
        }

        // Stop dragging when mouse button is released
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    // Handle the model rotation based on the new pivot
    void HandleRotation()
    {
        if (isDragging && targetObject != null) // Only rotate when dragging
        {
            Vector3 currentMousePosition = Input.mousePosition;
            Vector3 deltaMouse = currentMousePosition - previousMousePosition;

            // Interpolate for smooth rotation
            float deltaX = deltaMouse.y * rotationSpeed;
            float deltaY = -deltaMouse.x * rotationSpeed;

            // Apply smoothness (lerp the rotation speed)
            deltaX *= smoothness;
            deltaY *= smoothness;

            // Rotate the object around the pivot based on selected axis
            if (rotateX)
                targetObject.transform.RotateAround(pivotPoint, Vector3.right, deltaX);
            if (rotateY)
                targetObject.transform.RotateAround(pivotPoint, Vector3.up, deltaY);
            if (rotateZ)
                targetObject.transform.RotateAround(pivotPoint, Vector3.forward, (deltaX + deltaY) * 0.5f); // Average of both deltas for Z-axis

            previousMousePosition = currentMousePosition;
        }
    }

    // method to start the smooth reset
    public void StartSmoothReset()
    {
        if (targetObject != null)
        {
            isResetting = true;
            resetTimer = 0f; 
        }
    }

    // Smoothly reset the rotation and position
    private void SmoothReset()
    {
        if(!this.transform.gameObject.activeInHierarchy)
            return;
        
        // Increase the timer based on deltaTime
        resetTimer += Time.deltaTime * resetSpeed;

        // Normalize the timer to be between 0 and 1
        float t = Mathf.Clamp01(resetTimer);

        // Interpolate the rotation and position
        targetObject.transform.rotation = Quaternion.Lerp(targetObject.transform.rotation, Quaternion.Euler(initialRotation), t);
        targetObject.transform.position = Vector3.Lerp(targetObject.transform.position, initialPosition, t);
        
        pivotPoint = initialPivotPoint;
        
        // Optionally reset the pivot point (if desired)
        if (pivotMarker != null)
        {
           
           //pivotMarker.transform.position = Vector3.zero;
           pivotMarker.transform.position = Vector3.Lerp(pivotMarker.transform.position, initialPivotPoint, t);

        }

        // If the reset is complete, stop the reset process
        if (t >= 1f)
        {
            isResetting = false;
        }
    }
    
    
    public void SetRotateX(bool value)
    {
        rotateX = value;
        Debug.Log("SetRotateX: " + value);
    }

    public void SetRotateY(bool value)
    {
        rotateY = value;
    }

    public void SetRotateZ(bool value)
    {
        rotateZ = value;
    }

}
