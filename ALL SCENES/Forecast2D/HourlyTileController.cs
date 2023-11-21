using UnityEngine;
using TMPro;
using UnityEngine.UI; 
public class HourlyTileController : MonoBehaviour
{
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI temperatureText;
    public TextMeshProUGUI forecastText;
    public Image weatherIcon;
    public Button tileButton;

    public WeatherDisplayManager displayManager;
    private void Start()
    {
        // Assign the displayManager if not done through the inspector
        if (displayManager == null)
        {
            displayManager = FindObjectOfType<WeatherDisplayManager>();
        }

    }
   
    // Call this method to update the tile's information
    public void SetData(string time, float temperature, string forecast, Sprite icon = null)
    {
        timeText.text = time;
        temperatureText.text = temperature.ToString() + "°";
        forecastText.text = forecast;
        weatherIcon.sprite = icon;
    }

    public string GetForecast() {
        return forecastText.text; // Assuming forecastText contains the forecast string
    }
}
