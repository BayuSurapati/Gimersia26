using UnityEngine;

/// <summary>
/// Memutar animasi karakter secara otomatis berdasarkan apa yang sedang terjadi:
/// Idle, Walk, Jump (di udara), Land (sesaat setelah mendarat), Push (mendorong
/// kotak / kayu).
///
/// Script ini memanggil state Animator langsung lewat nama, jadi di Animator
/// Controller TIDAK perlu membuat transition atau parameter apa pun.
/// Cukup buat 5 state yang namanya sama dengan kolom "Nama state" di bawah.
///
/// Pasang di objek yang sama dengan CharacterController.
/// </summary>
[RequireComponent(typeof(CharacterAnimator), typeof(WeightBody))]
public class CharacterAnimator : MonoBehaviour
{
    [Tooltip("Kosongkan: otomatis dicari di objek ini atau anak-anaknya")]
    [SerializeField] Animator animator;

    [Header("Nama state di Animator Controller (harus sama persis)")]
    [SerializeField] string idleState = "Idle";
    [SerializeField] string walkState = "Walk";
    [SerializeField] string jumpState = "Jump";
    [SerializeField] string landState = "Land";
    [SerializeField] string pushState = "Push";

    [Header("Pengaturan")]
    [Tooltip("Lama animasi Land diputar (detik). Samakan dengan panjang clip Land")]
    [SerializeField] float landDuration = 0.2f;
    [Tooltip("Lama minimal di udara agar mendarat memutar Land (supaya turun anak tangga kecil tidak ikut)")]
    [SerializeField] float minAirTimeForLand = 0.15f;
    [Tooltip("Input minimal agar dianggap berjalan")]
    [SerializeField] float walkInputThreshold = 0.1f;
    [Tooltip("Kecepatan naik (relatif terhadap pijakan) yang dianggap sedang melompat")]
    [SerializeField] float jumpUpSpeed = 1.5f;

    [Header("Debug (lihat saat Play)")]
    [SerializeField] string debugState;

    CharacterController character;
    WeightBody weight;
    Rigidbody2D body;

    int currentHash;
    float airTime;
    float landUntil = -1f;

    static readonly ContactPoint2D[] contacts = new ContactPoint2D[8];

    void Awake()
    {
        character = GetComponent<CharacterController>();
        weight = GetComponent<WeightBody>();
        body = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError("CharacterAnimator: tidak menemukan Animator di objek ini atau anak-anaknya", this);
            enabled = false;
        }
    }

    void Update()
    {
        bool grounded = weight.IsGrounded && !IsRisingFromJump();

        // Hitung lama di udara, dan picu Land saat menyentuh pijakan
        if (!grounded)
        {
            airTime += Time.deltaTime;
        }
        else
        {
            if (airTime >= minAirTimeForLand) landUntil = Time.time + landDuration;
            airTime = 0f;
        }

        // Urutan prioritas animasi
        string next;
        if (!grounded) next = jumpState;
        else if (IsPushing()) next = pushState;
        else if (Time.time < landUntil) next = landState;
        else if (Mathf.Abs(character.MoveInput) > walkInputThreshold) next = walkState;
        else next = idleState;

        PlayState(next);
    }

    /// Baru saja melompat (masih menempel pijakan 1-2 frame, tapi sudah melaju ke atas)
    bool IsRisingFromJump()
    {
        float supportVy = 0f;
        Collider2D support = weight.Support;
        if (support != null && support.attachedRigidbody != null)
            supportVy = support.attachedRigidbody.GetPointVelocity(body.position).y;

        return body.linearVelocity.y - supportVy > jumpUpSpeed;
    }

    /// Menekan arah ke kotak / kayu yang menempel di depan karakter
    bool IsPushing()
    {
        float input = character.MoveInput;
        if (Mathf.Abs(input) < walkInputThreshold) return false;
        int dir = input > 0f ? 1 : -1;

        int count = body.GetContacts(contacts);
        for (int i = 0; i < count; i++)
        {
            ContactPoint2D c = contacts[i];
            if (Mathf.Abs(c.normal.x) < 0.5f) continue;                 // lantai / atap
            if ((c.point.x - body.position.x) * dir <= 0f) continue;    // bukan di depan

            Rigidbody2D other = c.rigidbody == body ? c.otherRigidbody : c.rigidbody;
            if (other == null) continue;

            if (other.GetComponent<PushableBox>() != null || other.GetComponent<FallingLog>() != null)
                return true;
        }
        return false;
    }

    void PlayState(string stateName)
    {
        int hash = Animator.StringToHash(stateName);
        if (hash == currentHash) return;

        if (!animator.HasState(0, hash))
        {
            Debug.LogWarning($"SheepAnimator: state \"{stateName}\" tidak ada di Animator Controller milik {animator.name}", this);
            currentHash = hash; // jangan spam warning
            return;
        }

        animator.Play(hash, 0, 0f);
        currentHash = hash;
        debugState = stateName;
    }
}