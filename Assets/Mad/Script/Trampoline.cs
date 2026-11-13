using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class Trampoline : MonoBehaviour
{
    [Header("Pantulan")]
    [Tooltip("Kecepatan lontar pantulan pertama (unit/detik).")]
    [SerializeField] private float bounceVelocity = 14f;
    [Tooltip("Tambahan kecepatan tiap pantulan berturut-turut.")]
    [SerializeField] private float growthPerBounce = 2f;
    [Tooltip("Batas kecepatan lontar (sebelum dikali pengali hewan).")]
    [SerializeField] private float maxBounceVelocity = 28f;
    [Tooltip("true = kecepatan menyamping (kiri/kanan) tetap dipertahankan.")]
    [SerializeField] private bool keepSidewaysVelocity = true;
    [Tooltip("Seberapa 'di atas' titik sentuh pusat objek harus berada agar memantul.")]
    [SerializeField] private float topHitThreshold = 0.1f;

    [Header("Visual (opsional)")]
    [Tooltip("Sprite yang gepeng saat dipijak. Kosongkan = objek ini sendiri. Pivot sprite sebaiknya di bawah.")]
    [SerializeField] private Transform visual;
    [SerializeField] private float squashAmount = 0.4f;
    [SerializeField] private float springStiffness = 250f;
    [SerializeField] private float springDamping = 12f;

    [Header("Event")]
    [Tooltip("Dipanggil tiap kali ada yang terpental. Isi dengan suara/partikel.")]
    public UnityEvent OnBounce;

    private Vector3 baseScale;
    private float squash;
    private float squashVelocity;

    private void Reset()
    {
        visual = transform;
    }

    private void Awake()
    {
        if (visual == null) visual = transform;
        baseScale = visual.localScale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Rigidbody2D rb = collision.rigidbody;
        if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic) return;

        Vector2 up = transform.up;
        ContactPoint2D contact = collision.GetContact(0);
        float height = Vector2.Dot(rb.worldCenterOfMass - contact.point, up);
        if (height < topHitThreshold) return;

        Launch(rb, up);
    }

    private void Launch(Rigidbody2D rb, Vector2 up)
    {
        // Tanpa BounceProfile: pantulan normal, tanpa efek makin tinggi
        BounceProfile profile = rb.GetComponent<BounceProfile>();
        float multiplier = profile != null ? profile.BounceMultiplier : 1f;
        int chain = profile != null ? profile.ChainCount : 0;

        float speed = Mathf.Min(bounceVelocity + chain * growthPerBounce, maxBounceVelocity) * multiplier;

        Vector2 v = rb.linearVelocity;
        Vector2 sideways = v - up * Vector2.Dot(v, up);
        rb.linearVelocity = (keepSidewaysVelocity ? sideways : Vector2.zero) + up * speed;

        if (profile != null) profile.RegisterBounce();

        squashVelocity = 6f;
        OnBounce.Invoke();
    }

    private void Update()
    {
        if (visual == null) return;

        float dt = Time.deltaTime;
        squashVelocity += (-squash * springStiffness - squashVelocity * springDamping) * dt;
        squash += squashVelocity * dt;

        float s = squash * squashAmount;
        visual.localScale = new Vector3(baseScale.x * (1f + s * 0.5f), baseScale.y * (1f - s), baseScale.z);
    }
}