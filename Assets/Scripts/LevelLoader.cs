// Purpose: When attached to an object in your level scene,
// automatically loads the player scene additively at runtime.
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    // Name of the player scene (must be in Build Settings)
    [SerializeField] private string playerSceneName = "PlayerScene";

    private void Start()
    {
        // Load the player scene additively (keeps current scene loaded)
        if (!SceneManager.GetSceneByName(playerSceneName).isLoaded)
        {
            SceneManager.LoadSceneAsync(playerSceneName, LoadSceneMode.Additive);
            Debug.Log($"Loading player scene: {playerSceneName}");
        }
    }
}
