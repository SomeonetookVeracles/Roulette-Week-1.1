using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class OrbGameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 120f; // 2 minutes default
    [SerializeField] private bool startOnAwake = false; // Changed to false by default
    [SerializeField] private bool startOnFirstMove = true; // Start when player moves
    [SerializeField] private GameObject playerObject;
    
    [Header("UI Elements")]
    [SerializeField] private Canvas gameCanvas;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI orbCountText;
    [SerializeField] private TextMeshProUGUI gameStatusText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalTimeText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;
    
    [Header("Visual Settings")]
    [SerializeField] private Color timerNormalColor = Color.white;
    [SerializeField] private Color timerWarningColor = Color.yellow;
    [SerializeField] private Color timerCriticalColor = Color.red;
    [SerializeField] private float warningTime = 30f;
    [SerializeField] private float criticalTime = 10f;
    
    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip orbCollectSound;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip loseSound;
    [SerializeField] private AudioClip warningSound;
    [SerializeField] private AudioClip tickSound;
    
    [Header("Orb Settings")]
    [SerializeField] private GameObject orbPrefab;
    [SerializeField] private Transform[] orbSpawnPoints;
    [SerializeField] private bool randomizeOrbPositions = false;
    [SerializeField] private float orbRespawnDelay = 0f; // 0 = no respawn
    
    // Game State
    private float currentTime;
    private int totalOrbs;
    private int orbsCollected;
    private bool gameActive;
    private bool gameWon;
    private bool waitingForFirstInput = true;
    private bool gameStarted = false;
    private List<CollectibleOrb> allOrbs;
    private float bestTime = float.MaxValue;
    private bool playedWarningSound = false;
    
    // Properties for external access
    public bool GameActive => gameActive;
    public float TimeRemaining => currentTime;
    public int OrbsRemaining => totalOrbs - orbsCollected;
    public bool WaitingForInput => waitingForFirstInput;
    
    private void Awake()
    {
        // Create UI if not assigned
        if (gameCanvas == null)
        {
            CreateGameUI();
        }
        
        // Find all orbs in scene
        allOrbs = new List<CollectibleOrb>();
        CollectibleOrb[] existingOrbs = FindObjectsOfType<CollectibleOrb>();
        foreach (var orb in existingOrbs)
        {
            RegisterOrb(orb);
        }
        
        // Spawn orbs at spawn points if specified
        if (orbPrefab != null && orbSpawnPoints != null && orbSpawnPoints.Length > 0)
        {
            SpawnOrbs();
        }
        
        totalOrbs = allOrbs.Count;
        
        // Setup restart button
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(() => {
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
            });
        }
        
        // Load best time
        bestTime = PlayerPrefs.GetFloat("BestTime", float.MaxValue);
    }
    
    private void Start()
    {
        if (startOnAwake)
        {
            StartGame();
        }
        else if (startOnFirstMove)
        {
            // Set up pre-game state
            SetupPreGame();
        }
        
        // Find player if not assigned
        if (playerObject == null)
        {
            ParkourMovementController controller = FindObjectOfType<ParkourMovementController>();
            if (controller != null)
            {
                playerObject = controller.gameObject;
            }
        }
    }
    
    private void Update()
    {
        // Check for first W press to start the game
        if (waitingForFirstInput && startOnFirstMove && !gameStarted)
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                waitingForFirstInput = false;
                gameStarted = true;
                StartGame();
                
                // Show "GO!" message when starting
                if (gameStatusText != null)
                {
                    gameStatusText.text = "GO! Collect all orbs!";
                    gameStatusText.color = Color.green;
                    StartCoroutine(HideStatusText(2f));
                }
                return;
            }
            
            // Update UI to show "Press W to Start"
            if (timerText != null)
            {
                timerText.text = "Press W to Start";
                timerText.color = Color.white;
                
                // Pulse effect
                float pulse = Mathf.PingPong(Time.time, 1f);
                timerText.transform.localScale = Vector3.one * (1f + pulse * 0.1f);
            }
            
            if (orbCountText != null)
            {
                orbCountText.text = $"Orbs: 0/{totalOrbs}";
            }
            
            if (gameStatusText != null)
            {
                gameStatusText.text = "Press W to begin the challenge!";
                gameStatusText.color = Color.yellow;
            }
            
            return; // Don't process game logic until started
        }
        
        if (!gameActive) return;
        
        // Update timer
        currentTime -= Time.deltaTime;
        
        // Check for game over
        if (currentTime <= 0)
        {
            currentTime = 0;
            EndGame(false);
        }
        
        // Update UI
        UpdateTimerDisplay();
        
        // Check for warning sounds
        if (!playedWarningSound && currentTime <= warningTime)
        {
            playedWarningSound = true;
            if (audioSource != null && warningSound != null)
            {
                audioSource.PlayOneShot(warningSound);
            }
        }
        
        // Play tick sound in critical time
        if (currentTime <= criticalTime && tickSound != null && audioSource != null)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.PlayOneShot(tickSound, 0.5f);
            }
        }
    }
    
    public void StartGame()
    {
        gameActive = true;
        gameWon = false;
        currentTime = gameDuration;
        orbsCollected = 0;
        playedWarningSound = false;
        waitingForFirstInput = false;
        gameStarted = true;
        
        // Reset all orbs
        foreach (var orb in allOrbs)
        {
            if (orb != null)
            {
                orb.ResetOrb();
            }
        }
        
        // Hide game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        // Don't show status text here - it's handled in Update when W is pressed
        
        UpdateOrbCountDisplay();
        
        // Enable player movement
        if (playerObject != null)
        {
            var controller = playerObject.GetComponent<ParkourMovementController>();
            if (controller != null)
            {
                controller.enabled = true;
            }
        }
        
        // Unlock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void SetupPreGame()
    {
        // Initialize game state but don't start timer
        gameActive = false;
        gameWon = false;
        currentTime = gameDuration;
        orbsCollected = 0;
        waitingForFirstInput = true;
        gameStarted = false;
        
        // Make sure orbs are visible but game hasn't started
        foreach (var orb in allOrbs)
        {
            if (orb != null)
            {
                orb.ResetOrb();
            }
        }
        
        // Hide game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        UpdateOrbCountDisplay();
        
        // Enable player movement
        if (playerObject != null)
        {
            var controller = playerObject.GetComponent<ParkourMovementController>();
            if (controller != null)
            {
                controller.enabled = true;
            }
        }
        
        // Unlock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    public void CollectOrb(CollectibleOrb orb)
    {
        // Don't collect orbs before game starts
        if (!gameActive || waitingForFirstInput) return;
        
        orbsCollected++;
        
        // Play sound
        if (audioSource != null && orbCollectSound != null)
        {
            audioSource.PlayOneShot(orbCollectSound);
        }
        
        // Update display
        UpdateOrbCountDisplay();
        
        // Flash collection message
        if (gameStatusText != null)
        {
            gameStatusText.text = $"Orb collected! {totalOrbs - orbsCollected} remaining";
            StartCoroutine(HideStatusText(1.5f));
        }
        
        // Check for win condition
        if (orbsCollected >= totalOrbs)
        {
            EndGame(true);
        }
        
        // Handle respawn if enabled
        if (orbRespawnDelay > 0)
        {
            StartCoroutine(RespawnOrb(orb, orbRespawnDelay));
        }
    }
    
    private void EndGame(bool won)
    {
        gameActive = false;
        gameWon = won;
        
        // Show game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        // Unlock cursor for menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        if (won)
        {
            float completionTime = gameDuration - currentTime;
            
            // Update best time
            if (completionTime < bestTime)
            {
                bestTime = completionTime;
                PlayerPrefs.SetFloat("BestTime", bestTime);
                PlayerPrefs.Save();
            }
            
            // Show win message
            if (gameStatusText != null)
            {
                gameStatusText.text = "You Win! All orbs collected!";
                gameStatusText.color = Color.green;
            }
            
            if (finalTimeText != null)
            {
                finalTimeText.text = $"Time: {FormatTime(completionTime)}\nBest: {FormatTime(bestTime)}";
            }
            
            if (finalScoreText != null)
            {
                finalScoreText.text = $"All {totalOrbs} orbs collected!";
            }
            
            // Play win sound
            if (audioSource != null && winSound != null)
            {
                audioSource.PlayOneShot(winSound);
            }
        }
        else
        {
            // Show lose message
            if (gameStatusText != null)
            {
                gameStatusText.text = "Time's Up! Game Over";
                gameStatusText.color = Color.red;
            }
            
            if (finalTimeText != null)
            {
                finalTimeText.text = "Time's Up!";
            }
            
            if (finalScoreText != null)
            {
                finalScoreText.text = $"Collected: {orbsCollected}/{totalOrbs} orbs";
            }
            
            // Play lose sound
            if (audioSource != null && loseSound != null)
            {
                audioSource.PlayOneShot(loseSound);
            }
        }
        
        // Disable player movement
        if (playerObject != null)
        {
            var controller = playerObject.GetComponent<ParkourMovementController>();
            if (controller != null)
            {
                controller.enabled = false;
            }
        }
    }
    
    public void RestartGame()
    {
        // Reset player position
        if (playerObject != null)
        {
            playerObject.transform.position = Vector3.zero + Vector3.up * 2f;
            playerObject.transform.rotation = Quaternion.identity;
            
            var controller = playerObject.GetComponent<ParkourMovementController>();
            if (controller != null)
            {
                controller.ResetMovement();
            }
        }
        
        // If we're using start on first move, go back to pre-game state
        if (startOnFirstMove)
        {
            SetupPreGame();
        }
        else
        {
            StartGame();
        }
    }
    
    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            timerText.text = FormatTime(currentTime);
            
            // Reset scale first
            timerText.transform.localScale = Vector3.one;
            
            // Change color based on time remaining
            if (currentTime <= criticalTime)
            {
                timerText.color = timerCriticalColor;
                // Make it pulse
                float pulse = Mathf.PingPong(Time.time * 2f, 1f);
                timerText.transform.localScale = Vector3.one * (1f + pulse * 0.1f);
            }
            else if (currentTime <= warningTime)
            {
                timerText.color = timerWarningColor;
            }
            else
            {
                timerText.color = timerNormalColor;
            }
        }
    }
    
    private void UpdateOrbCountDisplay()
    {
        if (orbCountText != null)
        {
            orbCountText.text = $"Orbs: {orbsCollected}/{totalOrbs}";
        }
    }
    
    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time % 1f) * 100f);
        return $"{minutes:00}:{seconds:00}.{milliseconds:00}";
    }
    
    private IEnumerator HideStatusText(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (gameStatusText != null)
        {
            gameStatusText.text = "";
            gameStatusText.color = Color.white;
        }
    }
    
    private IEnumerator RespawnOrb(CollectibleOrb orb, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (orb != null)
        {
            orb.ResetOrb();
            totalOrbs++;
            UpdateOrbCountDisplay();
        }
    }
    
    private void SpawnOrbs()
    {
        foreach (Transform spawnPoint in orbSpawnPoints)
        {
            if (spawnPoint != null && orbPrefab != null)
            {
                GameObject newOrb = Instantiate(orbPrefab, spawnPoint.position, Quaternion.identity);
                CollectibleOrb orbScript = newOrb.GetComponent<CollectibleOrb>();
                if (orbScript == null)
                {
                    orbScript = newOrb.AddComponent<CollectibleOrb>();
                }
                RegisterOrb(orbScript);
            }
        }
    }
    
    public void RegisterOrb(CollectibleOrb orb)
    {
        if (!allOrbs.Contains(orb))
        {
            allOrbs.Add(orb);
            orb.SetGameManager(this);
        }
    }
    
    private void CreateGameUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("Game Canvas");
        gameCanvas = canvasObj.AddComponent<Canvas>();
        gameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create Timer Text
        GameObject timerObj = new GameObject("Timer Text");
        timerObj.transform.SetParent(canvasObj.transform);
        timerText = timerObj.AddComponent<TextMeshProUGUI>();
        timerText.text = "00:00.00";
        timerText.fontSize = 48;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = timerNormalColor;
        
        RectTransform timerRect = timerObj.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 1f);
        timerRect.anchorMax = new Vector2(0.5f, 1f);
        timerRect.pivot = new Vector2(0.5f, 1f);
        timerRect.anchoredPosition = new Vector2(0, -20);
        timerRect.sizeDelta = new Vector2(300, 60);
        
        // Create Orb Count Text
        GameObject orbCountObj = new GameObject("Orb Count Text");
        orbCountObj.transform.SetParent(canvasObj.transform);
        orbCountText = orbCountObj.AddComponent<TextMeshProUGUI>();
        orbCountText.text = "Orbs: 0/0";
        orbCountText.fontSize = 32;
        orbCountText.alignment = TextAlignmentOptions.Center;
        
        RectTransform orbRect = orbCountObj.GetComponent<RectTransform>();
        orbRect.anchorMin = new Vector2(0.5f, 1f);
        orbRect.anchorMax = new Vector2(0.5f, 1f);
        orbRect.pivot = new Vector2(0.5f, 1f);
        orbRect.anchoredPosition = new Vector2(0, -80);
        orbRect.sizeDelta = new Vector2(200, 40);
        
        // Create Status Text
        GameObject statusObj = new GameObject("Status Text");
        statusObj.transform.SetParent(canvasObj.transform);
        gameStatusText = statusObj.AddComponent<TextMeshProUGUI>();
        gameStatusText.text = "";
        gameStatusText.fontSize = 36;
        gameStatusText.alignment = TextAlignmentOptions.Center;
        
        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0.5f);
        statusRect.anchorMax = new Vector2(0.5f, 0.5f);
        statusRect.pivot = new Vector2(0.5f, 0.5f);
        statusRect.anchoredPosition = new Vector2(0, 100);
        statusRect.sizeDelta = new Vector2(600, 50);
    }
}