using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Zoom parameters
    public float zoomSpeed = 10f;
    public float minOrthographicSize = 1f;
    public float maxOrthographicSize = 100f;

    // Pan parameters
    public float panSpeed = 0.5f;
    private Vector3 lastMousePosition;

    void Update()
    {
        HandleZoom();
        HandlePan();
    }

    private void HandleZoom()
    {
        // Only works if the camera is orthographic
        if (Camera.main.orthographic)
        {
            float scrollData = Input.GetAxis("Mouse ScrollWheel");
            float newSize = Camera.main.orthographicSize - scrollData * zoomSpeed;
            Camera.main.orthographicSize = Mathf.Clamp(newSize, minOrthographicSize, maxOrthographicSize);
        }
        else
        {
            // For perspective cameras, adjust the field of view
            float scrollData = Input.GetAxis("Mouse ScrollWheel");
            float newFOV = Camera.main.fieldOfView - scrollData * zoomSpeed;
            Camera.main.fieldOfView = Mathf.Clamp(newFOV, 15f, 90f);
        }
    }

    private void HandlePan()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Record mouse position on click
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(0))
        {
            // Calculate how much the mouse has moved
            Vector3 delta = Input.mousePosition - lastMousePosition;
            delta = Camera.main.ScreenToWorldPoint(delta) - Camera.main.ScreenToWorldPoint(Vector3.zero);

            // Move the camera opposite to the mouse movement
            transform.position -= delta;

            // Update last mouse position
            lastMousePosition = Input.mousePosition;
        }
    }
}
