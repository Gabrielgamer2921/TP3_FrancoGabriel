using System;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float range = 2.6f;
    [SerializeField] private LayerMask mask = ~0;

    private IInteractable current;
    private string currentPrompt;

    public string CurrentPrompt => currentPrompt;

    public event Action<string> PromptChanged;

    private void OnEnable() => input.InteractPressed += OnInteract;
    private void OnDisable() => input.InteractPressed -= OnInteract;

    private void Update()
    {
        IInteractable found = null;
        Transform cam = playerCamera.transform;

        if (LineOfSight.TryGetFirstHit(cam.position, cam.forward, range, transform, mask, out RaycastHit hit))
            found = hit.collider.GetComponentInParent<IInteractable>();

        current = found;
        string prompt = found != null ? found.GetPrompt(gameObject) : null;
        if (prompt == currentPrompt) return;

        currentPrompt = prompt;
        PromptChanged?.Invoke(prompt);
    }

    private void OnInteract()
    {
        current?.Interact(gameObject);
    }
}
