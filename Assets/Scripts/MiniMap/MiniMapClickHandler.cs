using System;
using UnityEngine;

public class MiniMapClickHandler : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera miniMapCamera;

    [Header("Model Settings")]
    [SerializeField] private Renderer modelRenderer; 
    [SerializeField] private LayerMask modelLayer;  

    [Header("Mini-map UI")]
    [SerializeField] private RectTransform miniMapRect;

    [Header("Camera Movement Settings")]
    [SerializeField] private float moveDistance = 3f;
    [SerializeField] private float smoothTime = 0.5f;
    [SerializeField] private bool useSmoothMovement = true;

    private Vector3 velocity = Vector3.zero;

   
    [SerializeField] private Vector3 originalCameraPosition;
    [SerializeField] private Quaternion originalCameraRotation;

    private void OnEnable()
    {
        ResetCameraPosition();
    }

    private void OnDisable()
    {
        ResetCameraPosition();
    }

    void Start()
    {
        if (mainCamera != null)
        {
            // Store original camera transform
            originalCameraPosition = mainCamera.transform.position;
            originalCameraRotation = mainCamera.transform.rotation;
        }

        if (miniMapCamera != null && mainCamera != null)
        {
            miniMapCamera.transform.position = mainCamera.transform.position;
            miniMapCamera.transform.rotation = mainCamera.transform.rotation;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(miniMapRect, Input.mousePosition))
            {
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(miniMapRect, Input.mousePosition, null, out localPoint))
                {
                    Vector2 normalized = RectPointToNormalized(miniMapRect, localPoint);

                    Vector3 miniMapScreenPoint = new Vector3(
                        normalized.x * miniMapCamera.pixelWidth,
                        normalized.y * miniMapCamera.pixelHeight,
                        0
                    );

                    Ray ray = miniMapCamera.ScreenPointToRay(miniMapScreenPoint);
                    Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 3f);

                    if (Physics.Raycast(ray, out RaycastHit hit, 1000f, modelLayer))
                    {
                        Debug.Log($"[MiniMap] Hit: {hit.collider.name} at {hit.point}");

                        Vector3 direction = (hit.point - mainCamera.transform.position).normalized;
                        Vector3 targetPosition = hit.point - direction * moveDistance;

                        Bounds bounds = modelRenderer.bounds;
                        Vector3 clampedTarget = bounds.ClosestPoint(targetPosition);

                        if (useSmoothMovement)
                            StartCoroutine(MoveCameraSmoothly(clampedTarget));
                        else
                            mainCamera.transform.position = clampedTarget;
                    }
                    else
                    {
                        Debug.LogWarning("[MiniMap] Click missed the model.");
                    }
                }
            }
        }
    }

    private Vector2 RectPointToNormalized(RectTransform rect, Vector2 localPoint)
    {
        float normalizedX = (localPoint.x / rect.rect.width) + rect.pivot.x;
        float normalizedY = (localPoint.y / rect.rect.height) + rect.pivot.y;
        return new Vector2(normalizedX, normalizedY);
    }

    private System.Collections.IEnumerator MoveCameraSmoothly(Vector3 targetPos)
    {
        targetPos.z = targetPos.z * (-1f) + moveDistance;
        float elapsed = 0f;
        Vector3 startPos = mainCamera.transform.position;

        while (elapsed < smoothTime)
        {
            mainCamera.transform.position = Vector3.SmoothDamp(mainCamera.transform.position, targetPos, ref velocity, smoothTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = targetPos;
        Debug.Log($"[Reset Smooth] Final Position: {mainCamera.transform.position}");
    }

    
    public void ResetCameraPosition()
    {
        /*if(!this.gameObject.activeInHierarchy)
            return;*/
        
        StopAllCoroutines(); 
        {
            mainCamera.transform.position = originalCameraPosition;
        }

        mainCamera.transform.rotation = originalCameraRotation;
        Debug.Log("[MiniMap] Camera reset to original position.");
    }
}
