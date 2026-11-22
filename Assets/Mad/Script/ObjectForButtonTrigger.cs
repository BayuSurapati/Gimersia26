using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Pasang di objek apa saja yang mau dibuka/tutup (pintu, gerbang, platform, dll).
/// Pilih mode di Inspector:
///   Move   = bergeser sejauh "Open Offset"
///   Rotate = berputar sebesar "Open Rotation" (opsional mengelilingi titik pivot)
/// Panggil Open(), Close(), atau Toggle() dari UnityEvent (misalnya ButtonSwitch).
///
/// Kalau objek punya Rigidbody2D, pakai Body Type = Kinematic supaya hewan
/// yang berdiri di atasnya ikut bergerak dengan benar.
/// </summary>
public class OpenableObject : MonoBehaviour
{
    public enum OpenMode { Move, Rotate }
    public enum OffsetSpace { Local, World }

    [Header("Mode")]
    [SerializeField] private OpenMode mode = OpenMode.Move;

    [Header("Mode Move")]
    [Tooltip("Geser ke mana saat terbuka. Contoh: (0, 3, 0) = naik 3 unit.")]
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 3f, 0f);
    [Tooltip("Local = mengikuti rotasi objek. World = arah mutlak di scene.")]
    [SerializeField] private OffsetSpace offsetSpace = OffsetSpace.World;
    [FormerlySerializedAs("speed")]
    [Tooltip("Kecepatan geser (unit/detik).")]
    [SerializeField] private float moveSpeed = 4f;

    [Header("Mode Rotate")]
    [Tooltip("Besar putaran saat terbuka (derajat). Untuk 2D, isi di Z. Contoh: (0, 0, 90). Nilai negatif = arah sebaliknya.")]
    [SerializeField] private Vector3 openRotation = new Vector3(0f, 0f, 90f);
    [Tooltip("Titik poros putaran. Kosongkan = berputar di titik pivot objek sendiri.")]
    [SerializeField] private Transform rotationPivot;
    [Tooltip("Kecepatan putar (derajat/detik).")]
    [SerializeField] private float rotateSpeed = 90f;

    [Header("Status awal")]
    [SerializeField] private bool startOpen = false;

    public bool IsOpen { get; private set; }

    private Rigidbody2D rb;
    private Vector3 closedPos;
    private Quaternion closedRot;
    private Vector3 worldOffset;
    private Vector3 pivotPoint;
    private float t;          // 0 = tertutup, 1 = terbuka
    private float targetT;
    private float tSpeed;     // kecepatan perubahan t per detik

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Pose di scene dianggap pose "tertutup"
        closedPos = transform.position;
        closedRot = transform.rotation;
        worldOffset = offsetSpace == OffsetSpace.Local
            ? transform.TransformVector(openOffset)
            : openOffset;
        pivotPoint = rotationPivot != null ? rotationPivot.position : closedPos;

        float total = mode == OpenMode.Move ? worldOffset.magnitude : MaxAbs(openRotation);
        float speed = mode == OpenMode.Move ? moveSpeed : rotateSpeed;
        tSpeed = total > 0.0001f ? speed / total : 1000f;

        IsOpen = startOpen;
        t = targetT = startOpen ? 1f : 0f;
        Apply(instant: true);
    }

    private void Update()
    {
        if (rb == null) Step(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (rb != null) Step(Time.fixedDeltaTime);
    }

    private void Step(float dt)
    {
        if (t == targetT) return;
        t = Mathf.MoveTowards(t, targetT, tSpeed * dt);
        Apply(instant: false);
    }

    private void Apply(bool instant)
    {
        Vector3 pos;
        Quaternion rot;

        if (mode == OpenMode.Move)
        {
            pos = closedPos + worldOffset * t;
            rot = closedRot;
        }
        else
        {
            Quaternion d = Quaternion.Euler(openRotation * t);
            pos = pivotPoint + d * (closedPos - pivotPoint);
            rot = d * closedRot;
        }

        if (rb != null && !instant)
        {
            rb.MovePosition(pos);
            rb.MoveRotation(rot.eulerAngles.z);
        }
        else
        {
            transform.SetPositionAndRotation(pos, rot);
        }
    }

    private static float MaxAbs(Vector3 v)
    {
        return Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    private void SetState(bool open)
    {
        IsOpen = open;
        targetT = open ? 1f : 0f;
    }

    public void Open() => SetState(true);
    public void Close() => SetState(false);
    public void Toggle() => SetState(!IsOpen);

    // Pratinjau di Scene view saat objek dipilih
    private void OnDrawGizmosSelected()
    {
        bool play = Application.isPlaying;
        Vector3 basePos = play ? closedPos : transform.position;

        if (mode == OpenMode.Move)
        {
            Vector3 off = play
                ? worldOffset
                : (offsetSpace == OffsetSpace.Local ? transform.TransformVector(openOffset) : openOffset);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(basePos, basePos + off);
            Gizmos.DrawWireSphere(basePos + off, 0.15f);
        }
        else
        {
            Vector3 pivot = play
                ? pivotPoint
                : (rotationPivot != null ? rotationPivot.position : basePos);
            Vector3 to = pivot + Quaternion.Euler(openRotation) * (basePos - pivot);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pivot, 0.12f);
            Gizmos.color = Color.gray;
            Gizmos.DrawLine(pivot, basePos);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pivot, to);
            Gizmos.DrawWireSphere(to, 0.15f);
        }
    }
}