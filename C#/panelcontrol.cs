using UnityEngine;

public class PanelController : MonoBehaviour
{
    public Animator panelAnimator; // Assign this in the inspector

    private string lastButtonClicked = "";

    void Start()
    {
        // Force the animator to play SlideDown state at the start
        panelAnimator.Play("SlideDown", 0, 0);
    }

    public void TogglePanel(string currentButton)
    {
        if (lastButtonClicked == currentButton)
        {
            // If the same button is clicked, toggle the panel
            bool isUp = panelAnimator.GetBool("IsUp");
            panelAnimator.SetBool("IsUp", !isUp);
        }
        else
        {
            // If a different button is clicked, ensure the panel is up
            if (!panelAnimator.GetBool("IsUp"))
            {
                panelAnimator.SetBool("IsUp", true);
            }
        }

        // Update the last clicked button
        lastButtonClicked = currentButton;
    }
}

