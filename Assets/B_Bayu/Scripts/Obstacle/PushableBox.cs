using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kotak yang hanya bisa didorong karakter AKTIF dengan strength yang cukup.
///
/// Cara kerja: posisi X kotak dikunci (seperti tembok). Saat karakter yang aktif
/// menyentuhnya dari samping, script mengecek "barisan" kotak di depannya:
///   - Kalau ujung barisan kosong, dan karakter cukup kuat untuk SEMUA kotak
///     di barisan itu, seluruh barisan dibuka kuncinya dan ikut terdorong.
///   - Kalau barisan mentok tembok / pintu / karakter lain, atau ada kotak yang
///     terlalu berat, tidak ada yang bergerak (bersih, tidak bergetar).
/// Sumbu Y tidak dikunci: kotak tetap bisa jatuh, dinaiki, ditumpuk, jadi pemberat.
/// Kotak juga ikut terbawa kalau berdiri di atas sesuatu yang bergerak.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(WeightBody))]
public class PushableBox : MonoBehaviour
{
    [Tooltip("2 = Sedang & Besar bisa dorong, 3 = hanya Besar")]
    public int requiredStrength = 2;

    [Tooltip("Seberapa 'menyamping' sentuhan agar dihitung dorongan (1 = harus tepat dari samping)")]
    [SerializeField, Range(0.3f, 1f)] float minSideNormal = 0.7f;

    [Tooltip("Maksimal jumlah kotak dalam satu barisan dorongan")]
    [SerializeField] int maxChain = 5;

    Rigidbody2D rb;
    WeightBody weight;
    Collider2D col;
    bool pushedLastStep;

    static readonly ContactPoint2D[] contactBuffer = new ContactPoint2D[16];
    static readonly List<PushableBox> chain = new List<PushableBox>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        weight = GetComponent<WeightBody>();
        col = GetComponent<Collider2D>();
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        // Urutan Unity: FixedUpdate -> simulasi fisika -> OnCollisionStay2D.
        // Jadi di sini kita memakai hasil sentuhan dari langkah fisika sebelumnya.

        float carryX = 0f;
        Collider2D support = weight.Support;
        if (support != null && support.attachedRigidbody != null)
            carryX = support.attachedRigidbody.GetPointVelocity(rb.position).x;
        bool carried = Mathf.Abs(carryX) > 0.01f;

        if (pushedLastStep)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        else if (carried)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.linearVelocity = new Vector2(carryX, rb.linearVelocity.y);
        }
        else
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        pushedLastStep = false;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        Rigidbody2D other = collision.rigidbody;
        if (other == null) return;

        // Ganti "SheepController" kalau class karaktermu bernama lain
        CharacterController sheep = other.GetComponent<CharacterController>();
        if (sheep == null || !sheep.IsActive) return;

        // Hanya sentuhan dari samping
        bool fromSide = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Mathf.Abs(collision.GetContact(i).normal.x) >= minSideNormal)
            {
                fromSide = true;
                break;
            }
        }
        if (!fromSide) return;

        // Arah dorong: menjauhi karakter
        int dir = col.bounds.center.x >= sheep.transform.position.x ? 1 : -1;

        if (BuildChain(dir, sheep.strength))
        {
            foreach (PushableBox box in chain) box.pushedLastStep = true;
        }
    }

    /// <summary>
    /// Kumpulkan barisan kotak mulai dari kotak ini ke arah dir.
    /// Return true kalau barisan boleh bergerak.
    /// </summary>
    bool BuildChain(int dir, int strength)
    {
        chain.Clear();
        PushableBox current = this;

        while (current != null)
        {
            if (current.requiredStrength > strength) return false; // terlalu berat
            chain.Add(current);
            if (chain.Count > maxChain) return false;

            PushableBox next = null;
            int count = current.rb.GetContacts(contactBuffer);

            for (int i = 0; i < count; i++)
            {
                ContactPoint2D c = contactBuffer[i];
                if (Mathf.Abs(c.normal.x) < minSideNormal) continue; // lantai / atap

                float side = c.point.x - current.col.bounds.center.x;
                if (side * dir <= 0f) continue; // bukan di sisi depan

                // Ambil rigidbody "lawan" dari kontak ini
                Rigidbody2D otherRb = c.rigidbody == current.rb ? c.otherRigidbody : c.rigidbody;

                // Tembok, lantai statis, pintu (Kinematic) => mentok
                if (otherRb == null || otherRb.bodyType != RigidbodyType2D.Dynamic) return false;

                PushableBox otherBox = otherRb.GetComponent<PushableBox>();
                if (otherBox == null) return false;          // karakter lain / benda lain => mentok
                if (chain.Contains(otherBox)) continue;      // sudah dihitung

                next = otherBox;
            }

            current = next;
        }

        return true;
    }
}