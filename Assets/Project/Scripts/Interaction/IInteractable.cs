using UnityEngine;

public interface IInteractable
{
    string GetPrompt(GameObject interactor);
    void Interact(GameObject interactor);
}
