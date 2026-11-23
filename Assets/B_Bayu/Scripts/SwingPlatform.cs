using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ayunan: papan yang digantung dua tali, papan selalu datar, berayun kiri-kanan.
///
/// - Hanya bergerak kalau karakter yang SEDANG DIKONTROL berdiri di atasnya dan
///   menekan kiri/kanan ("memompa"). Searah gerakan = menambah ayunan,
///   berlawanan arah = mengerem.
/// - Opsional (Mass Affects Swing): makin berat total penumpang, makin susah
///   dipompa, tapi ayunannya lebih lama berhenti. Kalau dimatikan, semua
///   karakter mengayun sama cepat.
/// - Ayunan cepat melambat sendiri kalau tidak dipompa.
///
/// Pasang di GameObject induk "Swing" yang posisinya = titik tengah antara
/// dua titik gantung (di atas). Papan dihitung & digerakkan oleh script ini.
/// </summary>
public class SwingPlatform : MonoBehaviour
{
    [Header("Bagian-bagian")]
    [Tooltip("Objek papan: punya Rigidbody2D (Kinematic) + collider")]
    [SerializeField] Rigidbody2D plank;
    [Tooltip("Sprite tali kiri (opsional, tanpa collider)")]
    [SerializeField] Transform ropeLeft;
    [Tooltip("Sprite tali kanan (opsional, tanpa collider)")]
    [SerializeField] Transform ropeRight;

    [Header("Bentuk")]
    [Tooltip("Panjang tali (jarak titik gantung ke papan)")]
    [SerializeField] float ropeLength = 3f;
    [Tooltip("Jarak tali dari tengah papan (biasanya setengah lebar papan)")]
    [SerializeField] float ropeHalfSpan = 1.4f;
    [SerializeField] float ropeWidth = 0.06f;
    [Tooltip("Sudut ayunan maksimum ke kiri/kanan")]
    [SerializeField, Range(10f, 80f)] float maxAngle = 50f;

    [Header("Fisika ayunan")]
    [Tooltip("Centang = berat penumpang memengaruhi pompa & redaman. " +
             "Matikan = semua karakter mengayun sama cepat")]
    [SerializeField] bool massAffectsSwing = false;
    [Tooltip("Berat yang dipakai saat Mass Affects Swing dimatikan")]
    [SerializeField] float referenceMass = 5f;
    [Tooltip("Berat papan sendiri (ditambah berat penumpang)")]
    [SerializeField] float plankMass = 2f;
    [Tooltip("Kekuatan pompa. Dibagi total berat: makin berat makin lambat")]
    [SerializeField] float pumpForce = 4f;
    [Tooltip("Kekuatan rem saat menekan arah berlawanan (x pumpForce)")]
    [SerializeField, Range(0f, 1f)] float brakeFactor = 0.5f;
    [Tooltip("Redaman yang dibagi total berat: penumpang berat = berhenti lebih lama")]
    [SerializeField] float massDamping = 1.5f;
    [Tooltip("Redaman dasar (selalu ada)")]
    [SerializeField] float baseDamping = 0.2f;
    [Tooltip("Pengali gravitasi ayunan. Besar = ayunan lebih cepat bolak-balik")]
    [SerializeField] float gravityScale = 1f;

    [Header("Debug (lihat saat Play)")]
    [SerializeField] float debugAngle;
    [SerializeField] float debugTotalMass;
    [SerializeField] bool debugPumping;

    float angle;      // radian, positif = ke kanan
    float angularVel; // radian/detik
    readonly List<Collider2D> plankColliders = new List<Collider2D>();

    Vector2 Pivot => transform.position;

    void Awake()
    {
        if (plank == null)
        {
            Debug.LogError("SwingPlatform: slot Plank belum diisi", this);
            enabled = false;
            return;
        }

        plank.bodyType = RigidbodyType2D.Kinematic;
        plank.interpolation = RigidbodyInterpolation2D.Interpolate;
        plank.freezeRotation = true;
        plank.rotation = 0f;

        // Semua collider papan (papan + bibir kiri/kanan)
        plank.GetAttachedColliders(plankColliders);

        // Mulai diam di posisi bawah
        plank.position = PlankPositionAt(0f);
        plank.transform.position = plank.position;
        UpdateRopes(plank.position);
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // 1. Hitung penumpang: total berat + input dari karakter yang aktif
        float riderMass = 0f;
        float pumpInput = 0f;

        foreach (WeightBody wb in WeightBody.All)
        {
            if (wb.Body == plank || !IsOnPlank(wb)) continue;

            riderMass += wb.Mass;

            // Ganti "SheepController" kalau class karaktermu bernama lain
            CharacterController sheep = wb.GetComponent<CharacterController>();
            if (sheep != null && sheep.IsActive && Mathf.Abs(sheep.MoveInput) > 0.2f)
                pumpInput = Mathf.Sign(sheep.MoveInput);
        }

        float totalMass = massAffectsSwing
            ? Mathf.Max(0.1f, plankMass + riderMass)
            : Mathf.Max(0.1f, referenceMass);

        // 2. Percepatan sudut: gravitasi (menarik ke tengah)
        float g = Physics2D.gravity.magnitude * gravityScale;
        float accel = -(g / ropeLength) * Mathf.Sin(angle);

        // 3. Pompa dari karakter (searah = dorong, berlawanan = rem)
        if (pumpInput != 0f)
        {
            bool sameDirection = Mathf.Abs(angularVel) < 0.05f || Mathf.Sign(angularVel) == pumpInput;
            float factor = sameDirection ? 1f : brakeFactor;
            accel += pumpInput * pumpForce * factor / totalMass;
        }

        // 4. Redaman: ayunan melambat sendiri; penumpang berat = lebih lama
        accel -= (baseDamping + massDamping / totalMass) * angularVel;

        // 5. Integrasi + batas sudut
        angularVel += accel * dt;
        angle += angularVel * dt;

        float max = maxAngle * Mathf.Deg2Rad;
        if (Mathf.Abs(angle) > max)
        {
            angle = Mathf.Sign(angle) * max;
            angularVel = 0f;
        }

        // 6. Gerakkan papan lewat velocity, supaya penumpang ikut terbawa
        Vector2 target = PlankPositionAt(angle);
        plank.linearVelocity = (target - plank.position) / dt;

        debugAngle = angle * Mathf.Rad2Deg;
        debugTotalMass = totalMass;
        debugPumping = pumpInput != 0f;
    }

    void LateUpdate()
    {
        if (plank != null) UpdateRopes(plank.transform.position);
    }

    bool IsOnPlank(WeightBody wb)
    {
        for (int i = 0; i < plankColliders.Count; i++)
        {
            if (wb.IsSupportedBy(plankColliders[i])) return true;
        }
        return false;
    }

    Vector2 PlankPositionAt(float a)
    {
        return Pivot + ropeLength * new Vector2(Mathf.Sin(a), -Mathf.Cos(a));
    }

    void UpdateRopes(Vector2 plankPos)
    {
        PlaceRope(ropeLeft, Pivot + Vector2.left * ropeHalfSpan, plankPos + Vector2.left * ropeHalfSpan);
        PlaceRope(ropeRight, Pivot + Vector2.right * ropeHalfSpan, plankPos + Vector2.right * ropeHalfSpan);
    }

    void PlaceRope(Transform rope, Vector2 top, Vector2 bottom)
    {
        if (rope == null) return;

        Vector2 d = bottom - top;
        rope.position = (top + bottom) * 0.5f;
        rope.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + 90f);
        rope.localScale = new Vector3(ropeWidth, d.magnitude, 1f);
    }

    // Gambar titik gantung, tali, dan jangkauan ayunan di Scene view
    void OnDrawGizmos()
    {
        Vector2 p = transform.position;
        Vector2 l = p + Vector2.left * ropeHalfSpan;
        Vector2 r = p + Vector2.right * ropeHalfSpan;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(l, 0.08f);
        Gizmos.DrawSphere(r, 0.08f);

        // Busur jangkauan tengah papan
        Gizmos.color = new Color(0f, 1f, 1f, 0.8f);
        float max = maxAngle * Mathf.Deg2Rad;
        Vector2 prev = p + ropeLength * new Vector2(Mathf.Sin(-max), -Mathf.Cos(-max));
        for (int i = 1; i <= 24; i++)
        {
            float a = Mathf.Lerp(-max, max, i / 24f);
            Vector2 next = p + ropeLength * new Vector2(Mathf.Sin(a), -Mathf.Cos(a));
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        // Posisi papan di kiri, tengah, kanan
        Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
        foreach (float a in new[] { -max, 0f, max })
        {
            Vector2 c = p + ropeLength * new Vector2(Mathf.Sin(a), -Mathf.Cos(a));
            Gizmos.DrawLine(c + Vector2.left * ropeHalfSpan, c + Vector2.right * ropeHalfSpan);
        }
    }
}