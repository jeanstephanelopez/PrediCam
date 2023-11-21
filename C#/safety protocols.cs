using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

public class HurricaneSafetyProtocol
{
    public int Category;
    public int SustainedWinds;
    public int SizeInMiles;
    public string SafetyProtocols;
    public string ProximityToWater;
    public string InfrastructureType;

    public HurricaneSafetyProtocol(string csvLine)
    {
        var values = csvLine.Split(',');
        Category = int.Parse(values[0]);
        SustainedWinds = int.Parse(values[1]);
        SizeInMiles = int.Parse(values[2]);
        SafetyProtocols = values[3];
        ProximityToWater = values[4];
        InfrastructureType = values[5];
    }
}

public class LocationSafetyAnalysis : MonoBehaviour
{
    private string googlePlacesApiKey = "AIzaSyDA5V4TUdhUQ_JxrVEC45XF3oiQRM6Ije0"; // Replace with your Google Places API Key
    private List<HurricaneSafetyProtocol> safetyProtocols = new List<HurricaneSafetyProtocol>();
    private string _waterBodiesResponse; // Store the response JSON here
    private string _infrastructureResponse; // Store the response JSON here

    private float userLatitude;
    private float userLongitude;
    private int hurricaneCategory;

    void Start()
    {
        LoadData();
        // Example usage
        SetUserLocation(30.441970f, -84.308310f); // Example coordinates
        SetHurricaneCategory(3); // Example category
        StartCoroutine(AnalyzeLocationSafety());
    }

    public void SetUserLocation(float latitude, float longitude)
    {
        userLatitude = latitude;
        userLongitude = longitude;
    }

    public void SetHurricaneCategory(int category)
    {
        hurricaneCategory = category;
    }

    private IEnumerator AnalyzeLocationSafety()
    {
        yield return StartCoroutine(FindNearbyWaterBodies());
        string proximityToWater = ProcessWaterBodiesResponse();

        yield return StartCoroutine(FindNearbyInfrastructure());
        string infrastructureType = ProcessInfrastructureResponse();

        string elevation = GetElevation(userLatitude, userLongitude); // This should be an async call

        string safetyProtocols = DetermineSafetyProtocols(hurricaneCategory, elevation, proximityToWater, infrastructureType);
        Debug.Log(safetyProtocols);
    }

    private IEnumerator FindNearbyWaterBodies()
    {
        string url = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={userLatitude},{userLongitude}&radius=5000&type=lake&key={googlePlacesApiKey}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.isNetworkError || request.isHttpError)
        {
            Debug.LogError(request.error);
        }
        else
        {
            _waterBodiesResponse = request.downloadHandler.text;
        }
    }

    private string ProcessWaterBodiesResponse()
    {
        JObject jsonResponse = JObject.Parse(_waterBodiesResponse);
        JArray results = (JArray)jsonResponse["results"];
        if (results.Count == 0) return "Far";

        float closestDistance = float.MaxValue;
        foreach (var result in results)
        {
            var location = result["geometry"]["location"];
            float waterBodyLat = (float)location["lat"];
            float waterBodyLng = (float)location["lng"];
            float distance = CalculateDistance(userLatitude, userLongitude, waterBodyLat, waterBodyLng);
            if (distance < closestDistance) closestDistance = distance;
        }
        return CategorizeProximity(closestDistance);
    }

    private float CalculateDistance(float lat1, float lon1, float lat2, float lon2)
    {
        float R = 6371; // Radius of the Earth in km
        float dLat = Mathf.Deg2Rad(lat2 - lat1);
        float dLon = Mathf.Deg2Rad(lon2 - lon1);
        float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) + Mathf.Cos(Mathf.Deg2Rad(lat1)) * Mathf.Cos(Mathf.Deg2Rad(lat2)) * Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);
        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        return R * c; // Distance in km
    }

    private string CategorizeProximity(float distance)
    {
        if (distance < 1) return "Close";
        if (distance < 5) return "Moderate";
        return "Far";
    }

    private IEnumerator FindNearbyInfrastructure()
    {
        string url = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={userLatitude},{userLongitude}&radius=1000&key={googlePlacesApiKey}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.isNetworkError || request.isHttpError)
        {
            Debug.LogError(request.error);
        }
        else
        {
            _infrastructureResponse = request.downloadHandler.text;
        }
    }

    private string ProcessInfrastructureResponse()
    {
        JObject jsonResponse = JObject.Parse(_infrastructureResponse);
        JArray results = (JArray)jsonResponse["results"];
        int residentialCount = 0, commercialCount = 0, publicAreaCount = 0;

        foreach (var result in results)
        {
            var types = (JArray)result["types"];
            foreach (var type in types)
            {
                string placeType = type.ToString();
                if (IsResidential(placeType)) residentialCount++;
                if (IsCommercial(placeType)) commercialCount++;
                if (IsPublicArea(placeType)) publicAreaCount++;
            }
        }
        return DeterminePredominantType(residentialCount, commercialCount, publicAreaCount);
    }

    private bool IsResidential(string placeType)
    {
        string[] residentialTypes = new string[] { "lodging", "homes", "neighborhood" };
        return residentialTypes.Contains(placeType);
    }

    private bool IsCommercial(string placeType)
    {
        var buildingTypes = new List<string> { "store", "shopping_mall", "office", "restaurant", "supermarket", "bank", "hotel", "apartment", "home", "school", "university", "hospital", "clinic", "government", "embassy", "courthouse", "library", "museum", "church" };
        return buildingTypes.Any(type => placeType.Contains(type));
    }

    private bool IsPublicArea(string placeType)
    {
        var publicAreaTypes = new List<string> { "park", "recreation_area", "public_square", "nature_reserve", "zoo", "stadium" };
        return publicAreaTypes.Any(type => placeType.Contains(type));
    }

    private string DeterminePredominantType(int residential, int commercial, int publicArea)
    {
        if (residential >= commercial && residential >= publicArea) return "Residential";
        if (commercial >= residential && commercial >= publicArea) return "Commercial";
        return "Public Area";
    }

    private string GetElevation(float latitude, float longitude)
    {
        // Placeholder implementation - Should be an async call to Google Maps Elevation API
        return "Medium";
    }

    public string DetermineSafetyProtocols(int hurricaneCategory, string elevation, string proximityToWater, string infrastructureType)
    {
        var matchedProtocols = safetyProtocols.Where(protocol => protocol.Category == hurricaneCategory && protocol.ProximityToWater == proximityToWater && protocol.InfrastructureType == infrastructureType).ToList();
        string aggregatedProtocols = "";
        foreach (var protocol in matchedProtocols)
        {
            aggregatedProtocols += protocol.SafetyProtocols + "\n";
        }
        return aggregatedProtocols;
    }

    private void LoadData()
    {
        TextAsset csvData = Resources.Load<TextAsset>("safety_protocols_simulation");
        string[] dataLines = csvData.text.Split(new char[] { '\n' });
        for (int i = 1; i < dataLines.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(dataLines[i]))
            {
                safetyProtocols.Add(new HurricaneSafetyProtocol(dataLines[i]));
            }
        }
    }
}