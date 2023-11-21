using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;

public class CloudTracker : MonoBehaviour
{
    public Image mapImage; // Assign this in the Unity Editor
    public Map mapScript; // Assign this in the Unity Editor

    private readonly string apiKey = "53e4537a3f1348b2051e482b77ea33cd";
    private readonly string layer = "clouds_new"; // Changed to clouds layer
    private readonly int zoom = 9; // Adjusted to your desired zoom level
    private int tileX; // To be calculated based on longitude
    private int tileY; // To be calculated based on latitude

    void Start()
    {
        if (mapScript != null)
        {
            int adjustedZoom = Mathf.Max(0, mapScript.GetCurrentZoomLevel() - 1); // Ensure it doesn't go below 0
            tileX = LongitudeToTileX(-84.308214f, adjustedZoom);
            tileY = LatitudeToTileY(30.442018f, adjustedZoom);

            StartCoroutine(GetWeatherTile());
        }
        else
        {
            Debug.LogError("Map script reference not set in CloudTracker.");
        }
    }

    public void RefreshWeatherData()
    {
        int adjustedZoom = Mathf.Max(0, mapScript.GetCurrentZoomLevel() - 1);
        tileX = LongitudeToTileX(-84.308214f, adjustedZoom);
        tileY = LatitudeToTileY(30.442018f, adjustedZoom);

        StartCoroutine(GetWeatherTile());

        Debug.Log("Refreshing Cloud Data at Zoom Level: " + adjustedZoom);
    }

    IEnumerator GetWeatherTile()
    {
        int adjustedZoom = Mathf.Max(0, mapScript.GetCurrentZoomLevel() - 1);
        string url = $"https://tile.openweathermap.org/map/{layer}/{adjustedZoom}/{tileX}/{tileY}.png?appid={apiKey}";

        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error fetching cloud tile: " + webRequest.error);
            }
            else
            {
                Texture2D cloudTileTexture = DownloadHandlerTexture.GetContent(webRequest);
                mapImage.sprite = Sprite.Create(cloudTileTexture, new Rect(0.0f, 0.0f, cloudTileTexture.width, cloudTileTexture.height), new Vector2(0.5f, 0.5f));
                Debug.Log("Cloud Tile Updated Successfully");
            }
        }
    }

    // Convert longitude to tile X coordinate
    private int LongitudeToTileX(float lon, int z)
    {
        return (int)Mathf.Floor((lon + 180.0f) / 360.0f * (1 << z));
    }

    // Convert latitude to tile Y coordinate
    private int LatitudeToTileY(float lat, int z)
    {
        float latRad = lat * Mathf.PI / 180.0f;
        return (int)Mathf.Floor((1.0f - Mathf.Log(Mathf.Tan(latRad) + 1.0f / Mathf.Cos(latRad)) / Mathf.PI) / 2.0f * (1 << z));
    }
}
