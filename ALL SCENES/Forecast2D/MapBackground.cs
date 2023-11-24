using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;

public class Map : MonoBehaviour
{
    public string apiKey;
    public float lat = 30.442018f;
    public float lon = -84.308214f;
    public int zoom = 17;
    public enum resolution { low = 1, high = 2 };
    public resolution mapResolution = resolution.low;
    public enum type { roadmap, satellite, gybrid, terrain };
    public type mapType = type.roadmap;
    public Rect interactiveZone;
    public UniqueStormTracker stormTrackerScript; // Assign this in the Unity Editor
    public CloudTracker cloudTrackerScript;

 public void SetMapInteraction(bool active)
    {
        enableMapInteraction = active;
    }

public int GetCurrentZoomLevel()
{
    return zoom;
}


    private bool enableMapInteraction = true;
    private string url = "";
    private int mapWidth = 850;
    private int mapHeight = 850;
    private Rect rect;

    private string apiKeyLast;
    private float latLast = -33.85660618894087f;
    private float lonLast = 151.21500701957325f;
    private int zoomLast = 17;
    private resolution mapResolutionLast = resolution.low;
    private type mapTypeLast = type.roadmap;
    private bool updateMap = true;
    private int prevZoom;



    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(GetGoogleMap());
        rect = gameObject.GetComponent<RawImage>().rectTransform.rect;
        mapWidth = (int)Math.Round(rect.width);
        mapHeight = (int)Math.Round(rect.height);
        prevZoom = zoom; 
    }

private Vector3 lastMousePosition;
private bool isDragging = false;

   void Update()
    {
        // Handle desktop input
        HandleDesktopInput();

        // Handle mobile input
        HandleMobileInput();

        // Check if the mouse or touch input is within the interactive zone
        bool isInputInInteractiveZone = IsInputInInteractiveZone();

        // Update the map's interactivity based on whether it's within the interactive zone
        enableMapInteraction = !isInputInInteractiveZone;

        if (updateMap && enableMapInteraction &&
            (apiKeyLast != apiKey || !Mathf.Approximately(latLast, lat) || !Mathf.Approximately(lonLast, lon) || zoomLast != zoom || mapResolutionLast != mapResolution || mapTypeLast != mapType))
        {
            // Update the map only if interactivity is enabled and there are changes
            rect = gameObject.GetComponent<RawImage>().rectTransform.rect;
            mapWidth = (int)Math.Round(rect.width);
            mapHeight = (int)Math.Round(rect.height);
            StartCoroutine(GetGoogleMap());
            updateMap = false;
        }
    }

 // Add this method to check if the mouse or touch input is within the interactive zone
    private bool IsInputInInteractiveZone()
    {
        Vector2 inputPosition;

        // Check if it's a mouse click or touch input
        if (Input.touchCount > 0)
        {
            // Use the first touch position
            inputPosition = Input.GetTouch(0).position;
        }
        else
        {
            // Use mouse position
            inputPosition = Input.mousePosition;
        }

        // Convert the input position to local coordinates of the map's parent canvas
        Vector2 localInputPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent.GetComponent<RectTransform>(),
            inputPosition,
            Camera.main,
            out localInputPosition
        );

        // Check if the local input position is within the interactive zone
        return interactiveZone.Contains(localInputPosition);
    }

void HandleDesktopInput()
{
    // Touchpad Zooming (using pinch-to-zoom or scroll gesture)
    float zoomSpeed = 5f; // Adjust as needed for sensitivity
    float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
    if (scrollDelta != 0)
    {
        int oldZoom = zoom; // Store the current zoom level
        zoom -= (int)(scrollDelta * zoomSpeed);
        zoom = Mathf.Clamp(zoom, 2, 20); // Adjust min/max zoom levels as needed
        updateMap = true;

        if (oldZoom != zoom)
        {
            if (stormTrackerScript != null)
                stormTrackerScript.RefreshWeatherData();
            if (cloudTrackerScript != null)
                cloudTrackerScript.RefreshWeatherData();

            Debug.Log("Desktop Zoom Level Changed to: " + zoom);
        }
    }

     if (Input.GetMouseButtonDown(0))
    {
        // Start dragging
        lastMousePosition = Input.mousePosition;
        isDragging = true;
    }

    if (Input.GetMouseButtonUp(0))
    {
        // Stop dragging
        isDragging = false;
    }

    if (isDragging)
    {
        Vector3 currentMousePosition = Input.mousePosition;
        Vector3 delta = currentMousePosition - lastMousePosition;

        // Convert screen delta to geographical coordinates
        // These factors control the conversion rate and will need tuning
        float longitudeFactor = 0.002f / mapWidth; 
        float latitudeFactor = 0.002f / mapHeight;

        lon -= delta.x * longitudeFactor;
        lat -= delta.y * latitudeFactor;
        updateMap = true;

        lastMousePosition = currentMousePosition; // Update the last mouse position
    }

}


void HandleMobileInput()
{
    if (Input.touchCount == 1)
    {
        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            // Start of a single finger touch
            lastTouchPosition = touch.position;
            isTouchDragging = true;
        }
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            // End of the touch
            isTouchDragging = false;
        }

        if (isTouchDragging && touch.phase == TouchPhase.Moved)
        {
            // Calculate delta for panning
            Vector2 delta = touch.position - lastTouchPosition;

            float longitudeFactor = 0.002f / mapWidth;
            float latitudeFactor = 0.002f / mapHeight;

            lon -= delta.x * longitudeFactor;
            lat -= delta.y * latitudeFactor;
            updateMap = true;

            lastTouchPosition = touch.position; // Update the last touch position
        }
        if (prevZoom != zoom)
        {
            if (stormTrackerScript != null)
                stormTrackerScript.RefreshWeatherData();
            if (cloudTrackerScript != null)
                cloudTrackerScript.RefreshWeatherData();

            Debug.Log("Mobile Zoom Level Changed to: " + zoom);
        }
    }
    else if (Input.touchCount == 2)
    {
        // Pinch-to-zoom
        Touch touchZero = Input.GetTouch(0);
        Touch touchOne = Input.GetTouch(1);

        Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
        Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

        float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
        float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

        float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

        zoom += (int)(deltaMagnitudeDiff * 0.01f); // Adjust for sensitivity
        zoom = Mathf.Clamp(zoom, 2, 20); // Adjust min/max zoom levels as needed
        updateMap = true;
    }
}

private Vector2 lastTouchPosition;
private bool isTouchDragging = false;

  IEnumerator GetGoogleMap()
{
    string styles = "&style=feature:poi|element:labels|visibility:off" + // Hides point of interest labels
                    "&style=feature:transit|element:labels|visibility:off"; // Hides transit labels
                    
    url = "https://maps.googleapis.com/maps/api/staticmap?center=" + lat + "," + lon +
          "&zoom=" + zoom + 
          "&size=" + mapWidth + "x" + mapHeight + 
          "&scale=" + (int)mapResolution + 
          "&maptype=" + mapType.ToString().ToLower() + 
          styles + // Add the styles here
          "&key=" + apiKey;

    UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
    yield return www.SendWebRequest();
    if (www.result != UnityWebRequest.Result.Success)
    {
        Debug.Log("WWW ERROR: " + www.error);
    }
    else
    {
        gameObject.GetComponent<RawImage>().texture = ((DownloadHandlerTexture)www.downloadHandler).texture;

        apiKeyLast = apiKey;
        latLast = lat;
        lonLast = lon;
        zoomLast = zoom;
        mapResolutionLast = mapResolution;
        mapTypeLast = mapType;
        updateMap = true;
    }
}


}
