using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dipasang di semua benda yang punya berat dan bisa ditumpuk (domba, kotak).
/// Tugasnya: mendeteksi "pijakan" di bawahnya, lalu menghitung total berat
/// yang menekan sebuah collider (dipakai PressurePlate dan BreakableFloor).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class WeightBody : MonoBehaviour
{
    // Daftar semua WeightBody aktif di scene
    public static readonly List<WeightBody> All = new List<WeightBody>();

    [Tooltip("Jarak cek ke bawah untuk mendeteksi pijakan")]
    [SerializeField] float groundCheckDistance = 0.06f;

    [Tooltip("Layer apa saja yang bisa dijadikan pijakan")]
    [SerializeField] LayerMask groundMask = ~0;

    [Tooltip("Sudut maksimum permukaan yang masih dianggap lantai")]
    [SerializeField, Range(0f, 89f)] float maxGroundAngle = 50f;

    [Tooltip("Lebar sensor kaki dipersempit sebanyak ini di kiri & kanan, " +
             "supaya dinding/kotak/karakter di samping tidak dianggap pijakan")]
    [SerializeField] float sideInset = 0.1f;

    public Rigidbody2D Body { get; private set; }

    /// <summary>Collider yang sedang dipijak (null kalau melayang).</summary>
    public Collider2D Support { get; private set; }

    public bool IsGrounded => Support != null;
    public float Mass => Body.mass;

    const float ProbeHeight = 0.1f;

    readonly RaycastHit2D[] hits = new RaycastHit2D[8];
    ContactFilter2D filter;
    Collider2D mainCollider;

    void Awake()
    {
        Body = GetComponent<Rigidbody2D>();
        mainCollider = GetComponent<Collider2D>();
        filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(groundMask);
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void FixedUpdate()
    {
        Support = null;
        if (mainCollider == null) return;

        // Sensor kaki: kotak tipis di bagian bawah collider, sedikit lebih SEMPIT
        // dari badan, lalu digeser ke bawah. Karena lebih sempit, benda yang hanya
        // menempel di samping (dinding, kotak, karakter lain) tidak ikut terdeteksi.
        Bounds b = mainCollider.bounds;
        Vector2 size = new Vector2(Mathf.Max(0.05f, b.size.x - 2f * sideInset), ProbeHeight);
        Vector2 origin = new Vector2(b.center.x, b.min.y + ProbeHeight * 0.5f);

        int count = Physics2D.BoxCast(origin, size, 0f, Vector2.down, filter, hits, groundCheckDistance);

        float minNormalY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        float closest = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            if (hits[i].rigidbody == Body) continue;     // collider milik sendiri
            if (hits[i].normal.y < minNormalY) continue; // dinding / terlalu miring
            if (hits[i].distance < closest)
            {
                closest = hits[i].distance;
                Support = hits[i].collider;
            }
        }
    }

    // Gambar sensor kaki di Scene view saat objek dipilih (hijau = berpijak)
    void OnDrawGizmosSelected()
    {
        Collider2D col = mainCollider != null ? mainCollider : GetComponent<Collider2D>();
        if (col == null) return;

        Bounds b = col.bounds;
        Vector3 size = new Vector3(Mathf.Max(0.05f, b.size.x - 2f * sideInset), ProbeHeight + groundCheckDistance, 0f);
        Vector3 center = new Vector3(b.center.x, b.min.y + (ProbeHeight - groundCheckDistance) * 0.5f, 0f);

        Gizmos.color = Application.isPlaying && IsGrounded ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(center, size);
    }

    /// <summary>
    /// True kalau objek ini (langsung atau lewat tumpukan) bertumpu pada target.
    /// Contoh: domba kecil di atas domba besar yang berdiri di atas plate
    /// => domba kecil juga dihitung menekan plate.
    /// </summary>
    public bool IsSupportedBy(Collider2D target, int maxDepth = 10)
    {
        Collider2D current = Support;
        for (int depth = 0; depth < maxDepth && current != null; depth++)
        {
            if (current == target) return true;

            Rigidbody2D rb = current.attachedRigidbody;
            if (rb == null) return false;

            WeightBody below = rb.GetComponent<WeightBody>();
            if (below == null || below == this) return false;

            current = below.Support;
        }
        return false;
    }

    /// <summary>Total massa semua WeightBody yang menekan collider target.</summary>
    public static float MassOn(Collider2D target)
    {
        float total = 0f;
        for (int i = 0; i < All.Count; i++)
        {
            if (All[i].IsSupportedBy(target)) total += All[i].Mass;
        }
        return total;
    }
}