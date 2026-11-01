using TMPro;
using UnityEngine;

public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private TMP_Text label;

    private void OnEnable()
    {
        interactor.PromptChanged += OnPromptChanged;
        OnPromptChanged(interactor.CurrentPrompt);
    }

    private void OnDisable()
    {
        interactor.PromptChanged -= OnPromptChanged;
    }

    private void OnPromptChanged(string prompt)
    {
        label.text = prompt ?? string.Empty;
    }
}
