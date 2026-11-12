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

    public Rigidbody2D Body { get; private set; }

    /// <summary>Collider yang sedang dipijak (null kalau melayang).</summary>
    public Collider2D Support { get; private set; }

    public bool IsGrounded => Support != null;
    public float Mass => Body.mass;

    readonly RaycastHit2D[] hits = new RaycastHit2D[8];
    ContactFilter2D filter;

    void Awake()
    {
        Body = GetComponent<Rigidbody2D>();
        filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(groundMask);
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void FixedUpdate()
    {
        Support = null;

        // Cast seluruh collider milik Rigidbody ini sedikit ke bawah.
        // Collider milik objek ini sendiri otomatis diabaikan.
        int count = Body.Cast(Vector2.down, filter, hits, groundCheckDistance);

        float minNormalY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        float closest = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            if (hits[i].normal.y < minNormalY) continue; // dinding / terlalu miring
            if (hits[i].distance < closest)
            {
                closest = hits[i].distance;
                Support = hits[i].collider;
            }
        }
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