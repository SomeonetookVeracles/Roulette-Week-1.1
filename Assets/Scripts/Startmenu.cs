// Purpose: Handles Play and Exit buttons in the main menu.
using UnityEngine;
using UnityEngine.SceneManagement; // For loading scenes

public class MenuControls : MonoBehaviour
{
    // Name of the level to load (must be in Build Settings)
    [SerializeField] private string levelName = "MainLevel";

    // Called by Play button OnClick
    public void PlayGame()
    {
        Debug.Log($"Loading scene: {levelName}");
        SceneManager.LoadScene(levelName);
    }

    // Called by Exit button OnClick
    public void ExitGame()
    {
        Debug.Log("Exiting game...");
        Application.Quit(); // Works in build only
    }
}
