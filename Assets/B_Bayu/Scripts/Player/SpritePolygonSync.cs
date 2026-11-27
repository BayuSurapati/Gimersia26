using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Membuat Polygon Collider 2D selalu mengikuti bentuk sprite yang sedang tampil,
/// termasuk saat sprite berganti karena animasi dan saat sprite dibalik (Flip X / Y).
///
/// Bentuk diambil dari "Physics Shape" sprite (lihat Sprite Editor > Custom Physics Shape).
/// Bentuk tiap sprite disimpan (cache), jadi tidak dihitung ulang setiap frame.
///
/// Pasang di objek yang punya Polygon Collider 2D. Sprite Renderer boleh di objek yang
/// sama atau di objek anak.
/// </summary>
[RequireComponent(typeof(PolygonCollider2D))]
[DefaultExecutionOrder(100)] // setelah script lain; LateUpdate tetap setelah Animator
public class SpritePolygonSync : MonoBehaviour
{
    [Tooltip("Kosongkan: otomatis dicari di objek ini atau anak-anaknya")]
    [SerializeField] SpriteRenderer spriteRenderer;

    [Tooltip("Perkecil bentuk collider ke arah tengah (0 = sama persis dengan sprite, 0.9 = 90%)")]
    [SerializeField, Range(0.5f, 1f)] float shapeScale = 1f;

    [Header("Debug")]
    [SerializeField] string debugCurrentSprite;

    PolygonCollider2D poly;

    Sprite lastSprite;
    bool lastFlipX, lastFlipY;
    Matrix4x4 lastMatrix;

    // Cache bentuk mentah per sprite (dalam satuan lokal sprite)
    static readonly Dictionary<Sprite, List<Vector2[]>> shapeCache = new Dictionary<Sprite, List<Vector2[]>>();
    static readonly List<Vector2> tempPoints = new List<Vector2>();
    readonly List<Vector2> pathBuffer = new List<Vector2>();

    void Awake()
    {
        poly = GetComponent<PolygonCollider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpritePolygonSync: Sprite Renderer tidak ditemukan", this);
            enabled = false;
        }
    }

    void OnEnable() => Refresh(force: true);

    void LateUpdate() => Refresh(force: false);

    void Refresh(bool force)
    {
        if (spriteRenderer == null || poly == null) return;

        Sprite sprite = spriteRenderer.sprite;
        Matrix4x4 m = transform.worldToLocalMatrix * spriteRenderer.transform.localToWorldMatrix;

        if (!force && sprite == lastSprite && spriteRenderer.flipX == lastFlipX &&
            spriteRenderer.flipY == lastFlipY && m == lastMatrix)
            return;

        lastSprite = sprite;
        lastFlipX = spriteRenderer.flipX;
        lastFlipY = spriteRenderer.flipY;
        lastMatrix = m;
        debugCurrentSprite = sprite != null ? sprite.name : "(none)";

        if (sprite == null)
        {
            poly.pathCount = 0;
            return;
        }

        List<Vector2[]> shapes = GetShapes(sprite);
        if (shapes.Count == 0) return; // sprite tanpa physics shape: biarkan collider lama

        float sx = lastFlipX ? -1f : 1f;
        float sy = lastFlipY ? -1f : 1f;
        Vector2 center = sprite.bounds.center;

        poly.pathCount = shapes.Count;
        for (int s = 0; s < shapes.Count; s++)
        {
            pathBuffer.Clear();
            Vector2[] src = shapes[s];
            for (int i = 0; i < src.Length; i++)
            {
                // Perkecil ke arah tengah sprite, lalu balik sesuai flip
                Vector2 p = center + (src[i] - center) * shapeScale;
                p = new Vector2(p.x * sx, p.y * sy);

                // Dari ruang sprite renderer ke ruang objek collider
                pathBuffer.Add(m.MultiplyPoint3x4(p));
            }
            poly.SetPath(s, pathBuffer);
        }
    }

    static List<Vector2[]> GetShapes(Sprite sprite)
    {
        if (shapeCache.TryGetValue(sprite, out List<Vector2[]> cached)) return cached;

        var list = new List<Vector2[]>();
        int count = sprite.GetPhysicsShapeCount();
        for (int i = 0; i < count; i++)
        {
            tempPoints.Clear();
            sprite.GetPhysicsShape(i, tempPoints);
            list.Add(tempPoints.ToArray());
        }

        if (count == 0)
            Debug.LogWarning($"SpritePolygonSync: sprite \"{sprite.name}\" tidak punya Physics Shape. " +
                             "Centang Generate Physics Shape di Import Settings sprite.");

        shapeCache[sprite] = list;
        return list;
    }
}