using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// DeathManager automatically detects the player by tag and kills them
/// if they leave the vertical bounds, returning to the MainMenu.
/// </summary>
public class DeathManager : MonoBehaviour
{
    [Header("Player Limits")]
    public float minY = -10f; // Lower vertical bound
    public float maxY = 20f;  // Upper vertical bound

    private Transform player;

    void Start()
    {
        // Find the player automatically by tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogWarning("DeathManager: No GameObject with tag 'Player' found!");
    }

    void Update()
    {
        if (player == null) return;

        float y = player.position.y;

        // Kill the player if they go out of bounds
        if (y < minY || y > maxY)
        {
            KillPlayer();
        }
    }

    private void KillPlayer()
    {
        // Optionally, play effects here (particles, sounds, animations)
        SceneManager.LoadScene("MainMenu");
    }
}
