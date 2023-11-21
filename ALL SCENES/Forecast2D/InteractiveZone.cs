using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InteractiveZone : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    private ScrollRect scrollRect;
    private bool isDragging = false;
    private Vector2 startDragPosition;

    public Map map; // Assign the Map component in the Inspector

    // The sensitivity for swipe detection (adjust as needed)
    public float swipeSensitivity = 12f;

    public RectTransform interactiveZoneRect; // Link to the interactive zone's RectTransform

    private void Awake()
    {
        // Find the ScrollRect component attached to the UI panel
        scrollRect = GetComponent<ScrollRect>();

        if (scrollRect == null)
        {
          
        }
    }

public void OnPointerEnter(PointerEventData eventData)
    {
        // Disable map interactions
        if (map != null)
        {
            map.enabled = false;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Re-enable map interactions
        if (map != null)
        {
            map.enabled = true;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Called at the beginning of a drag interaction
        isDragging = true;
        startDragPosition = eventData.position;

        // Disable the ScrollRect's scrolling while dragging the interactive zone
        scrollRect.enabled = false;

        // Disable interactions with the Map component if the drag is within the interactive zone
        if (IsWithinInteractiveZone(eventData.position))
        {
            map.enabled = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Called during a drag interaction
        if (isDragging)
        {
            // Calculate the delta position for the swipe
            Vector2 delta = eventData.position - startDragPosition;

            // Implement your logic here for handling the swipe
            // Example: Update weather tiles based on the swipe direction

            // Adjust the sensitivity for swipe detection (higher values make it less sensitive)
            if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x) * swipeSensitivity)
            {
                // Vertical swipe detected (up or down)
                // Implement your vertical swipe logic here
            }
            else if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y) * swipeSensitivity)
            {
                // Horizontal swipe detected (left or right)
                // Implement your horizontal swipe logic here
            }

            startDragPosition = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Called at the end of a drag interaction
        isDragging = false;

        // Re-enable the ScrollRect's scrolling after dragging the interactive zone
        scrollRect.enabled = true;

        // Re-enable interactions with the Map component
        map.enabled = true;
    }

    private bool IsWithinInteractiveZone(Vector2 position)
    {
        // Check if the input position is within the RectTransform's boundaries
        return RectTransformUtility.RectangleContainsScreenPoint(
            interactiveZoneRect,
            position
        );
    }
}
