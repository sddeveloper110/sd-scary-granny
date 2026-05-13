using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject movementController;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] AudioClip jumpscare;
    [Header("Attack Feel Settings")]
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeIntensity = 0.2f;

    [Header("Slap Settings")]
    [SerializeField] private float slapSpeed = 0.15f; // Kitni jaldi garden ghume
    [SerializeField] private float slapRotationAmount = 40f; // Kitna door tak head jaye
    private Vector3 originalCamPos;

    
    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.OnGameStarted += SetupForGameplay;
        GrannyAI.OnAttackPlayer += HandlePlayerHit;
       
    }


    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.OnGameStarted -= SetupForGameplay;
    GrannyAI.OnAttackPlayer -= HandlePlayerHit;
    }

    private void Start()
    {
        ToggleControls(GameManager.Instance.isGameStarted);
    }
    private void HandlePlayerHit()
    {
        SoundManager.PlayThisAudio(jumpscare);

        StartCoroutine(HitSequence());
    }

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

        GameManager.Instance.GameEnd();
        //ToggleControls(false);
    }
    public void ToggleControls(bool isActive)
    {
        if (movementController != null) movementController.SetActive(isActive);
        if (playerCamera != null) playerCamera.gameObject.SetActive(isActive);
        if(gameplayPanel !=null) gameplayPanel.gameObject.SetActive(isActive);   
    }

    private void ShowGameplayUI()
    {
        CanvasManager.LoadToGameplay();
    }

    public void SetupForGameplay()
    {
        ResetPlayer(GameManager.Instance.GetSpawnPosition,Quaternion.identity);
        ToggleControls(true);
        ShowGameplayUI();
    }

    public void ResetPlayer(Vector3 position, Quaternion rotation)
    {
        ToggleControls(false);
        transform.localPosition = position;
        transform.rotation = rotation;

        transform.GetChild(0).localPosition = Vector3.zero;
        transform.GetChild(0).rotation = Quaternion.Euler(0,90,0);
    }
}
