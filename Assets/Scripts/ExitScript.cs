// Purpose: Attached to an Exit/Quit button, closes the game when clicked.
using UnityEngine;

public class ExitButton : MonoBehaviour
{
    // Called by the button's OnClick event
    public void ExitGame()
    {
        // Works in a built game
        Application.Quit();

        // Debug message so you can see it works in the editor
        Debug.Log("Game is exiting... (will only close in a built game)");
    }
}
