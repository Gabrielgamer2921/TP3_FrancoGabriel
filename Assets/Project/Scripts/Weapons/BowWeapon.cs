using System;
using System.Collections;
using UnityEngine;

public class BowWeapon : ProjectileWeapon
{
    [Header("Arco")]
    [SerializeField] private Transform nockPoint;
    [SerializeField] private Vector3 restNock = new Vector3(0f, 0f, -0.105f);
    [SerializeField] private Vector3 drawnNock = new Vector3(0f, 0f, -0.32f);
    [SerializeField] private Vector3 arrowRest = new Vector3(0.022f, 0f, 0f);
    [SerializeField] private Transform upperTip;
    [SerializeField] private Transform lowerTip;
    [SerializeField] private Transform upperString;
    [SerializeField] private Transform lowerString;
    [SerializeField] private GameObject loadedArrow;
    [SerializeField] private float drawTime = 0.7f;
    [SerializeField] private float relaxTime = 0.2f;
    [SerializeField] private float nockTime = 1.1f;
    [SerializeField] private float stringThickness = 0.004f;

    private PlayerWeaponController controller;
    private Coroutine nockRoutine;
    private bool hasArrow = true;
    private bool nocking;
    private float nockStart;

    public float Draw { get; private set; }
    public bool IsNocking => nocking;

    public event Action DrawStarted;
    public event Action NockStarted;
    public event Action NockFinished;

    public override float ViewLowerAmount => nocking ? Mathf.Sin(Mathf.Clamp01((Time.time - nockStart) / nockTime) * Mathf.PI) : 0f;

    protected override void OnInitialized()
    {
        controller = owner != null ? owner.GetComponent<PlayerWeaponController>() : null;
    }

    protected override bool CanShootNow() => hasArrow && !nocking && Draw >= 0.98f;

    protected override void OnShotFired()
    {
        hasArrow = false;
        Draw = 0f;
        if (loadedArrow != null) loadedArrow.SetActive(false);
        nocking = true;
        nockStart = Time.time;
        NockStarted?.Invoke();
        nockRoutine = StartCoroutine(NockRoutine());
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (nockRoutine != null) StopCoroutine(nockRoutine);
        nockRoutine = null;
        if (!hasArrow || nocking) FinishNock();
        Draw = 0f;
    }

    private void Update()
    {
        bool wantsDraw = controller != null && controller.CurrentWeapon == this && controller.IsAiming && hasArrow && !nocking;
        float previous = Draw;
        Draw = Mathf.MoveTowards(Draw, wantsDraw ? 1f : 0f, Time.deltaTime / (wantsDraw ? drawTime : relaxTime));
        if (previous <= 0f && Draw > 0f) DrawStarted?.Invoke();
    }

    private void LateUpdate()
    {
        if (nockPoint == null) return;
        Vector3 nock = Vector3.Lerp(restNock, drawnNock, Mathf.SmoothStep(0f, 1f, Draw));
        nockPoint.localPosition = nock;
        Vector3 toRest = arrowRest - nock;
        if (toRest.sqrMagnitude > 0.0001f) nockPoint.localRotation = Quaternion.LookRotation(toRest, Vector3.up);
        if (upperTip != null) PlaceString(upperString, upperTip.localPosition, nockPoint.localPosition);
        if (lowerTip != null) PlaceString(lowerString, lowerTip.localPosition, nockPoint.localPosition);
    }

    private void PlaceString(Transform segment, Vector3 from, Vector3 to)
    {
        if (segment == null) return;
        Vector3 delta = to - from;
        segment.localPosition = (from + to) * 0.5f;
        if (delta.sqrMagnitude > 0.000001f) segment.localRotation = Quaternion.LookRotation(delta, Vector3.forward);
        segment.localScale = new Vector3(stringThickness, stringThickness, delta.magnitude);
    }

    private IEnumerator NockRoutine()
    {
        yield return new WaitForSeconds(nockTime * 0.55f);
        if (loadedArrow != null) loadedArrow.SetActive(true);
        hasArrow = true;
        AddToMagazine(data.magazineSize);
        yield return new WaitForSeconds(nockTime * 0.45f);
        nocking = false;
        nockRoutine = null;
        NockFinished?.Invoke();
    }

    private void FinishNock()
    {
        hasArrow = true;
        nocking = false;
        if (loadedArrow != null) loadedArrow.SetActive(true);
        AddToMagazine(data.magazineSize);
    }
}
