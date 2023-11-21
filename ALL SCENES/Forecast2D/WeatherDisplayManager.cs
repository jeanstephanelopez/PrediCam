using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class WeatherDisplayManager : MonoBehaviour
{
    public static WeatherDisplayManager Instance;
    public TextMeshProUGUI locationText; // Assign this in the inspector
    private string googleApiKey = "AIzaSyDvCuA0WB-KoNC5QOrQsZde6tQcQFmaGu0";
    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
        } else {
            Instance = this;
        }

#if UNITY_EDITOR
    // Provide mock coordinates for testing in the Unity editor
    StartCoroutine(UpdateLocationText(29.651980f, -82.325020f));
#else
    // Use actual coordinates on a mobile device
    StartCoroutine(UpdateLocationText(Input.location.lastData.latitude, Input.location.lastData.longitude));
#endif

       }

// Define classes to match the JSON structure of the response
[System.Serializable]
public class GeocodeJSON
{
    public Result[] results;
}

[System.Serializable]
public class Result
{
    public AddressComponent[] address_components;
}

[System.Serializable]
public class AddressComponent
{
    public string long_name;
    public string[] types;
}

IEnumerator UpdateLocationText(float latitude, float longitude)
{
    string requestUrl = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude},{longitude}&key={googleApiKey}";
    using (UnityWebRequest webRequest = UnityWebRequest.Get(requestUrl))
    {
        yield return webRequest.SendWebRequest();

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error fetching location: " + webRequest.error);
        }
        else
        {
            GeocodeJSON geocodeData = JsonUtility.FromJson<GeocodeJSON>(webRequest.downloadHandler.text);
            string city = "";
            string state = "";
            foreach (var result in geocodeData.results)
            {
                foreach (var component in result.address_components)
                {
                    if (Array.Exists(component.types, type => type == "locality"))
                    {
                        city = component.long_name;
                    }
                    if (Array.Exists(component.types, type => type == "administrative_area_level_1"))
                    {
                        state = component.long_name;
                    }
                }
            }
            if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(state))
            {
                locationText.text = $"Current Location: {city}, {state}";
            }
            else
            {
                Debug.Log("City or State not found in the response.");
            }
        }
    }
}


    public GameObject hourlyTilePrefab; // Assign your HourlyTile prefab in the inspector
    public Transform contentParent; // Assign the Scroll View's content transform in the inspector
   

    private Sprite GetIconForForecast(string forecast) {
        if (forecast.Contains("Sunny") || forecast.Contains("Clear")) {
            return Resources.Load<Sprite>("WeatherIcons/sunnySprite2");
        } else if (forecast.Contains("Cloudy") || forecast.Contains("Fog")) {
            return Resources.Load<Sprite>("WeatherIcons/cloudySprite2");
        } else if (forecast.Contains("Rain")) {
            return Resources.Load<Sprite>("WeatherIcons/rainSprite2");
        }
        return null; // Default icon or null if no match
    }

private List<HourlyTileController> allTileControllers = new List<HourlyTileController>();

private string FormatTime(string startTime)
{
    DateTime time = DateTime.Parse(startTime, null, System.Globalization.DateTimeStyles.RoundtripKind);
    string formattedTime = time.ToString("htt");
    string hour = formattedTime.Substring(0, formattedTime.Length - 2);
    string amPm = formattedTime.Substring(formattedTime.Length - 2);
    return hour + "<size=75%>" + amPm + "</size>"; // Adjust '75%' to your preference
}


    public void DisplayWeatherData(List<ForecastPeriod> forecastData)
    {
        // Clear out old tiles
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        allTileControllers.Clear(); // Clear the list for new tiles
        
        // Instantiate a new tile for each ForecastPeriod up to 13
        int tileCount = Mathf.Min(forecastData.Count, 13); 
        for (int i = 0; i < tileCount; i++)
        {
            GameObject tileGO = Instantiate(hourlyTilePrefab, contentParent);
            HourlyTileController tileController = tileGO.GetComponent<HourlyTileController>();
            allTileControllers.Add(tileController); // Add the tile controller to the list
            var period = forecastData[i];
            
        // If it's the first tile, set the time text to "Now"
        string timeText = i == 0 ? "Now" : FormatTime(period.startTime);

        Sprite icon = GetIconForForecast(period.shortForecast);
        tileController.SetData(
            timeText,
            period.temperature,
            period.shortForecast,
            icon
        );

        }
    }



}


