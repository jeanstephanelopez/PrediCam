using UnityEngine;
using UnityEngine.UI; 
using System.Collections.Generic;
using TMPro;

public class InfoManager : MonoBehaviour
{
    public TextMeshProUGUI informationText;
    
    // Updated data storage for mapping combined button names to content
    private Dictionary<string, string> topRowContent = new Dictionary<string, string>()
    {
        {"Evacuation_Cat1", "Evacuation Category 1 info..."},
        {"Evacuation_Cat2", "Evacuation Category 2 info..."},
        {"Evacuation_Cat3", "Evacuation Category 3 info..."},
        {"Evacuation_Cat4", "Evacuation Category 4  info..."},
        {"Evacuation_Cat5", "Evacuation Category 5 info..."},

        {"PowerOutage_Cat1", "Power Outage Category 1 info..."},
        {"PowerOutage_Cat2", "Power Outage Category 2 info..."},
        {"PowerOutage_Cat3", "Power Outage Category 3 info..."},
        {"PowerOutage_Cat4", "Power Outage Category 4 info..."},    
        {"PowerOutage_Cat5", "Power Outage Category 5 info..."},

        {"SafetyProtocols_Cat1", "Safety Protocols Category 1 info..."},
        {"SafetyProtocols_Cat2", "Safety Protocols Category 2 info..."},
        {"SafetyProtocols_Cat3", "Safety Protocols Category 3 info..."},
        {"SafetyProtocols_Cat4", "Safety Protocols Category 4 info..."},
        {"SafetyProtocols_Cat5", "Safety Protocols Category 5 info..."},

    };

private string selectedTopRow;
private string selectedCategory;

    // Method to be called when a top-row button is clicked
    public void SelectTopRowButton(string buttonName)
    {
        selectedTopRow = buttonName;
        UpdateInformationText();
    }

    // Method to be called when a category button is clicked
    public void SelectCategoryButton(string buttonName)
    {
        selectedCategory = buttonName;
        UpdateInformationText();
    }

    private void UpdateInformationText()
    {
        if (selectedTopRow != null && selectedCategory != null)
        {
            string key = selectedTopRow + "_" + selectedCategory;
            if (topRowContent.ContainsKey(key))
            {
                informationText.text = topRowContent[key];
            }
            else
            {
                informationText.text = "Information not available for this selection.";
            }
        }
    }
}

