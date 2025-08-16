using UnityEngine;

public class InteractionSystem : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRange = 3f;
    public KeyCode interactionKey = KeyCode.E;
    public LayerMask interactableLayer = -1;
    
    [Header("UI Settings")]
    public bool showInteractionPrompt = true;
    public string interactionPromptText = "Press E to interact";
    
    private IInteractable currentInteractable = null;
    private Camera playerCamera;
    private bool isInteracting = false;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    void Update()
    {
        DetectInteractable();
        HandleInteractionInput();
    }

    void DetectInteractable()
    {
        if (playerCamera == null) return;
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            
            if (interactable != null && interactable.CanInteract())
            {
                if (currentInteractable != interactable)
                {
                    if (currentInteractable != null)
                    {
                        currentInteractable.OnLookExit();
                    }
                    
                    currentInteractable = interactable;
                    currentInteractable.OnLookEnter();
                }
            }
            else
            {
                ExitCurrentInteractable();
            }
        }
        else
        {
            ExitCurrentInteractable();
        }
        
        Debug.DrawRay(ray.origin, ray.direction * interactionRange, 
                     currentInteractable != null ? Color.green : Color.red, 0.1f);
    }

    void ExitCurrentInteractable()
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnLookExit();
            currentInteractable = null;
        }
    }

    void HandleInteractionInput()
    {
        if (Input.GetKeyDown(interactionKey) && currentInteractable != null && !isInteracting)
        {
            StartCoroutine(InteractWithObject());
        }
    }

    System.Collections.IEnumerator InteractWithObject()
    {
        isInteracting = true;
        yield return StartCoroutine(currentInteractable.OnInteract());
        isInteracting = false;
    }

    void OnGUI()
    {
        if (!showInteractionPrompt || currentInteractable == null) return;
        
        string promptText = currentInteractable.GetInteractionPrompt();
        if (string.IsNullOrEmpty(promptText))
        {
            promptText = interactionPromptText;
        }
        
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 16;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;
        
        Vector2 textSize = style.CalcSize(new GUIContent(promptText));
        float xPos = (Screen.width - textSize.x) / 2f;
        float yPos = Screen.height * 0.7f;
        
        GUI.Label(new Rect(xPos, yPos, textSize.x, textSize.y), promptText, style);
    }

    public bool IsInteracting() => isInteracting;
    public IInteractable GetCurrentInteractable() => currentInteractable;
}