using UnityEngine;

public class OpenableObject : MonoBehaviour
{
    public enum OffsetSpace { Local, World }
 
    [Header("Gerakan")]
    [Tooltip("Geser ke mana saat terbuka. Contoh: (0, 3, 0) = naik 3 unit.")]
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 3f, 0f);
    [Tooltip("Local = mengikuti rotasi objek. World = arah mutlak di scene.")]
    [SerializeField] private OffsetSpace offsetSpace = OffsetSpace.World;
    [SerializeField] private float speed = 4f;
 
    [Header("Status awal")]
    [SerializeField] private bool startOpen = false;
 
    public bool IsOpen { get; private set; }
 
    private Vector3 closedPos;
    private Vector3 openPos;
    private Vector3 target;
 
    private void Awake()
    {
        // Posisi di scene dianggap posisi "tertutup"
        closedPos = transform.position;
        openPos = closedPos + GetWorldOffset();
        SetState(startOpen, instant: true);
    }
 
    private void Update()
    {
        if (transform.position != target)
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }
 
    private Vector3 GetWorldOffset()
    {
        return offsetSpace == OffsetSpace.Local
            ? transform.TransformVector(openOffset)
            : openOffset;
    }
 
    private void SetState(bool open, bool instant = false)
    {
        IsOpen = open;
        target = open ? openPos : closedPos;
        if (instant) transform.position = target;
    }
 
    public void Open() => SetState(true);
    public void Close() => SetState(false);
    public void Toggle() => SetState(!IsOpen);
 
    // Garis & titik di Scene view supaya kelihatan objek akan bergeser ke mana
    private void OnDrawGizmosSelected()
    {
        Vector3 from = Application.isPlaying ? closedPos : transform.position;
        Vector3 to = Application.isPlaying ? openPos : from + GetWorldOffset();
 
        Gizmos.color = Color.green;
        Gizmos.DrawLine(from, to);
        Gizmos.DrawWireSphere(to, 0.15f);
    }
}
