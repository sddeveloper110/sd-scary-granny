using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] AudioClip jumpscare;

    [Header("Attack Feel Settings")]
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeIntensity = 0.2f;

    [Header("Slap Settings")]
    [SerializeField] private float slapSpeed = 0.15f; // Kitni jaldi garden ghume
    [SerializeField] private float slapRotationAmount = 40f; // Kitna door tak head jaye

    // ── NEW: Mutual Sighting Screams ─────────────────────────────────────────
    [Header("Mutual Sighting Screams (NEW)")]
    [Tooltip("Granny's scream audio — plays when player looks at Granny while she spots them.")]
    [SerializeField] private AudioClip grannyScreamClip;
    [Tooltip("Player's scared scream audio — plays at the same moment.")]
    [SerializeField] private AudioClip playerScreamClip;
    [Tooltip("Seconds before another mutual-sighting scream can play. " +
             "Should match or be slightly longer than GrannyAI.mutualSightingCooldown.")]
    [SerializeField] private float screamCooldown = 4f;

    private float _lastScreamTime = -99f;
    // ─────────────────────────────────────────────────────────────────────────

    private Vector3 originalCamPos;

    private void OnEnable()
    {
        Debug.Log("[PlayerController] Subscribing to OnGameStarted");
        GameManager.OnGameStarted += SetupForGameplay;
        GrannyAI.OnAttackPlayer += HandlePlayerHit;
        GrannyAI.OnMutualSighting += HandleMutualSighting; // NEW
    }

    private void OnDisable()
    {
        GameManager.OnGameStarted -= SetupForGameplay;
        GrannyAI.OnAttackPlayer -= HandlePlayerHit;
        GrannyAI.OnMutualSighting -= HandleMutualSighting; // NEW
    }

    private void HandlePlayerHit()
    {
        SoundManager.PlayThisAudio(jumpscare);
        StartCoroutine(HitSequence());
    }

    // ── NEW: Mutual Sighting Handler ──────────────────────────────────────────
    /// <summary>
    /// Called by GrannyAI.OnMutualSighting when Granny spots the player
    /// AND the player's camera can see Granny at the same moment.
    /// Plays both Granny's scream and the player's scared scream.
    /// </summary>
    private void HandleMutualSighting()
    {
        // Guard: don't spam if the event fires repeatedly (extra safety on top of GrannyAI cooldown)
        if (Time.time - _lastScreamTime < screamCooldown) return;
        _lastScreamTime = Time.time;

        if (grannyScreamClip != null)
            SoundManager.PlayThisAudio(grannyScreamClip);

        if (playerScreamClip != null)
            SoundManager.PlayThisAudio(playerScreamClip);
    }
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator HitSequence()
    {
        // 1. Force Camera to look straight (X = 0)
        Vector3 currentRot = playerCamera.transform.localEulerAngles;
        playerCamera.transform.localEulerAngles = new Vector3(0, currentRot.y, 0);

        // 2. Shake Phase (Darr/Struggle)
        float elapsed = 0f;
        Vector3 camOriginalLocalPos = playerCamera.transform.localPosition;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;
            playerCamera.transform.localPosition = camOriginalLocalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Shake ke baad position reset
        playerCamera.transform.localPosition = camOriginalLocalPos;

        // 3. Slap Phase (Head Rotation)
        // Ghost ka right hand slap player ke head ko left side pe rotate karega
        float slapElapsed = 0f;
        Quaternion startRot = playerCamera.transform.localRotation;
        Quaternion targetSlapRot = Quaternion.Euler(10f, startRot.eulerAngles.y - slapRotationAmount, 0f);
        while (slapElapsed < slapSpeed)
        {
            slapElapsed += Time.deltaTime;
            // Slerp use kar rahe hain smoothly rotate karne ke liye
            playerCamera.transform.localRotation = Quaternion.Slerp(startRot, targetSlapRot, slapElapsed / slapSpeed);
            yield return null;
        }

        //GameManager.Instance.GameEnd();
        //ToggleControls(false);
    }

    private void ShowGameplayUI()
    {
        CanvasManager.LoadToGameplay();
    }

    public void SetupForGameplay()
    {
        Debug.Log("[PlayerController] SetupForGameplay called!");
        ResetPlayer(GameManager.Instance.GetSpawnPosition, Quaternion.identity);
        //ToggleControls(true);
        ShowGameplayUI();
    }

    public void ResetPlayer(Vector3 position, Quaternion rotation)
    {
        transform.localPosition = position;
        transform.rotation = rotation;
        transform.GetChild(0).localPosition = Vector3.zero;
        transform.GetChild(0).rotation = Quaternion.Euler(0, -90, 0);
    }
}