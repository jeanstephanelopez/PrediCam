using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class RiskAssessmentManager : MonoBehaviour
{
    [SerializeField] private TextAsset hurricaneDataCsv; // Assign CSV file in Unity Editor
    [SerializeField] private string googleMapsApiKey; // Assign in Unity Editor

    private string googleMapsBaseUrl = "https://maps.googleapis.com/maps/api";

    void Start()
    {
        // Example usage
        float testLongitude = -80.1917902f;
        float testLatitude = 25.7616798f;
        int testCategory = 3;

        StartCoroutine(RunRiskAssessment(testLongitude, testLatitude, testCategory));
    }

    private IEnumerator RunRiskAssessment(float longitude, float latitude, int hurricaneCategory)
    {
        List<HurricaneData> hurricaneDataList = ParseCsvData(hurricaneDataCsv.text);
        HurricaneData selectedData = hurricaneDataList.Find(data => data.Category == hurricaneCategory);

        if (selectedData != null)
        {
            yield return StartCoroutine(FindNearestInfrastructureSafeZone(longitude, latitude, hurricaneCategory));
            yield return StartCoroutine(GetElevation(longitude, latitude));
            yield return StartCoroutine(GetDistanceFromCoast(longitude, latitude));
        }
        else
        {
            Debug.LogError("Hurricane data for the specified category not found.");
        }
    }

    private IEnumerator FindNearestInfrastructureSafeZone(float longitude, float latitude, int hurricaneCategory)
    {
        string[] safeInfrastructureTypes = { "hospital", "school", "community_centre", "local_government_office", "university", "church", "fire_station", "police" };
        Dictionary<string, float> categoryMinDistances = new Dictionary<string, float>
        {
            { "3", 37.5f }, { "4", 71.5f }, { "5", 80f }
        };

        categoryMinDistances.TryGetValue(hurricaneCategory.ToString(), out float minDistanceMiles);
        float searchRadiusMiles = Mathf.Max(30, minDistanceMiles); 
        float searchRadiusMeters = searchRadiusMiles * 1609.34f;

        foreach (var infrastructureType in safeInfrastructureTypes)
        {
            string placesUrl = $"{googleMapsBaseUrl}/place/nearbysearch/json?location={latitude},{longitude}&radius={searchRadiusMeters}&type={infrastructureType}&key={googleMapsApiKey}";
            UnityWebRequest placesRequest = UnityWebRequest.Get(placesUrl);
            yield return placesRequest.SendWebRequest();

            if (placesRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(placesRequest.error);
                continue;
            }

            PlacesResponse placesResponse = JsonUtility.FromJson<PlacesResponse>(placesRequest.downloadHandler.text);
            if (placesResponse.results.Length > 0)
        {
            var nearestSafePlace = placesResponse.results[0];
            var location = nearestSafePlace.geometry.location;
            yield return StartCoroutine(GetDirections(longitude, latitude, location.lat, location.lng));
            yield break;
        }
    }

    Debug.Log("No safe infrastructure found within the radius.");
}

private IEnumerator GetDirections(float originLong, float originLat, float destLong, float destLat)
{
    string url = $"{googleMapsBaseUrl}/directions/json?origin={originLat},{originLong}&destination={destLat},{destLong}&key={googleMapsApiKey}";
    UnityWebRequest request = UnityWebRequest.Get(url);
    yield return request.SendWebRequest();

    if (request.result != UnityWebRequest.Result.Success)
    {
        Debug.LogError(request.error);
    }
    else
    {
        var directionsResponse = JsonUtility.FromJson<DirectionsResponse>(request.downloadHandler.text);
        // Process the directions response
        // Example: Debug.Log(directionsResponse.routes[0].legs[0].steps[0].html_instructions);
    }
}

[Serializable]
private class DirectionsResponse
{
    public Route[] routes;
}

[Serializable]
private class Route
{
    public Leg[] legs;
}

[Serializable]
private class Leg
{
    public Step[] steps;
}

[Serializable]
private class Step
{
    public string html_instructions; // Direction instructions
    public Distance distance;
    public Duration duration;
}

    private IEnumerator GetElevation(float longitude, float latitude)
    {
        string url = $"{googleMapsBaseUrl}/elevation/json?locations={latitude},{longitude}&key={googleMapsApiKey}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error);
        }
        else
        {
            Debug.Log(request.downloadHandler.text);
        }
    }

    private IEnumerator GetDistanceFromCoast(float longitude, float latitude)
    {
        Vector2 coastCoordinates = new Vector2(-80.1917902f, 25.7616798f);
        string origins = $"{latitude},{longitude}";
        string destinations = $"{coastCoordinates.x},{coastCoordinates.y}";
        string url = $"{googleMapsBaseUrl}/distancematrix/json?origins={origins}&destinations={destinations}&mode=driving&key={googleMapsApiKey}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error);
        }
        else
        {
            Debug.Log(request.downloadHandler.text);
        }
    }

    private List<HurricaneData> ParseCsvData(string csvContent)
    {
        List<HurricaneData> dataList = new List<HurricaneData>();
        string[] lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        for (int i = 1; i < lines.Length; i++) // Skipping the header
        {
            string[] elements = lines[i].Split(',');
            if (elements.Length >= 4)
            {
                dataList.Add(new HurricaneData
                {
                    Category = int.Parse(elements[0]),
                    SustainedWindsMPH = int.Parse(elements[1]),
                    SizeInMiles = int.Parse(elements[2]),
                    DamagePotential = elements[3].Trim('"') // Remove quotes
                });
            }
        }
        return dataList;
    }

    [Serializable]
    public class HurricaneData
    {
        public int Category;
        public int SustainedWindsMPH;
        public int SizeInMiles;
        public string DamagePotential;
    }

    [Serializable]
    private class PlacesResponse
    {
        public PlaceResult[] results;
    }

    [Serializable]
    private class PlaceResult
    {
        public string name;
        public string vicinity;
        // Other fields as necessary
    }

    [Serializable]
    private class ElevationResponse
    {
        public ElevationResult[] results;
    }

    [Serializable]
    private class ElevationResult
    {
        public float elevation; // Elevation in meters above sea level
        public Location location; // Location for which elevation data is provided
        public float resolution; // The maximum distance between data points from which the elevation was interpolated, in meters
    }

    [Serializable]
    private class Location
    {
        public float lat; // Latitude
        public float lng; // Longitude
    }

    [Serializable]
    private class DistanceMatrixResponse
    {
        public string[] destination_addresses;
        public string[] origin_addresses;
        public Row[] rows;
        public string status;
    }

    [Serializable]
    private class Row
    {
        public Element[] elements;
    }

    [Serializable]
    private class Element
    {
        public Distance distance; // Distance information
        public Duration duration; // Duration information
        public string status;
    }

    [Serializable]
    private class Distance
    {
        public string text; // Human-readable representation of the distance
        public int value; // Distance in meters
    }

    [Serializable]
    private class Duration
    {
        public string text; // Human-readable representation of the duration
        public int value; // Duration in seconds
    }
}
