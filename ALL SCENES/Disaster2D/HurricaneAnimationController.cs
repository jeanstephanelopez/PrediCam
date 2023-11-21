using UnityEngine;

public class AnimationControl : MonoBehaviour
{
    public Animator animator; // Attach your Animator component here through the inspector

    // Method called by UI buttons to switch animations
    public void SetCategory(int category)
    {
        Debug.Log("SetCategory called with category: " + category);
        if (animator != null)
        {
            // Reset all triggers to ensure only one animation plays
            animator.ResetTrigger("PlayCat1");
            animator.ResetTrigger("PlayCat2");
            animator.ResetTrigger("PlayCat3");
            animator.ResetTrigger("PlayCat4");
            animator.ResetTrigger("PlayCat5");

            // Set the trigger for the corresponding category
            animator.SetTrigger("PlayCat" + category);
        }
        else
        {
            Debug.LogError("Animator not set on AnimationControl script");
        }
    }
}

