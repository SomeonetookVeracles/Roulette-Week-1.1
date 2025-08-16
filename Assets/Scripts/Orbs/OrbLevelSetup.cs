using UnityEngine;
using UnityEditor;

public class OrbLevelSetup : MonoBehaviour
{
    [Header("Quick Setup")]
    [SerializeField] private bool createTestLevel = false;
    [SerializeField] private int numberOfOrbs = 10;
    [SerializeField] private float spawnRadius = 20f;
    [SerializeField] private float minHeight = 1f;
    [SerializeField] private float maxHeight = 10f;
    
    [Header("Orb Prefab Settings")]
    [SerializeField] private GameObject orbPrefab;
    [SerializeField] private Color[] orbColors = new Color[] {
        new Color(1f, 0.2f, 0.2f),  // Red
        new Color(0.2f, 1f, 0.2f),  // Green
        new Color(0.2f, 0.2f, 1f),  // Blue
        new Color(1f, 1f, 0.2f),    // Yellow
        new Color(1f, 0.2f, 1f),    // Magenta
        new Color(0.2f, 1f, 1f),    // Cyan
    };
    
    [Header("Platform Settings")]
    [SerializeField] private bool createPlatforms = true;
    [SerializeField] private GameObject platformPrefab;
    [SerializeField] private int numberOfPlatforms = 15;
    [SerializeField] private Vector2 platformSizeMin = new Vector2(3f, 3f);
    [SerializeField] private Vector2 platformSizeMax = new Vector2(8f, 8f);
    
    [Header("Wall Settings")]
    [SerializeField] private bool createWalls = true;
    [SerializeField] private float wallHeight = 10f;
    [SerializeField] private float arenaSize = 30f;
    
    private void OnValidate()
    {
        if (createTestLevel)
        {
            createTestLevel = false;
            SetupTestLevel();
        }
    }
    
    public void SetupTestLevel()
    {
        Debug.Log("Setting up test level...");
        
        // Create parent objects for organization
        GameObject orbContainer = new GameObject("Orbs");
        GameObject levelGeometry = new GameObject("Level Geometry");
        
        // Create orbs
        CreateOrbs(orbContainer);
        
        // Create platforms
        if (createPlatforms)
        {
            CreatePlatforms(levelGeometry);
        }
        
        // Create walls
        if (createWalls)
        {
            CreateArenaWalls(levelGeometry);
        }
        
        // Create ground
        CreateGround(levelGeometry);
        
        // Setup game manager if it doesn't exist
        SetupGameManager();
        
        Debug.Log($"Test level created with {numberOfOrbs} orbs!");
    }
    
    private void CreateOrbs(GameObject parent)
    {
        for (int i = 0; i < numberOfOrbs; i++)
        {
            Vector3 randomPos = GetRandomPosition();
            GameObject orb = CreateOrb(randomPos, parent);
            
            // Assign random color
            CollectibleOrb orbScript = orb.GetComponent<CollectibleOrb>();
            if (orbScript != null && orbColors.Length > 0)
            {
                Color randomColor = orbColors[Random.Range(0, orbColors.Length)];
                MeshRenderer renderer = orb.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = randomColor;
                    renderer.material.SetColor("_EmissionColor", randomColor * 2f);
                }
                
                Light orbLight = orb.GetComponent<Light>();
                if (orbLight != null)
                {
                    orbLight.color = randomColor;
                }
            }
            
            orb.name = $"Orb_{i + 1}";
        }
    }
    
    private GameObject CreateOrb(Vector3 position, GameObject parent)
    {
        GameObject orb;
        
        if (orbPrefab != null)
        {
            orb = Instantiate(orbPrefab, position, Quaternion.identity, parent.transform);
        }
        else
        {
            // Create orb from scratch
            orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.transform.position = position;
            orb.transform.localScale = Vector3.one * 0.5f;
            orb.transform.SetParent(parent.transform);
            
            // Add CollectibleOrb component
            CollectibleOrb orbScript = orb.AddComponent<CollectibleOrb>();
            
            // Remove default collider and add trigger
            Destroy(orb.GetComponent<Collider>());
            SphereCollider trigger = orb.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.7f;
        }
        
        return orb;
    }
    
    private void CreatePlatforms(GameObject parent)
    {
        GameObject platformContainer = new GameObject("Platforms");
        platformContainer.transform.SetParent(parent.transform);
        
        for (int i = 0; i < numberOfPlatforms; i++)
        {
            Vector3 position = GetRandomPosition();
            position.y = Random.Range(minHeight, maxHeight);
            
            GameObject platform;
            if (platformPrefab != null)
            {
                platform = Instantiate(platformPrefab, position, Quaternion.identity, platformContainer.transform);
            }
            else
            {
                platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                platform.transform.position = position;
                platform.transform.SetParent(platformContainer.transform);
            }
            
            // Random size
            float width = Random.Range(platformSizeMin.x, platformSizeMax.x);
            float length = Random.Range(platformSizeMin.y, platformSizeMax.y);
            platform.transform.localScale = new Vector3(width, 0.5f, length);
            
            // Random rotation (only Y axis)
            platform.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            
            platform.name = $"Platform_{i + 1}";
            
            // Add to wall layer for wall running
            platform.layer = LayerMask.NameToLayer("Default");
            platform.tag = "Platform";
        }
    }
    
    private void CreateArenaWalls(GameObject parent)
    {
        GameObject wallContainer = new GameObject("Walls");
        wallContainer.transform.SetParent(parent.transform);
        
        // Create 4 walls
        CreateWall(new Vector3(0, wallHeight/2, arenaSize), new Vector3(arenaSize*2, wallHeight, 1), "North Wall", wallContainer);
        CreateWall(new Vector3(0, wallHeight/2, -arenaSize), new Vector3(arenaSize*2, wallHeight, 1), "South Wall", wallContainer);
        CreateWall(new Vector3(arenaSize, wallHeight/2, 0), new Vector3(1, wallHeight, arenaSize*2), "East Wall", wallContainer);
        CreateWall(new Vector3(-arenaSize, wallHeight/2, 0), new Vector3(1, wallHeight, arenaSize*2), "West Wall", wallContainer);
    }
    
    private void CreateWall(Vector3 position, Vector3 scale, string name, GameObject parent)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.transform.SetParent(parent.transform);
        wall.name = name;
        
        // Set to wall layer if it exists
        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer != -1)
        {
            wall.layer = wallLayer;
        }
        
        // Make walls slightly transparent
        MeshRenderer renderer = wall.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material wallMat = new Material(Shader.Find("Standard"));
            wallMat.color = new Color(0.5f, 0.5f, 0.6f, 0.9f);
            wallMat.SetFloat("_Metallic", 0.2f);
            wallMat.SetFloat("_Glossiness", 0.3f);
            renderer.material = wallMat;
        }
    }
    
    private void CreateGround(GameObject parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = Vector3.one * (arenaSize * 0.4f);
        ground.transform.SetParent(parent.transform);
        ground.name = "Ground";
        
        // Set to ground layer if it exists
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer != -1)
        {
            ground.layer = groundLayer;
        }
        
        // Create checkerboard material
        MeshRenderer renderer = ground.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material groundMat = new Material(Shader.Find("Standard"));
            groundMat.color = new Color(0.3f, 0.3f, 0.3f);
            groundMat.SetFloat("_Metallic", 0f);
            groundMat.SetFloat("_Glossiness", 0.2f);
            renderer.material = groundMat;
        }
    }
    
    private Vector3 GetRandomPosition()
    {
        float x = Random.Range(-spawnRadius, spawnRadius);
        float y = Random.Range(minHeight, maxHeight);
        float z = Random.Range(-spawnRadius, spawnRadius);
        return new Vector3(x, y, z);
    }
    
    private void SetupGameManager()
    {
        // Check if game manager exists
        OrbGameManager existingManager = FindObjectOfType<OrbGameManager>();
        if (existingManager == null)
        {
            GameObject managerObj = new GameObject("Orb Game Manager");
            OrbGameManager manager = managerObj.AddComponent<OrbGameManager>();
            
            // Try to find player
            ParkourMovementController player = FindObjectOfType<ParkourMovementController>();
            if (player != null)
            {
                Debug.Log("Game Manager created and linked to player!");
            }
        }
        
        // Setup main camera if needed
        if (Camera.main == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();
        }
        
        // Create directional light if none exists
        Light[] lights = FindObjectsOfType<Light>();
        bool hasDirectional = false;
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                hasDirectional = true;
                break;
            }
        }
        
        if (!hasDirectional)
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.intensity = 1f;
            dirLight.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(45f, -30f, 0);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(OrbLevelSetup))]
public class OrbLevelSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        OrbLevelSetup setup = (OrbLevelSetup)target;
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Create Test Level", GUILayout.Height(30)))
        {
            setup.SetupTestLevel();
        }
        
        if (GUILayout.Button("Clear Level", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Clear Level", "Are you sure you want to delete all orbs and level geometry?", "Yes", "No"))
            {
                GameObject orbs = GameObject.Find("Orbs");
                GameObject geometry = GameObject.Find("Level Geometry");
                
                if (orbs != null) DestroyImmediate(orbs);
                if (geometry != null) DestroyImmediate(geometry);
                
                Debug.Log("Level cleared!");
            }
        }
    }
}
#endif