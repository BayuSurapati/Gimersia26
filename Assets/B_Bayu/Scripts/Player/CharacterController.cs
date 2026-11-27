using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Kontrol satu karakter: jalan, lompat, dan ikut terbawa pijakan yang bergerak.
/// Input dibaca dari Input Actions (GameControls), jadi keyboard dan gamepad
/// sama-sama bisa dipakai tanpa kode tambahan.
/// Hanya karakter yang IsActive = true yang membaca input (nanti diatur SheepSwitcher).
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(WeightBody))]
public class CharacterController : MonoBehaviour
{
    [Header("Identitas")]
    public string displayName = "Domba";

    [Header("Input (dari GameControls)")]
    [Tooltip("Pilih Player/Move")]
    [SerializeField] InputActionReference moveAction;
    [Tooltip("Pilih Player/Jump")]
    [SerializeField] InputActionReference jumpAction;

    [Header("Gerak")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float acceleration = 60f;
    [SerializeField] float jumpVelocity = 9f;

    [Tooltip("Kekuatan dorong. Dibandingkan dengan PushableBox.requiredStrength")]
    public int strength = 2;

    [Header("Rasa lompat")]
    [Tooltip("Masih boleh lompat sesaat setelah lepas dari pijakan")]
    [SerializeField] float coyoteTime = 0.1f;
    [Tooltip("Tombol lompat yang ditekan sedikit sebelum mendarat tetap dihitung")]
    [SerializeField] float jumpBuffer = 0.12f;
    [Tooltip("Jatuh lebih cepat daripada naik, supaya lompatan terasa mantap")]
    [SerializeField] float fallGravityMultiplier = 1.8f;
    [Tooltip("Sesaat setelah lompat, pijakan diabaikan (mencegah lompat dobel karena sensor masih menyentuh lantai)")]
    [SerializeField] float groundIgnoreAfterJump = 0.1f;

    [Header("Debug lompat (lihat saat Play)")]
    [SerializeField] bool debugGrounded;
    [SerializeField] bool debugCanJump;

    [Header("Visual (opsional)")]
    [SerializeField] SpriteRenderer spriteRenderer;

    [Header("Testing")]
    [Tooltip("Centang untuk mencoba satu karakter tanpa SheepSwitcher. Nanti SheepSwitcher akan menimpanya.")]
    [SerializeField] bool activeOnStart = false;

    public bool IsActive { get; set; }
    public Rigidbody2D Body { get; private set; }
    public WeightBody Weight { get; private set; }
    public int FacingDirection { get; private set; } = 1;

    /// <summary>Input kiri/kanan saat ini (-1..1). Dipakai SwingPlatform untuk "memompa" ayunan.</summary>
    public float MoveInput => inputX;

    float inputX;
    float lastGroundedTime = -10f;
    float lastJumpPressedTime = -10f;
    float lastJumpTime = -10f;
    float baseGravity;

    void Awake()
    {
        Body = GetComponent<Rigidbody2D>();
        Weight = GetComponent<WeightBody>();
        baseGravity = Body.gravityScale;
        Body.freezeRotation = true;
        IsActive = activeOnStart; // Awake berjalan sebelum Start milik SheepSwitcher
    }

    void OnEnable()
    {
        // Action dari asset biasa harus di-Enable dulu sebelum bisa dibaca.
        // Aman dipanggil berkali-kali walau ketiga karakter memakai action yang sama.
        if (moveAction != null) moveAction.action.Enable();
        if (jumpAction != null) jumpAction.action.Enable();
    }

    void Update()
    {
        inputX = 0f;
        if (!IsActive) return;

        if (moveAction != null)
        {
            // Mendukung Move bertipe Vector2 (2D composite / stik utuh) maupun float (1D Axis)
            InputAction move = moveAction.action;
            inputX = move.activeValueType == typeof(Vector2)
                ? move.ReadValue<Vector2>().x
                : move.ReadValue<float>();
            inputX = Mathf.Clamp(inputX, -1f, 1f);
        }

        if (jumpAction != null && jumpAction.action.WasPressedThisFrame())
            lastJumpPressedTime = Time.time;

        if (Mathf.Abs(inputX) > 0.1f)
        {
            FacingDirection = inputX > 0f ? 1 : -1;
            if (spriteRenderer) spriteRenderer.flipX = FacingDirection < 0;
        }
    }

    void FixedUpdate()
    {
        bool grounded = Weight.IsGrounded && Time.time - lastJumpTime > groundIgnoreAfterJump;
        if (grounded) lastGroundedTime = Time.time;

        // Kecepatan pijakan (karakter lain, platform bergerak, ayunan, jungkat-jungkit)
        Vector2 supportVel = Vector2.zero;
        if (grounded && Weight.Support != null && Weight.Support.attachedRigidbody != null)
            supportVel = Weight.Support.attachedRigidbody.GetPointVelocity(Body.position);

        Vector2 v = Body.linearVelocity;

        float targetX = inputX * moveSpeed + supportVel.x;
        v.x = Mathf.MoveTowards(v.x, targetX, acceleration * Time.fixedDeltaTime);

        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        bool wantsJump = Time.time - lastJumpPressedTime <= jumpBuffer;
        if (canJump && wantsJump)
        {
            // Lompat relatif terhadap pijakan yang sedang naik (lift / ayunan),
            // supaya lompatan dari pijakan bergerak tidak terasa "tertahan"
            v.y = jumpVelocity + Mathf.Max(0f, supportVel.y);
            lastJumpPressedTime = -10f;
            lastGroundedTime = -10f;
            lastJumpTime = Time.time;
        }

        debugGrounded = grounded;
        debugCanJump = canJump;

        Body.linearVelocity = v;
        Body.gravityScale = v.y < 0f ? baseGravity * fallGravityMultiplier : baseGravity;
    }
}