using UnityEngine;

/// <summary>
/// Area trigger di bawah peta (jurang, sungai, dll).
/// Jika karakter pemain masuk ke sini, level langsung gagal untuk seluruh tim.
/// Mendukung fisika 2D (Collider2D) maupun 3D (Collider).
/// </summary>
public class FallZone : MonoBehaviour
{
    [Tooltip("Tag yang dipakai semua karakter pemain.")]
    [SerializeField] string playerTag = "Player";
    [Tooltip("Alasan gagal, diteruskan ke LevelManager.onFailed (bisa dipakai untuk teks UI).")]
    [SerializeField] string reason = "Jatuh";

    // Otomatis centang "Is Trigger" saat komponen ditambahkan
    void Reset()
    {
        if (TryGetComponent(out Collider2D c2)) c2.isTrigger = true;
        if (TryGetComponent(out Collider c3)) c3.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        bool isPlayer = other.CompareTag(playerTag) || (rb != null && rb.CompareTag(playerTag));
        if (isPlayer) TriggerFail(rb != null ? rb.transform : other.transform);
    }

    void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        bool isPlayer = other.CompareTag(playerTag) || (rb != null && rb.CompareTag(playerTag));
        if (isPlayer) TriggerFail(rb != null ? rb.transform : other.transform);
    }

    void TriggerFail(Transform who)
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.Fail(who, reason);
        else
            Debug.LogWarning("[FallZone] Tidak ada LevelManager di scene.", this);
    }

    // Kotak merah transparan di Scene view supaya area jatuh mudah terlihat
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        if (TryGetComponent(out BoxCollider2D b2)) Gizmos.DrawCube(b2.offset, b2.size);
        else if (TryGetComponent(out BoxCollider b3)) Gizmos.DrawCube(b3.center, b3.size);
    }
}