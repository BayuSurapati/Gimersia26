using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dipasang di semua benda yang punya berat dan bisa ditumpuk (domba, kotak).
/// Tugasnya: mendeteksi "pijakan" di bawahnya, lalu menghitung total berat
/// yang menekan sebuah collider (dipakai PressurePlate dan BreakableFloor).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(-50)] // jalan SEBELUM SheepController, supaya data pijakan selalu terbaru
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

    [Tooltip("Sudut maksimum saat berdiri di atas KARAKTER LAIN atau KOTAK. Dibuat longgar " +
             "supaya karakter yang menumpang di kepala bentuk tidak rata tetap bisa lompat")]
    [SerializeField, Range(0f, 89f)] float maxStackAngle = 75f;

    [Tooltip("Lebar sensor kaki dipersempit sebanyak ini di kiri & kanan, " +
             "supaya dinding/kotak/karakter di samping tidak dianggap pijakan")]
    [SerializeField] float sideInset = 0.1f;

    public Rigidbody2D Body { get; private set; }

    /// <summary>Collider yang sedang dipijak (null kalau melayang).</summary>
    public Collider2D Support { get; private set; }

    /// <summary>
    /// SEMUA collider yang tersentuh sensor kaki (bisa lebih dari satu, misalnya
    /// lantai + pressure plate yang ditanam tepat di bawah permukaan lantai).
    /// </summary>
    public IReadOnlyList<Collider2D> Supports => supports;

    public bool IsGrounded => Support != null;
    public float Mass => Body.mass;

    const float ProbeHeight = 0.1f;
    readonly List<Collider2D> supports = new List<Collider2D>();

    readonly RaycastHit2D[] hits = new RaycastHit2D[16];
    static readonly ContactPoint2D[] contacts = new ContactPoint2D[16];
    readonly List<Collider2D> ownColliders = new List<Collider2D>();
    ContactFilter2D filter;

    void Awake()
    {
        Body = GetComponent<Rigidbody2D>();
        filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(groundMask);
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    /// Gabungan bounds semua collider (non-trigger) milik Rigidbody ini,
    /// termasuk collider yang ada di objek anak.
    bool TryGetBodyBounds(out Bounds bounds)
    {
        bounds = default;
        Body.GetAttachedColliders(ownColliders);
        bool found = false;
        foreach (Collider2D c in ownColliders)
        {
            if (c == null || c.isTrigger || !c.enabled) continue;
            if (!found) { bounds = c.bounds; found = true; }
            else bounds.Encapsulate(c.bounds);
        }
        return found;
    }

    void FixedUpdate()
    {
        Support = null;
        supports.Clear();
        if (!TryGetBodyBounds(out Bounds b)) return;

        float minGroundY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        float minStackY = Mathf.Cos(maxStackAngle * Mathf.Deg2Rad);
        float closest = float.MaxValue;

        // 1. Sensor kaki: kotak tipis di bagian bawah badan, sedikit lebih SEMPIT
        //    dari badan, lalu digeser ke bawah. Karena lebih sempit, benda yang hanya
        //    menempel di samping (dinding, kotak, karakter lain) tidak ikut terdeteksi.
        Vector2 size = new Vector2(Mathf.Max(0.05f, b.size.x - 2f * sideInset), ProbeHeight);
        Vector2 origin = new Vector2(b.center.x, b.min.y + ProbeHeight * 0.5f);
        int count = Physics2D.BoxCast(origin, size, 0f, Vector2.down, filter, hits, groundCheckDistance);

        for (int i = 0; i < count; i++)
        {
            if (hits[i].rigidbody == Body) continue; // collider milik sendiri

            float minY = IsStackable(hits[i].collider) ? minStackY : minGroundY;
            if (hits[i].normal.y < minY) continue;    // dinding / terlalu miring

            AddSupport(hits[i].collider, hits[i].distance, ref closest);
        }

        // 2. Cadangan: sentuhan fisika nyata di bagian kaki.
        //    Menangani karakter yang "terjepit" di atas karakter lain / bentuk collider tidak rata.
        if (Support == null)
        {
            int n = Body.GetContacts(contacts);
            for (int i = 0; i < n; i++)
            {
                ContactPoint2D c = contacts[i];
                Collider2D other = c.collider != null && c.collider.attachedRigidbody == Body ? c.otherCollider : c.collider;
                if (other == null || other.isTrigger) continue;
                if (((1 << other.gameObject.layer) & groundMask.value) == 0) continue;

                float minY = IsStackable(other) ? minStackY : minGroundY;
                if (Mathf.Abs(c.normal.y) < minY) continue;       // sentuhan samping
                if (c.point.y > b.min.y + ProbeHeight) continue;   // bukan di kaki

                AddSupport(other, 0f, ref closest);
            }
        }
    }

    void AddSupport(Collider2D col, float distance, ref float closest)
    {
        if (!supports.Contains(col)) supports.Add(col);
        if (distance < closest)
        {
            closest = distance;
            Support = col;
        }
    }

    /// Karakter lain / kotak (benda yang juga punya WeightBody)
    static bool IsStackable(Collider2D col)
    {
        Rigidbody2D rb = col.attachedRigidbody;
        return rb != null && rb.GetComponent<WeightBody>() != null;
    }

    // Gambar sensor kaki di Scene view saat objek dipilih (hijau = berpijak)
    void OnDrawGizmosSelected()
    {
        if (Body == null) Body = GetComponent<Rigidbody2D>();
        if (Body == null || !TryGetBodyBounds(out Bounds b)) return;

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
        if (maxDepth <= 0) return false;

        for (int i = 0; i < supports.Count; i++)
        {
            Collider2D c = supports[i];
            if (c == target) return true;

            // Pijakan ini benda yang juga punya berat? Telusuri ke bawahnya.
            Rigidbody2D rb = c.attachedRigidbody;
            if (rb == null) continue;

            WeightBody below = rb.GetComponent<WeightBody>();
            if (below == null || below == this) continue;

            if (below.IsSupportedBy(target, maxDepth - 1)) return true;
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