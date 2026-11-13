using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mengatur karakter mana yang sedang dikontrol.
/// Keyboard: Tab = berikutnya, 1/2/3 = pilih langsung.
/// Gamepad : RB/R1 = berikutnya, LB/L1 = sebelumnya.
/// Juga memindahkan target kamera dan penanda (panah) di atas karakter aktif.
/// Pasang di GameObject kosong "GameManager".
/// </summary>
public class CharacterSwitcher : MonoBehaviour
{
    [Tooltip("Urutan di sini = tombol 1, 2, 3")]
    [SerializeField] List<CharacterController> sheep = new List<CharacterController>();
    [SerializeField] CameraFollow cameraFollow;

    [Header("Input (dari GameControls)")]
    [SerializeField] InputActionReference nextAction;     // Player/NextCharacter
    [SerializeField] InputActionReference prevAction;     // Player/PrevCharacter
    [SerializeField] InputActionReference select1Action;  // Player/SelectCharacter1
    [SerializeField] InputActionReference select2Action;  // Player/SelectCharacter2
    [SerializeField] InputActionReference select3Action;  // Player/SelectCharacter3

    [Header("Penanda karakter aktif (opsional)")]
    [Tooltip("Prefab sprite kecil (misalnya segitiga) yang muncul di atas karakter aktif")]
    [SerializeField] GameObject selectionMarkerPrefab;
    [SerializeField] float markerHeight = 0.4f;

    int index;
    GameObject marker;

    public CharacterController Active => sheep.Count > 0 ? sheep[index] : null;
    public IReadOnlyList<CharacterController> All => sheep;

    void OnEnable()
    {
        EnableAction(nextAction);
        EnableAction(prevAction);
        EnableAction(select1Action);
        EnableAction(select2Action);
        EnableAction(select3Action);
    }

    void Start()
    {
        if (selectionMarkerPrefab) marker = Instantiate(selectionMarkerPrefab);
        if (sheep.Count > 0) Select(0); // menimpa "Active On Start" di tiap karakter
    }

    void Update()
    {
        if (sheep.Count == 0) return;

        if (Pressed(nextAction)) Select((index + 1) % sheep.Count);
        if (Pressed(prevAction)) Select((index - 1 + sheep.Count) % sheep.Count);

        if (Pressed(select1Action) && sheep.Count > 0) Select(0);
        if (Pressed(select2Action) && sheep.Count > 1) Select(1);
        if (Pressed(select3Action) && sheep.Count > 2) Select(2);
    }

    void LateUpdate()
    {
        if (marker == null || Active == null) return;

        // Taruh penanda tepat di atas kepala karakter aktif
        Collider2D col = Active.GetComponent<Collider2D>();
        Vector3 pos = Active.transform.position;
        if (col != null) pos = new Vector3(col.bounds.center.x, col.bounds.max.y + markerHeight, 0f);
        marker.transform.position = pos;
    }

    public void Select(int i)
    {
        index = i;
        for (int k = 0; k < sheep.Count; k++) sheep[k].IsActive = (k == i);
        if (cameraFollow) cameraFollow.target = sheep[i].transform;
    }

    /// <summary>Matikan semua kontrol (dipakai saat level selesai / gagal).</summary>
    public void DisableAll()
    {
        foreach (CharacterController s in sheep) s.IsActive = false;
        enabled = false;
        if (marker) marker.SetActive(false);
    }

    static void EnableAction(InputActionReference a)
    {
        if (a != null) a.action.Enable();
    }

    static bool Pressed(InputActionReference a)
    {
        return a != null && a.action.WasPressedThisFrame();
    }
}