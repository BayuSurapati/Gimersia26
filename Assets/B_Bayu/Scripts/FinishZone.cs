using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Area trigger "rumah". Level selesai jika SEMUA karakter berada di dalamnya
/// selama holdTime detik. Mendukung fisika 2D (Collider2D) maupun 3D (Collider).
/// Pasang tag Player di objek root karakter (yang punya Rigidbody), bukan di child-nya.
/// </summary>
public class FinishZone : MonoBehaviour
{
    [Tooltip("Tag yang dipakai semua karakter pemain.")]
    [SerializeField] string playerTag = "Player";
    [Tooltip("Jumlah karakter yang harus masuk. 0 = hitung otomatis semua objek bertag Player saat Start.")]
    [SerializeField] int requiredCount = 0;
    [Tooltip("Berapa lama (detik) semua karakter harus tetap di dalam sebelum level dianggap selesai.")]
    [SerializeField] float holdTime = 0.3f;

    [Header("Event (opsional)")]
    [Tooltip("Dipanggil setiap jumlah karakter di dalam berubah: (jumlah di dalam, jumlah dibutuhkan). " +
             "Cocok untuk feedback seperti lampu rumah menyala satu per satu.")]
    public UnityEvent<int, int> onCountChanged;

    // Satu karakter bisa punya beberapa collider, jadi yang dihitung per karakter, bukan per collider
    readonly Dictionary<Transform, int> inside = new Dictionary<Transform, int>();
    float holdTimer;
    bool finished;

    public int InsideCount => inside.Count;
    public int RequiredCount => requiredCount;

    void Reset()
    {
        if (TryGetComponent(out Collider2D c2)) c2.isTrigger = true;
        if (TryGetComponent(out Collider c3)) c3.isTrigger = true;
    }

    void Start()
    {
        if (requiredCount <= 0)
            requiredCount = GameObject.FindGameObjectsWithTag(playerTag).Length;

        if (requiredCount <= 0)
            Debug.LogWarning("[FinishZone] Tidak ada objek bertag " + playerTag + " di scene.", this);
    }

    void Update()
    {
        if (finished || requiredCount <= 0) return;
        if (LevelManager.Instance == null || LevelManager.Instance.CurrentState != LevelManager.State.Playing) return;

        if (inside.Count >= requiredCount)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= holdTime)
            {
                finished = true;
                LevelManager.Instance.Complete();
            }
        }
        else
        {
            holdTimer = 0f;
        }
    }

    // ---------------- 2D ----------------
    void OnTriggerEnter2D(Collider2D other) { if (IsPlayer(other.gameObject, other.attachedRigidbody)) Add(Key(other.transform, other.attachedRigidbody)); }
    void OnTriggerExit2D(Collider2D other) { if (IsPlayer(other.gameObject, other.attachedRigidbody)) Remove(Key(other.transform, other.attachedRigidbody)); }

    // ---------------- 3D ----------------
    void OnTriggerEnter(Collider other) { if (IsPlayer(other.gameObject, other.attachedRigidbody)) Add(Key(other.transform, other.attachedRigidbody)); }
    void OnTriggerExit(Collider other) { if (IsPlayer(other.gameObject, other.attachedRigidbody)) Remove(Key(other.transform, other.attachedRigidbody)); }

    bool IsPlayer(GameObject go, Component rb) =>
        go.CompareTag(playerTag) || (rb != null && rb.CompareTag(playerTag));

    static Transform Key(Transform colliderTransform, Component rb) =>
        rb != null ? rb.transform : colliderTransform;

    void Add(Transform who)
    {
        inside.TryGetValue(who, out int n);
        inside[who] = n + 1;
        if (n == 0) onCountChanged?.Invoke(inside.Count, requiredCount);
    }

    void Remove(Transform who)
    {
        if (!inside.TryGetValue(who, out int n)) return;
        if (n <= 1)
        {
            inside.Remove(who);
            onCountChanged?.Invoke(inside.Count, requiredCount);
        }
        else
        {
            inside[who] = n - 1;
        }
    }

    // Kotak hijau transparan di Scene view supaya area rumah mudah terlihat
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        if (TryGetComponent(out BoxCollider2D b2)) Gizmos.DrawCube(b2.offset, b2.size);
        else if (TryGetComponent(out BoxCollider b3)) Gizmos.DrawCube(b3.center, b3.size);
    }
}