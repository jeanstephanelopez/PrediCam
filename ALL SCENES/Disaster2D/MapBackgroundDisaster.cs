using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MapBackgroundDisaster : MonoBehaviour
{
    public string apiKey;
    public float lat = 29.651980f;
    public float lon = -82.325020f;
    public int zoom = 8; // Default zoom set to 8
    public Map.resolution mapResolution = Map.resolution.low;
    public Map.type mapType = Map.type.roadmap;
    
    private string url = "";
    private int mapWidth = 850;
    private int mapHeight = 850;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(GetGoogleMap());
    }

    IEnumerator GetGoogleMap()
    {
        url = "https://maps.googleapis.com/maps/api/staticmap?center=" + lat + "," + lon +
              "&zoom=" + zoom +
              "&size=" + mapWidth + "x" + mapHeight +
              "&scale=" + (int)mapResolution +
              "&maptype=" + mapType.ToString().ToLower() +
              "&key=" + apiKey;

        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("WWW ERROR: " + www.error);
        }
        else
        {
            gameObject.GetComponent<RawImage>().texture = DownloadHandlerTexture.GetContent(www);
        }
    }
}

