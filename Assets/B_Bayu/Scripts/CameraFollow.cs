using UnityEngine;

/// <summary>
/// Kamera 2D sederhana yang mengikuti karakter aktif dengan halus.
/// Pasang di Main Camera. Target diisi otomatis oleh SheepSwitcher.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    [SerializeField] Vector2 offset = new Vector2(0f, 1f);
    [SerializeField] float smoothTime = 0.25f;

    [Header("Batas kamera (opsional)")]
    [SerializeField] bool useBounds;
    [SerializeField] Vector2 minPosition = new Vector2(-10f, -5f);
    [SerializeField] Vector2 maxPosition = new Vector2(10f, 5f);

    Vector3 velocity;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z);

        if (useBounds)
        {
            desired.x = Mathf.Clamp(desired.x, minPosition.x, maxPosition.x);
            desired.y = Mathf.Clamp(desired.y, minPosition.y, maxPosition.y);
        }

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}