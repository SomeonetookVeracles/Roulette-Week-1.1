using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SphereCollider))]
public class CollectibleOrb : MonoBehaviour
{
    [Header("Orb Settings")]
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float bobAmount = 0.5f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private bool autoSpin = true;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject collectEffect;
    [SerializeField] private float effectDuration = 1f;
    [SerializeField] private Material orbMaterial;
    [SerializeField] private Color orbColor = new Color(0.5f, 0.8f, 1f, 1f);
    [SerializeField] private float glowIntensity = 2f;
    
    [Header("Collection Settings")]
    [SerializeField] private bool requirePlayerTag = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float magnetRange = 5f;
    [SerializeField] private float magnetSpeed = 10f;
    [SerializeField] private bool useMagnet = true;
    
    [Header("Audio")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private float soundVolume = 1f;
    
    [Header("Animation")]
    [SerializeField] private AnimationCurve collectCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float collectAnimDuration = 0.5f;
    
    // Components
    private SphereCollider sphereCollider;
    private MeshRenderer meshRenderer;
    private Light orbLight;
    private AudioSource audioSource;
    private ParticleSystem particles;
    
    // State
    private Vector3 initialPosition;
    private bool isCollected = false;
    private bool isBeingAttracted = false;
    private Transform playerTransform;
    private OrbGameManager gameManager;
    
    // Visual state
    private float currentBobOffset = 0f;
    private Material instanceMaterial;
    
    private void Awake()
    {
        // Get components
        sphereCollider = GetComponent<SphereCollider>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        // Make sure it's a trigger
        sphereCollider.isTrigger = true;
        
        // Store initial position
        initialPosition = transform.position;
        
        // Create mesh if it doesn't exist
        if (meshRenderer == null)
        {
            CreateOrbMesh();
        }
        
        // Setup material
        SetupMaterial();
        
        // Add light component
        SetupLight();
        
        // Setup particles
        SetupParticles();
        
        // Add audio source if collect sound is assigned
        if (collectSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.volume = soundVolume;
        }
        
        // Find game manager if not set
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<OrbGameManager>();
        }
    }
    
    private void Start()
    {
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            // Try to find by component
            ParkourMovementController controller = FindObjectOfType<ParkourMovementController>();
            if (controller != null)
            {
                playerTransform = controller.transform;
            }
        }
    }
    
    private void Update()
    {
        if (isCollected) return;
        
        // Auto rotation
        if (autoSpin)
        {
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        }
        
        // Bobbing motion
        currentBobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        Vector3 newPos = initialPosition + Vector3.up * currentBobOffset;
        
        // Magnet effect (only when game is active and not waiting for input)
        if (useMagnet && playerTransform != null && !isBeingAttracted)
        {
            // Check if game is active and not waiting for input before applying magnet
            if (gameManager == null || (gameManager.GameActive && !gameManager.WaitingForInput))
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                if (distanceToPlayer <= magnetRange)
                {
                    isBeingAttracted = true;
                    StartCoroutine(MagnetToPlayer());
                }
            }
        }
        
        if (!isBeingAttracted)
        {
            transform.position = newPos;
        }
        
        // Pulse glow effect
        if (instanceMaterial != null && instanceMaterial.HasProperty("_EmissionColor"))
        {
            float pulse = Mathf.PingPong(Time.time, 1f);
            Color emissionColor = orbColor * Mathf.LinearToGammaSpace(glowIntensity * (0.8f + pulse * 0.2f));
            instanceMaterial.SetColor("_EmissionColor", emissionColor);
        }
        
        // Update light intensity
        if (orbLight != null)
        {
            float pulse = Mathf.PingPong(Time.time * 2f, 1f);
            orbLight.intensity = 2f + pulse;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        
        // Check if game is active and not waiting for input
        if (gameManager != null && (!gameManager.GameActive || gameManager.WaitingForInput))
        {
            return; // Don't collect if game hasn't started
        }
        
        bool isPlayer = false;
        
        if (requirePlayerTag)
        {
            isPlayer = other.CompareTag(playerTag);
        }
        else
        {
            isPlayer = other.GetComponent<ParkourMovementController>() != null;
        }
        
        if (isPlayer)
        {
            Collect();
        }
    }
    
    private void Collect()
    {
        if (isCollected) return;
        
        isCollected = true;
        
        // Notify game manager
        if (gameManager != null)
        {
            gameManager.CollectOrb(this);
        }
        
        // Play collect animation
        StartCoroutine(CollectAnimation());
        
        // Play sound
        if (audioSource != null && collectSound != null)
        {
            audioSource.PlayOneShot(collectSound);
        }
        
        // Spawn effect
        if (collectEffect != null)
        {
            GameObject effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
        
        // Play particles
        if (particles != null)
        {
            particles.Play();
        }
    }
    
    private IEnumerator CollectAnimation()
    {
        // Disable collider
        sphereCollider.enabled = false;
        
        float elapsedTime = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = Vector3.zero;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * 2f;
        
        while (elapsedTime < collectAnimDuration)
        {
            float t = elapsedTime / collectAnimDuration;
            float curveValue = collectCurve.Evaluate(t);
            
            // Scale down
            transform.localScale = Vector3.Lerp(startScale, endScale, curveValue);
            
            // Move up
            transform.position = Vector3.Lerp(startPos, endPos, curveValue);
            
            // Spin faster
            transform.Rotate(Vector3.up * rotationSpeed * 5f * Time.deltaTime);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Hide object
        meshRenderer.enabled = false;
        if (orbLight != null)
        {
            orbLight.enabled = false;
        }
        
        // Wait for audio to finish if playing
        if (audioSource != null && audioSource.isPlaying)
        {
            yield return new WaitForSeconds(audioSource.clip.length);
        }
        
        // Destroy or disable
        gameObject.SetActive(false);
    }
    
    private IEnumerator MagnetToPlayer()
    {
        while (!isCollected && playerTransform != null)
        {
            // Stop magnet if game hasn't started or is waiting for input
            if (gameManager != null && (!gameManager.GameActive || gameManager.WaitingForInput))
            {
                isBeingAttracted = false;
                yield break;
            }
            
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distance > magnetRange * 1.5f)
            {
                // Player moved away, stop magnet
                isBeingAttracted = false;
                yield break;
            }
            
            // Move towards player
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            float speed = magnetSpeed * (1f - (distance / magnetRange)); // Speed up as we get closer
            transform.position += direction * speed * Time.deltaTime;
            
            // Add spinning effect
            transform.Rotate(Vector3.up * rotationSpeed * 3f * Time.deltaTime);
            
            yield return null;
        }
        
        isBeingAttracted = false;
    }
    
    public void ResetOrb()
    {
        isCollected = false;
        isBeingAttracted = false;
        transform.position = initialPosition;
        transform.localScale = Vector3.one;
        
        // Re-enable components
        sphereCollider.enabled = true;
        meshRenderer.enabled = true;
        if (orbLight != null)
        {
            orbLight.enabled = true;
        }
        
        gameObject.SetActive(true);
    }
    
    public void SetGameManager(OrbGameManager manager)
    {
        gameManager = manager;
    }
    
    private void CreateOrbMesh()
    {
        // Add mesh filter and renderer
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        
        // Create a sphere mesh
        GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        meshFilter.mesh = tempSphere.GetComponent<MeshFilter>().mesh;
        DestroyImmediate(tempSphere);
        
        // Scale to appropriate size
        transform.localScale = Vector3.one * 0.5f;
    }
    
    private void SetupMaterial()
    {
        if (meshRenderer == null) return;
        
        // Create material instance
        if (orbMaterial != null)
        {
            instanceMaterial = new Material(orbMaterial);
        }
        else
        {
            // Create default material
            instanceMaterial = new Material(Shader.Find("Standard"));
            instanceMaterial.color = orbColor;
            instanceMaterial.SetFloat("_Metallic", 0.5f);
            instanceMaterial.SetFloat("_Glossiness", 0.8f);
        }
        
        // Enable emission
        instanceMaterial.EnableKeyword("_EMISSION");
        instanceMaterial.SetColor("_EmissionColor", orbColor * Mathf.LinearToGammaSpace(glowIntensity));
        
        meshRenderer.material = instanceMaterial;
    }
    
    private void SetupLight()
    {
        orbLight = gameObject.AddComponent<Light>();
        orbLight.type = LightType.Point;
        orbLight.color = orbColor;
        orbLight.intensity = 2f;
        orbLight.range = 5f;
        orbLight.shadows = LightShadows.Soft;
    }
    
    private void SetupParticles()
    {
        GameObject particleObj = new GameObject("Orb Particles");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = Vector3.zero;
        
        particles = particleObj.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.duration = 0.5f;
        main.startLifetime = 1f;
        main.startSpeed = 5f;
        main.startSize = 0.1f;
        main.startColor = orbColor;
        main.maxParticles = 50;
        main.playOnAwake = false;
        
        var emission = particles.emission;
        emission.enabled = true;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0.0f, 50)
        });
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;
        
        var velocityOverLifetime = particles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(2f);
        
        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw magnet range
        if (useMagnet)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawWireSphere(transform.position, magnetRange);
        }
    }
}