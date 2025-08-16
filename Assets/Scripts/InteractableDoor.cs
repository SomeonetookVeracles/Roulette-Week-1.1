using UnityEngine;
using System.Collections;

public class InteractableDoor : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    public bool startsOpen = false;
    public float openAngle = 90f;
    public float openSpeed = 2f;
    public bool autoClose = false;
    public float autoCloseDelay = 3f;
    public bool isLocked = false;
    
    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip lockedSound;
    
    [Header("Visual Feedback")]
    public GameObject lockIndicator; // Visual element showing door is locked
    public Material lockedMaterial;
    public Material unlockedMaterial;
    
    // Door state
    private bool isOpen = false;
    private bool isMoving = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;
    
    // Components
    private AudioSource audioSource;
    private MeshRenderer meshRenderer;
    private Collider doorCollider;
    
    // Highlighting
    private Material originalMaterial;
    private bool isHighlighted = false;

    void Start()
    {
        // Get components
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        meshRenderer = GetComponent<MeshRenderer>();
        doorCollider = GetComponent<Collider>();
        
        // Store original state
        closedRotation = transform.rotation;
        openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
        
        if (meshRenderer != null)
        {
            originalMaterial = meshRenderer.material;
        }
        
        // Set initial state
        isOpen = startsOpen;
        if (startsOpen)
        {
            transform.rotation = openRotation;
        }
        
        // Update door appearance
        UpdateDoorAppearance();
    }

    void UpdateDoorAppearance()
    {
        // Update lock indicator
        if (lockIndicator != null)
        {
            lockIndicator.SetActive(isLocked);
        }
        
        // Update material
        if (meshRenderer != null)
        {
            if (isLocked && lockedMaterial != null)
            {
                meshRenderer.material = lockedMaterial;
            }
            else if (!isLocked && unlockedMaterial != null)
            {
                meshRenderer.material = unlockedMaterial;
            }
            else if (!isHighlighted)
            {
                meshRenderer.material = originalMaterial;
            }
        }
    }

    public void OnLookEnter()
    {
        isHighlighted = true;
        
        // Highlight the door when looked at (unless locked material is showing)
        if (meshRenderer != null && !isLocked)
        {
            Color highlightColor = meshRenderer.material.color;
            highlightColor = Color.Lerp(highlightColor, Color.white, 0.3f);
            meshRenderer.material.color = highlightColor;
        }
    }

    public void OnLookExit()
    {
        isHighlighted = false;
        UpdateDoorAppearance();
    }

    public IEnumerator OnInteract()
    {
        if (isLocked)
        {
            // Play locked sound and shake
            if (lockedSound != null)
            {
                audioSource.PlayOneShot(lockedSound);
            }
            
            // Small shake effect
            yield return StartCoroutine(ShakeDoor());
            yield break;
        }
        
        if (isMoving) yield break;
        
        // Toggle door state
        if (isOpen)
        {
            yield return StartCoroutine(CloseDoor());
        }
        else
        {
            yield return StartCoroutine(OpenDoor());
        }
    }

    IEnumerator OpenDoor()
    {
        isMoving = true;
        isOpen = true;
        
        // Play open sound
        if (openSound != null)
        {
            audioSource.PlayOneShot(openSound);
        }
        
        // Animate door opening
        float elapsed = 0f;
        Quaternion startRotation = transform.rotation;
        
        while (elapsed < 1f / openSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (1f / openSpeed);
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth animation curve
            
            transform.rotation = Quaternion.Slerp(startRotation, openRotation, t);
            yield return null;
        }
        
        transform.rotation = openRotation;
        isMoving = false;
        
        // Auto close if enabled
        if (autoClose)
        {
            StartCoroutine(AutoCloseAfterDelay());
        }
    }

    IEnumerator CloseDoor()
    {
        isMoving = true;
        isOpen = false;
        
        // Play close sound
        if (closeSound != null)
        {
            audioSource.PlayOneShot(closeSound);
        }
        
        // Animate door closing
        float elapsed = 0f;
        Quaternion startRotation = transform.rotation;
        
        while (elapsed < 1f / openSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (1f / openSpeed);
            t = Mathf.SmoothStep(0f, 1f, t);
            
            transform.rotation = Quaternion.Slerp(startRotation, closedRotation, t);
            yield return null;
        }
        
        transform.rotation = closedRotation;
        isMoving = false;
    }

    IEnumerator AutoCloseAfterDelay()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        
        if (isOpen && !isMoving)
        {
            yield return StartCoroutine(CloseDoor());
        }
    }

    IEnumerator ShakeDoor()
    {
        Vector3 originalPosition = transform.position;
        float shakeDuration = 0.3f;
        float shakeIntensity = 0.05f;
        
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            
            Vector3 randomOffset = Random.insideUnitSphere * shakeIntensity;
            randomOffset.y = 0; // Don't shake vertically
            
            transform.position = originalPosition + randomOffset;
            yield return null;
        }
        
        transform.position = originalPosition;
    }

    public bool CanInteract()
    {
        return !isMoving && gameObject.activeInHierarchy;
    }

    public string GetInteractionPrompt()
    {
        if (isLocked)
        {
            return "Locked";
        }
        
        return isOpen ? "Press E to Close" : "Press E to Open";
    }

    // Public methods for external control
    public void SetLocked(bool locked)
    {
        isLocked = locked;
        UpdateDoorAppearance();
    }

    public bool IsOpen() => isOpen;
    public bool IsLocked() => isLocked;
    public bool IsMoving() => isMoving;
}