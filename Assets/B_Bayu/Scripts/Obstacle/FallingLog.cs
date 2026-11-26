using UnityEngine;

/// <summary>
/// Kayu berdiri yang hanya bisa ditumbangkan karakter dengan Strength cukup.
/// Selebihnya (miring, jatuh, mendarat) dikerjakan fisika Unity.
///
/// - Saat tegak & tidak didorong: kayu dikunci, jadi karakter lemah tidak bisa
///   menggesernya.
/// - Saat karakter AKTIF dengan Strength >= Required Strength mendorong dari samping:
///   kunci dilepas, kayu berengsel di sudut alas (supaya terguling, bukan tergelincir),
///   lalu fisika yang bekerja.
/// - Setelah rebah: engsel dilepas, kayu jadi benda fisika biasa.
///
/// Mass dan Gravity Scale diatur langsung di Rigidbody 2D.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class FallingLog : MonoBehaviour
{
    [Tooltip("Strength minimal karakter untuk bisa menumbangkan kayu")]
    public int requiredStrength = 2;

    const float SideNormal = 0.5f;   // sentuhan dianggap dari samping
    const float UprightAngle = 1f;   // di bawah ini = dianggap tegak
    const float FallenAngle = 80f;   // di atas ini = dianggap sudah rebah

    Rigidbody2D rb;
    BoxCollider2D box;
    HingeJoint2D hinge;
    int hingeSide;
    bool fallen;
    float restTimer;
    int pushDirLastStep;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        Lock();
    }

    void FixedUpdate()
    {
        if (fallen) return;

        float tilt = -Mathf.DeltaAngle(0f, rb.rotation); // positif = condong ke kanan
        bool pushed = pushDirLastStep != 0;

        // Diam di posisi miring jauh (misalnya tertahan tepian / karakter)
        restTimer = Mathf.Abs(tilt) > 30f && Mathf.Abs(rb.angularVelocity) < 5f
            ? restTimer + Time.fixedDeltaTime
            : 0f;

        if (Mathf.Abs(tilt) >= FallenAngle || restTimer > 0.3f)
        {
            Release();
        }
        else if (pushed || Mathf.Abs(tilt) > UprightAngle)
        {
            // Engsel di sudut alas sisi condong (atau sisi arah dorongan saat masih tegak)
            int side = Mathf.Abs(tilt) > UprightAngle ? (tilt > 0f ? 1 : -1) : pushDirLastStep;
            Unlock(side);
        }
        else
        {
            Lock();
        }

        pushDirLastStep = 0;
    }

    void Lock()
    {
        RemoveHinge();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
        rb.rotation = 0f;
        rb.angularVelocity = 0f;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    void Unlock(int side)
    {
        rb.constraints = RigidbodyConstraints2D.None;
        if (hinge != null && hingeSide == side) return;

        RemoveHinge();
        hinge = gameObject.AddComponent<HingeJoint2D>();
        hinge.anchor = box.offset + new Vector2(side * box.size.x * 0.5f, -box.size.y * 0.5f);
        hinge.autoConfigureConnectedAnchor = true;
        hingeSide = side;
    }

    void Release()
    {
        fallen = true;
        RemoveHinge();
        rb.constraints = RigidbodyConstraints2D.None;
    }

    void RemoveHinge()
    {
        if (hinge != null) Destroy(hinge);
        hinge = null;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (fallen) return;

        Rigidbody2D other = collision.rigidbody;
        if (other == null) return;

        // Ganti "SheepController" kalau class karaktermu bernama lain
        CharacterController sheep = other.GetComponent<CharacterController>();
        if (sheep == null || !sheep.IsActive || sheep.strength < requiredStrength) return;

        // Harus menekan arah ke kayu
        float input = sheep.MoveInput;
        if (Mathf.Abs(input) < 0.2f) return;
        int dir = input > 0f ? 1 : -1;
        if ((rb.position.x - other.position.x) * dir <= 0f) return;

        // Sentuhan dari samping, bukan berdiri di atas kayu
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Mathf.Abs(collision.GetContact(i).normal.x) >= SideNormal)
            {
                pushDirLastStep = dir;
                return;
            }
        }
    }
}