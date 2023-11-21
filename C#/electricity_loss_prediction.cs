using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HurricanePredictor : MonoBehaviour
{
    public TextAsset csvFile; // Assign this in the Unity Editor
    public string googleMapsApiKey; // Assign this in the Unity Editor securely

    void Start()
    {
        if (csvFile != null)
        {
            StartCoroutine(ProcessCsvData(csvFile.text));
            TestPrediction(); // Test the prediction functionality
        }
        else
        {
            Debug.LogError("CSV file is not assigned in the Inspector.");
        }
    }

    IEnumerator ProcessCsvData(string csvData)
    {
        List<HurricaneData> hurricaneDataList = new List<HurricaneData>();
        string[] lines = csvData.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var values = lines[i].Split(',');
            if (values.Length >= 3)
            {
                if (int.TryParse(values[0], out int category) &&
                    TryParseDistance(values[1], out int averageDistance) &&
                    float.TryParse(values[2], out float chanceOfLosingElectricity))
                {
                    hurricaneDataList.Add(new HurricaneData(category, averageDistance, chanceOfLosingElectricity));
                }
            }
            else
            {
                Debug.LogWarning($"Line {i + 1} is malformed. Skipping line: {lines[i]}");
            }
        }

        yield return null;
    }

    private bool TryParseDistance(string distanceRange, out int averageDistance)
    {
        var distanceParts = distanceRange.Split('-');
        if (distanceParts.Length == 2 &&
            int.TryParse(distanceParts[0], out int minDistance) &&
            int.TryParse(distanceParts[1], out int maxDistance))
        {
            averageDistance = (minDistance + maxDistance) / 2;
            return true;
        }

        averageDistance = 0;
        return false;
    }

    public void TestPrediction()
    {
        float exampleLat = 29.7604f;
        float exampleLng = -95.3698f;
        int exampleHurricaneCategory = 3;

        Predict(exampleLat, exampleLng, exampleHurricaneCategory);
    }

    public void Predict(float latitude, float longitude, int hurricaneCategory)
    {
        StartCoroutine(MakePrediction(hurricaneCategory, latitude, longitude, (predictedChance, distance, sourceType) =>
        {
            string response = $"Predicted chance of losing electricity: {predictedChance:F2}%, " +
                              $"The nearest power source ({sourceType}) is {distance:F2} miles away.";
            Debug.Log(response);
        }));
    }

    private IEnumerator MakePrediction(int hurricaneCategory, float lat, float lng, Action<float, float, string> onCompleted)
    {
        yield return FindNearestPowerSource(lat, lng, (distance, sourceType) =>
        {
            float predictedChance = CalculateChanceOfLosingElectricity(hurricaneCategory, distance);
            onCompleted?.Invoke(predictedChance, distance, sourceType);
        });
    }

    private float CalculateChanceOfLosingElectricity(int hurricaneCategory, float distanceToPowerSource)
    {
        // Basic formula for demonstration purposes
        // Assuming higher categories and closer distances increase the chance
        float baseChance = hurricaneCategory * 10; // Base chance increases with hurricane category
        float distanceFactor = 100 - distanceToPowerSource; // Closer distance increases the chance
        float chance = Mathf.Clamp(baseChance + distanceFactor, 0, 100); // Clamping to ensure value is between 0 and 100
        return chance;
    }

    public IEnumerator FindNearestPowerSource(float lat, float lng, Action<float, string> onCompleted)
    {
        string[] powerSources = { "Power Plant", "Power Station", "nuclear power plant", "coal power plant", "hydroelectric power plant", "solar power plant" };
        float nearestDistance = float.MaxValue;
        string nearestSource = "";

        foreach (var source in powerSources)
        {
            string placesUrl = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={lat},{lng}&radius=50000&keyword={source}&key={googleMapsApiKey}";
            UnityWebRequest placesRequest = UnityWebRequest.Get(placesUrl);
            yield return placesRequest.SendWebRequest();

            if (placesRequest.isNetworkError || placesRequest.isHttpError)
            {
                Debug.LogError(placesRequest.error);
                continue;
            }

            GoogleMapsResponse placesResponse = JsonUtility.FromJson<GoogleMapsResponse>(placesRequest.downloadHandler.text);

            foreach (var place in placesResponse.results)
            {
                string destinationLat = place.geometry.location.lat.ToString();
                string destinationLng = place.geometry.location.lng.ToString();

                string distanceMatrixUrl = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={lat},{lng}&destinations={destinationLat},{destinationLng}&key={googleMapsApiKey}";
                UnityWebRequest distanceRequest = UnityWebRequest.Get(distanceMatrixUrl);
                yield return distanceRequest.SendWebRequest();

                if (distanceRequest.isNetworkError || distanceRequest.isHttpError)
                {
                    Debug.LogError(distanceRequest.error);
                    continue;
                }

                DistanceMatrixResponse distanceResponse = JsonUtility.FromJson<DistanceMatrixResponse>(distanceRequest.downloadHandler.text);
                float distanceMiles = distanceResponse.rows[0].elements[0].distance.value / 1609.34f;

                if (distanceMiles < nearestDistance)
                {
                    nearestDistance = distanceMiles;
                    nearestSource = source;
                }
            }
        }

        onCompleted?.Invoke(nearestDistance, nearestSource);
    }

    [Serializable]
    public class HurricaneData
    {
        public int Category;
        public int AverageDistance;
        public float ChanceOfLosingElectricity;

        public HurricaneData(int category, int averageDistance, float chanceOfLosingElectricity)
        {
            Category = category;
            AverageDistance = averageDistance;
            ChanceOfLosingElectricity = chanceOfLosingElectricity;
        }
    }

    [Serializable]
    private class GoogleMapsResponse
    {
        public Result[] results;
    }

    [Serializable]
    private class Result
    {
        public Geometry geometry;
    }

    [Serializable]
    private class Geometry
    {
        public Location location;
    }

    [Serializable]
    private class Location
    {
        public float lat;
        public float lng;
    }

    [Serializable]
    private class DistanceMatrixResponse
    {
        public Row[] rows;
    }

    [Serializable]
    private class Row
    {
        public Element[] elements;
    }

    [Serializable]
    private class Element
    {
        public Distance distance;
    }

    [Serializable]
    private class Distance
    {
        public float value;
    }
}
