using System;
using UnityEngine;
using UnityEngine.AI;

public class LockedDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private string requiredKeyId = "llave_patio";
    [SerializeField] private string keyDisplayName = "la llave del patio";
    [SerializeField] private bool startsLocked = true;
    [SerializeField] private bool playerCanOpen = true;
    [SerializeField] private Transform hinge;
    [SerializeField] private float openAngle = 100f;
    [SerializeField] private float swingSpeed = 220f;
    [SerializeField] private NavMeshObstacle obstacle;

    private bool locked;
    private bool sealedShut;
    private float currentAngle;
    private float targetAngle;

    public bool IsOpen { get; private set; }

    public event Action Opened;
    public event Action Closed;
    public event Action Denied;

    private void Awake()
    {
        locked = startsLocked;
    }

    private void Update()
    {
        if (hinge == null || Mathf.Approximately(currentAngle, targetAngle)) return;
        currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, swingSpeed * Time.deltaTime);
        hinge.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
    }

    public string GetPrompt(GameObject interactor)
    {
        if (IsOpen) return null;
        if (sealedShut) return "Está trabada desde adentro";
        if (!playerCanOpen) return null;
        if (!locked) return "[E] Abrir";

        PlayerKeyring keyring = interactor.GetComponent<PlayerKeyring>();
        return keyring != null && keyring.Has(requiredKeyId)
            ? $"[E] Abrir con {keyDisplayName}"
            : $"Cerrada. Necesitás {keyDisplayName}";
    }

    public void Interact(GameObject interactor)
    {
        if (IsOpen || sealedShut || !playerCanOpen) return;

        if (locked)
        {
            PlayerKeyring keyring = interactor.GetComponent<PlayerKeyring>();
            if (keyring == null || !keyring.Has(requiredKeyId))
            {
                Denied?.Invoke();
                return;
            }
            locked = false;
        }

        Open();
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        sealedShut = false;
        targetAngle = openAngle;
        if (obstacle != null) obstacle.enabled = false;
        Opened?.Invoke();
    }

    public void CloseAndSeal()
    {
        IsOpen = false;
        sealedShut = true;
        targetAngle = 0f;
        if (obstacle != null) obstacle.enabled = true;
        Closed?.Invoke();
    }

    public void Unseal()
    {
        sealedShut = false;
        locked = false;
    }
}
