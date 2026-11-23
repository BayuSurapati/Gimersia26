using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Pengatur status level. Satu per scene gameplay. Belum bergantung pada UI apa pun.
///
/// Alur gagal  : Fail()     → kontrol dimatikan, slow motion sebentar → onFailSequenceDone
///                          → (jika autoRestart) level dimuat ulang.
/// Alur selesai: Complete() → kontrol dimatikan, jeda perayaan → onCompleteSequenceDone
///                          → (jika autoLoadNext) pindah ke level berikutnya.
///
/// Nanti saat UI sudah ada: matikan autoRestart / autoLoadNext, lalu hubungkan event "...SequenceDone" ke UI-nya.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public enum State { Playing, Failed, Completed }

    public static LevelManager Instance { get; private set; }

    [Header("Karakter")]
    [Tooltip("Script kontrol pemain yang dimatikan saat gagal/selesai (mis. PlayerController, CharacterSwitcher).")]
    [SerializeField] Behaviour[] disableOnEnd;
    [Tooltip("Rigidbody karakter yang dihentikan saat gagal/selesai. Saat gagal, karakter yang jatuh dibiarkan tetap jatuh.")]
    [SerializeField] Rigidbody2D[] stopOnEnd;

    [Header("Saat gagal")]
    [Tooltip("Jeda (detik nyata) setelah karakter jatuh, supaya pemain sempat melihatnya.")]
    [SerializeField] float failDelay = 0.8f;
    [Tooltip("Slow motion selama jeda. Isi 1 untuk mematikan.")]
    [Range(0.05f, 1f)]
    [SerializeField] float slowMotionScale = 0.5f;
    [Tooltip("Muat ulang level otomatis setelah gagal. Matikan nanti kalau sudah ada UI kalah.")]
    [SerializeField] bool autoRestart = true;
    [SerializeField] float autoRestartDelay = 0.4f;

    [Header("Saat selesai")]
    [Tooltip("Jeda (detik nyata) untuk momen perayaan sebelum lanjut.")]
    [SerializeField] float completeDelay = 1.2f;
    [Tooltip("Pindah ke level berikutnya (urutan Build Profiles) secara otomatis. Matikan nanti kalau sudah ada UI menang.")]
    [SerializeField] bool autoLoadNext = true;
    [Tooltip("Dimuat jika ini level terakhir.")]
    [SerializeField] string mainMenuSceneName = "MainMenu";

    [Header("Event (opsional)")]
    [Tooltip("Dipanggil tepat saat gagal, membawa alasan gagal. Cocok untuk SFX, kamera shake, dll.")]
    public UnityEvent<string> onFailed;
    [Tooltip("Dipanggil setelah jeda gagal selesai. Nanti hubungkan ke UI kalah di sini.")]
    public UnityEvent onFailSequenceDone;
    [Tooltip("Dipanggil tepat saat semua karakter sampai rumah. Cocok untuk SFX, konfeti, animasi karakter senang.")]
    public UnityEvent onCompleted;
    [Tooltip("Dipanggil setelah jeda perayaan selesai. Nanti hubungkan ke UI menang di sini.")]
    public UnityEvent onCompleteSequenceDone;

    /// <summary>Untuk script lain yang perlu tahu karakter mana yang jatuh (mis. kamera).</summary>
    public event Action<Transform> Failed;
    /// <summary>Untuk script lain yang perlu tahu level selesai.</summary>
    public event Action Completed;

    public State CurrentState { get; private set; } = State.Playing;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        Time.timeScale = 1f;   // jangan sampai slow motion terbawa ke scene lain
    }

    // =====================================================================
    // Gagal
    // =====================================================================

    /// <summary>Gagalkan level. Aman dipanggil berkali-kali (mis. dua karakter jatuh bersamaan).</summary>
    public void Fail(Transform who, string reason = "Jatuh")
    {
        if (CurrentState != State.Playing) return;
        CurrentState = State.Failed;

        FreezeCharacters(who);
        Failed?.Invoke(who);
        onFailed?.Invoke(reason);

        Time.timeScale = slowMotionScale;
        DOVirtual.DelayedCall(failDelay, OnFailDelayDone, true).SetLink(gameObject);
    }

    void OnFailDelayDone()
    {
        Time.timeScale = 1f;
        onFailSequenceDone?.Invoke();

        if (autoRestart)
            DOVirtual.DelayedCall(autoRestartDelay, Restart, true).SetLink(gameObject);
    }

    // =====================================================================
    // Selesai
    // =====================================================================

    /// <summary>Selesaikan level. Dipanggil oleh FinishZone. Tidak berpengaruh jika level sudah gagal.</summary>
    public void Complete()
    {
        if (CurrentState != State.Playing) return;
        CurrentState = State.Completed;

        FreezeCharacters(null);
        Completed?.Invoke();
        onCompleted?.Invoke();

        DOVirtual.DelayedCall(completeDelay, OnCompleteDelayDone, true).SetLink(gameObject);
    }

    void OnCompleteDelayDone()
    {
        onCompleteSequenceDone?.Invoke();
        if (autoLoadNext) LoadNextLevel();
    }

    // =====================================================================
    // Navigasi scene
    // =====================================================================

    /// <summary>Muat ulang level ini. Bisa dipanggil dari input restart (mis. tombol R).</summary>
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>Muat level berikutnya sesuai urutan scene di Build Profiles. Kalau ini level terakhir, kembali ke main menu.</summary>
    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(next);
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // =====================================================================
    // Helper
    // =====================================================================

    void FreezeCharacters(Transform except)
    {
        foreach (Behaviour b in disableOnEnd)
            if (b != null) b.enabled = false;

        foreach (Rigidbody2D rb in stopOnEnd)
        {
            if (rb == null || (except != null && rb.transform == except)) continue;
            rb.linearVelocity = Vector2.zero;   // Unity 6: pengganti rb.velocity
        }
    }
}