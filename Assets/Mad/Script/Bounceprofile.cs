using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BounceProfile : MonoBehaviour
{
    [Header("Pantulan")]
    [Tooltip("Kecepatan lontar pada pantulan pertama (unit/detik).")]
    [SerializeField] private float initialBounceVelocity = 6f;
    [Tooltip("Tambahan kecepatan pada setiap pantulan berantai.")]
    [SerializeField] private float growthPerBounce = 1f;
    [Tooltip("Batas maksimum kecepatan lontar.")]
    [SerializeField] private float maxBounceVelocity = 16f;

    [Header("Pantulan berantai")]
    [Tooltip("true = makin lama memantul di trampolin, makin tinggi terbang.")]
    [SerializeField] private bool chainEnabled = true;

    [Tooltip("Seberapa 'di atas' titik sentuh pusat hewan harus berada agar dianggap mendarat (bukan menabrak dinding).")]
    [SerializeField] private float landThreshold = 0.1f;

    public float InitialBounceVelocity => initialBounceVelocity;
    public float GrowthPerBounce => growthPerBounce;
    public float MaxBounceVelocity => maxBounceVelocity;

    /// <summary>Jumlah pantulan berturut-turut sejauh ini.</summary>
    public int ChainCount { get; private set; }

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void RegisterBounce()
    {
        if (chainEnabled) ChainCount++;
    }

    public void ResetChain()
    {
        ChainCount = 0;
    }

    // Mendarat di tanah/benda lain (bukan trampolin) = rantai pantulan putus
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (ChainCount == 0) return;
        if (collision.collider.GetComponentInParent<Trampoline>() != null) return;

        float height = rb.worldCenterOfMass.y - collision.GetContact(0).point.y;
        if (height > landThreshold) ResetChain();
    }
}