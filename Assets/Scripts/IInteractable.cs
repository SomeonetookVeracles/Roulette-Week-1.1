using UnityEngine;
using System.Collections;

public interface IInteractable
{
    /// <summary>
    /// Called when player looks at the interactable object
    /// </summary>
    void OnLookEnter();
    
    /// <summary>
    /// Called when player stops looking at the interactable object
    /// </summary>
    void OnLookExit();
    
    /// <summary>
    /// Called when player presses the interaction key
    /// </summary>
    /// <returns>Coroutine for any animation/delay</returns>
    IEnumerator OnInteract();
    
    /// <summary>
    /// Returns whether this object can currently be interacted with
    /// </summary>
    /// <returns>True if interactable, false otherwise</returns>
    bool CanInteract();
    
    /// <summary>
    /// Returns the text to show when looking at this object
    /// </summary>
    /// <returns>Interaction prompt text</returns>
    string GetInteractionPrompt();
}