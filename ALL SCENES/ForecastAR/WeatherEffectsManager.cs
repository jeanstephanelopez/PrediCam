using UnityEngine;
using UnityEngine.UI;


public class WeatherEffectsManager : MonoBehaviour
{
    public GameObject rainPrefab;
    //public GameObject sunPrefab;
    public GameObject cloudPrefab;
    private GameObject currentEffectInstance;

    public static WeatherEffectsManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy if another instance is already set
        }
        else
        {
            Instance = this; // Assign this as the instance
        }
    }


    // Call this method from the Start method in WeatherDataManager after DisplayWeatherData is called.
        public void AddListenersToWeatherTiles()
    {
        WeatherDisplayManager displayManager = WeatherDisplayManager.Instance;

        foreach (Transform tileTransform in displayManager.contentParent)
        {
            Button button = tileTransform.GetComponent<Button>();
            HourlyTileController tileController = tileTransform.GetComponent<HourlyTileController>();

            // Use the forecast text from the tile controller for the effect
            button.onClick.AddListener(() => PlayWeatherEffect(tileController.forecastText.text));
        }
    }

    public void StopAllWeatherEffects() {
        if(currentEffectInstance != null) {
            ParticleSystem particleSystem = currentEffectInstance.GetComponent<ParticleSystem>();
            if (particleSystem != null && particleSystem.isPlaying) {
                particleSystem.Stop();
                Destroy(currentEffectInstance);
            }
        }
    }

private void PlayWeatherEffect(string forecast) {
    StopAllWeatherEffects();

    GameObject effectInstance = null;
    Vector3 position;
    Vector3 scale = new Vector3(1, 1, 1); 
    Quaternion rotation = Quaternion.Euler(-90f, 0f, 0f); // Rotation set for the X axis

    // Define positions and scales for each effect
    Vector3 rainPosition = new Vector3(-159, 204, 28);
    Vector3 sunPosition = new Vector3(132, 459, 507); 
    Vector3 cloudPosition = new Vector3(-159, 204, 28); 

    Vector3 rainScale = new Vector3(4, 1, 1);
    Vector3 sunScale = new Vector3(3, 5, 1); 
    Vector3 cloudScale = new Vector3(8, 8, 1); 

    // Here you will define how to play the effects based on the forecast
    if (forecast.Contains("Rain")) {
        position = rainPosition;
        scale = rainScale;
        effectInstance = Instantiate(rainPrefab, position, rotation); // Use the rotation defined above
        effectInstance.transform.localScale = scale;
      //else if (forecast.Contains("Sunny") || forecast.Contains("Clear")) {
        //position = sunPosition;
        //scale = sunScale;
        //effectInstance = Instantiate(sunPrefab, position, Quaternion.identity); // Play sun effect 
        } else if (forecast.Contains("Cloudy") || forecast.Contains("Fog")) {
        position = cloudPosition;
        scale = cloudScale;
        effectInstance = Instantiate(cloudPrefab, position, Quaternion.identity); // Play cloud effect 
    }

     // If an effect was instantiated, set its scale and start its particle system
    if (effectInstance != null) {
        effectInstance.transform.localScale = scale; // Set the scale
        ParticleSystem particleSystem = effectInstance.GetComponent<ParticleSystem>();
        if (particleSystem != null && !particleSystem.isPlaying) {
            particleSystem.Play();
        }
    }

     if (effectInstance != null) {
        // Set the current effect instance
        currentEffectInstance = effectInstance;
}


}
}
