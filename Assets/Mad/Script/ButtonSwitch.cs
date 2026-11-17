using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ButtonSwitch : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Child yang punya sprite (objek 'Trigger').")]
    [SerializeField] private Transform visual;
    [Tooltip("Seberapa jauh tombol turun (posisi lokal).")]
    [SerializeField] private Vector3 pressedOffset = new Vector3(0f, -0.15f, 0f);
    [SerializeField] private float moveSpeed = 3f;

    [Header("Deteksi")]
    [Tooltip("Drag collider trigger ke sini. Kosongkan = otomatis cari semua collider trigger di child.")]
    [SerializeField] private Collider2D[] triggerColliders;
    [Tooltip("Layer yang bisa menekan. Default: semua.")]
    [SerializeField] private LayerMask detectLayers = ~0;
    [SerializeField] private bool ignoreOtherTriggers = true;

    [Header("Perilaku")]
    [Tooltip("true = naik lagi saat dilepas. false = sekali tekan tetap turun.")]
    [SerializeField] private bool releaseWhenEmpty = true;

    [Header("Event")]
    public UnityEvent OnPressed;
    public UnityEvent OnReleased;

    private readonly HashSet<Collider2D> touching = new HashSet<Collider2D>();
    private Vector3 upPos;
    private Vector3 downPos;
    private bool isPressed;

    private void Reset()
    {
        if (transform.childCount > 0) visual = transform.GetChild(0);
        var col = GetComponentInChildren<Collider2D>();
        if (col != null) triggerColliders = new[] { col };
    }

    private void Awake()
    {
        if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
        if (visual != null)
        {
            upPos = visual.localPosition;
            downPos = upPos + pressedOffset;
        }

        // Pasang penerus event ke setiap collider trigger (di induk maupun child)
        Collider2D[] cols = (triggerColliders != null && triggerColliders.Length > 0)
            ? triggerColliders
            : GetComponentsInChildren<Collider2D>(true);

        foreach (var col in cols)
        {
            if (col == null || !col.isTrigger) continue;
            var relay = col.GetComponent<ButtonTriggerRelay>();
            if (relay == null) relay = col.gameObject.AddComponent<ButtonTriggerRelay>();
            relay.owner = this;
        }
    }

    private bool IsValid(Collider2D other)
    {
        if (other.transform.IsChildOf(transform)) return false;          // abaikan tombol sendiri
        if (ignoreOtherTriggers && other.isTrigger) return false;
        return (detectLayers.value & (1 << other.gameObject.layer)) != 0;
    }

    public void HandleEnter(Collider2D other)
    {
        if (IsValid(other)) touching.Add(other);
    }

    public void HandleExit(Collider2D other)
    {
        touching.Remove(other);
    }

    private void Update()
    {
        // Buang objek yang sudah dihancurkan/dinonaktifkan (Exit tidak selalu terpanggil)
        touching.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);

        bool shouldPress = touching.Count > 0;

        if (shouldPress && !isPressed)
        {
            isPressed = true;
            Debug.Log($"Tombol '{name}' diinjak.", this);
            OnPressed.Invoke();
        }
        else if (!shouldPress && isPressed && releaseWhenEmpty)
        {
            isPressed = false;
            OnReleased.Invoke();
        }

        if (visual != null)
        {
            Vector3 target = isPressed ? downPos : upPos;
            visual.localPosition = Vector3.MoveTowards(visual.localPosition, target, moveSpeed * Time.deltaTime);
        }
    }
}

/// <summary>Dipasang otomatis oleh ButtonSwitch pada collider trigger.</summary>
public class ButtonTriggerRelay : MonoBehaviour
{
    [HideInInspector] public ButtonSwitch owner;

    private void OnTriggerEnter2D(Collider2D other) { if (owner != null) owner.HandleEnter(other); }
    private void OnTriggerExit2D(Collider2D other) { if (owner != null) owner.HandleExit(other); }
}