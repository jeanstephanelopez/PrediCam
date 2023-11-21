using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class ForecastPeriod
{
  public int number;
  public string startTime;
  public string endTime;
  public bool isDaytime;
  public float temperature; 
  public string shortForecast;
}

public class WeatherDataManager : MonoBehaviour
{
    private string latitude;
    private string longitude;
    private ParticleSystem currentEffect; // Currently active weather effect
    public GameObject hourlyTilePrefab; // Assign this in the Unity Editor
    public Transform tilesParentTransform; // Assign this in the Unity Editor

    private List<ForecastPeriod> forecastPeriods = new List<ForecastPeriod>();
    // Start is called before the first frame update
    void Start()
    {
        // Permission checks and location fetching will be handled here
        StartCoroutine(GetLocationAndWeatherData());
    }

    // This method will handle checking for location permissions, 
    // fetching the user's location, and constructing the API call.
    private IEnumerator GetLocationAndWeatherData()
    {
         #if UNITY_EDITOR
        // Mock location data for testing in the editor
        float mockLatitude = 29.651980f; // Replace with your desired latitude for testing
        float mockLongitude = -82.325020f; // Replace with your desired longitude for testing
        // Use the mock location to construct the URL
        string mockUrl = $"https://api.weather.gov/points/{mockLatitude},{mockLongitude}";
        UnityWebRequest mockRequest = UnityWebRequest.Get(mockUrl);
        mockRequest.SetRequestHeader("User-Agent", "MyWeatherApp (contact@myweatherapp.com)");
        yield return mockRequest.SendWebRequest();

        if (mockRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error fetching points data: " + mockRequest.error);
        }
        else
        {
            PointsData pointsData = JsonUtility.FromJson<PointsData>(mockRequest.downloadHandler.text);
            StartCoroutine(GetWeatherData(pointsData.properties.forecastHourly));
        }
    #else
        // First, check if user has location service enabled
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogError("Location services are not enabled by the user");
            yield break;
        }

        // Start the location service
        Input.location.Start();

        // Wait until service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // If the service didn't initialize in 20 seconds or if the connection failed
        if (maxWait < 1 || Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("Location services failed to start");
            yield break;
        }

        // Access granted and location value could be retrieved
        float latitude = Input.location.lastData.latitude;
        float longitude = Input.location.lastData.longitude;

        // Stop the location service if there is no need to query location updates continuously
        Input.location.Stop();

        // Now that we have the location, we fetch the URL to get the forecast
        string url = $"https://api.weather.gov/points/{latitude},{longitude}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("User-Agent", "MyWeatherApp (contact@myweatherapp.com)");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error fetching points data: " + request.error);
        }
        else
        {
            PointsData pointsData = JsonUtility.FromJson<PointsData>(request.downloadHandler.text);
            StartCoroutine(GetWeatherData(pointsData.properties.forecastHourly));
        }
    #endif
    }

    // This coroutine makes the actual API request and handles the response.
    IEnumerator GetWeatherData(string forecastHourlyUrl)
    {
        UnityWebRequest request = UnityWebRequest.Get(forecastHourlyUrl);
        request.SetRequestHeader("User-Agent", "MyWeatherApp (contact@myweatherapp.com)");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error fetching weather data: " + request.error);
        }
        else
        {
            string jsonData = request.downloadHandler.text;
            ProcessWeatherData(jsonData);
        }
    }

    private ForecastPeriod FindCurrentForecast(ForecastPeriod[] periods)
    {
        DateTime now = DateTime.UtcNow; // Use UTC time to match the API response
        ForecastPeriod closestPeriod = null;

        foreach (var period in periods)
        {
            DateTime startTime = DateTime.Parse(period.startTime, null, System.Globalization.DateTimeStyles.RoundtripKind);
            if (startTime > now) continue;

            if (closestPeriod == null || (now - startTime) < (now - DateTime.Parse(closestPeriod.startTime)))
            {
                closestPeriod = period;
            }
        }

        return closestPeriod;
    }

    // This method will take the JSON data from the API and convert it to your data classes.
private void ProcessWeatherData(string jsonData)
{
    ForecastResponse forecastResponse = JsonUtility.FromJson<ForecastResponse>(jsonData);
    forecastPeriods.Clear();  // Clear previous data

    foreach (var period in forecastResponse.properties.periods)
    {
        forecastPeriods.Add(period);
    }

    // Once data is processed, call the DisplayWeatherData method on WeatherDisplayManager
    WeatherDisplayManager displayManager = FindObjectOfType<WeatherDisplayManager>();
    if (displayManager != null)
    {
        displayManager.DisplayWeatherData(forecastPeriods);

        if (WeatherEffectsManager.Instance != null)
        {
        WeatherEffectsManager.Instance.AddListenersToWeatherTiles();
        }
    }
}

private void ActivateWeatherEffect(string shortForecast)
{
    Debug.Log($"Trying to activate weather effect for: {shortForecast}");

    // Deactivate any current weather effect
    if (currentEffect != null)
    {
        currentEffect.Stop();
        Destroy(currentEffect.gameObject);
        currentEffect = null; // Reset the current effect
    }

    // Convert the shortForecast to lowercase for case-insensitive comparison
    string forecastLower = shortForecast.ToLowerInvariant();

    // Look for keywords in the shortForecast and activate the corresponding effect
    foreach (var weatherEffect in weatherEffects)
    {
        // Using ToLowerInvariant to make the comparison case-insensitive
        if (forecastLower.Contains(weatherEffect.forecastKey.ToLowerInvariant()))
        {
            Debug.Log($"Activating effect for: {weatherEffect.forecastKey}");
            // Instantiate the effect at the WeatherManager's position
           currentEffect = Instantiate(weatherEffect.effectPrefab, weatherEffect.originalTransform.position, weatherEffect.originalTransform.rotation);
            currentEffect.Play();
            return; // Found the matching effect, exit the method
        }
    }
}

[System.Serializable]
public class PointsData
{
    public PointsProperties properties;
}

[System.Serializable]
public class PointsProperties
    {
        public string forecastHourly;
        // Add other fields as needed...
    }

[System.Serializable]
public class WeatherEffect
{
    public string forecastKey; // The keyword to look for in the shortForecast
    public ParticleSystem effectPrefab; // The particle system prefab for the weather effect
    public Transform originalTransform; // Add this line to store the original transform
}

public WeatherEffect[] weatherEffects; // Array to hold your weather effects

[System.Serializable]
public class ForecastResponse
{
    public ForecastProperties properties;
}

[System.Serializable]
public class ForecastProperties
{
    public ForecastPeriod[] periods;
}


}
