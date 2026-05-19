using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class UiFeelMaster : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private enum EffectState { Waiting, Animating, Resetting }
    public enum Direction { Left, Right, Up, Down }

    [Header("--- Main Toggles ---")]
    public bool useScale = true;
    public bool useRotate = true;
    public bool useShake = true;
    public bool useShine = false;

    [Header("--- Interaction ---")]
    public bool playSoundOnClick = false;
    public bool popOnClick = false;

    [Header("--- OnEnable Effects ---")]
    public bool useRandomImage = false;
    public bool usePopEffect = false;
    public bool useSlideEffect = false;

    // Settings (HideInInspector use kar rahe hain kyunke Editor handle karega)
    [HideInInspector] public float scaleAmount = 1.15f;
    [HideInInspector] public float scaleDuration = 0.5f;
    [HideInInspector] public float scaleWait = 3.0f;

    [HideInInspector] public float rotateStrength = 800f;
    [HideInInspector] public float rotateFriction = 3.0f;
    [HideInInspector] public float rotateWait = 5.0f;

    [HideInInspector] public float shakeStrength = 5.0f;
    [HideInInspector] public float shakeSpeed = 60.0f;
    [HideInInspector] public float shakeDuration = 0.4f;
    [HideInInspector] public float shakeWait = 6.0f;

    [HideInInspector] public RectTransform shineObject;
    [HideInInspector] public float shineSpeed = 500f;
    [HideInInspector] public float shineWait = 3f;

    [HideInInspector] public Sprite[] randomImages;
    [HideInInspector] public float popDuration = 0.3f;
    [HideInInspector] public Direction appearFrom = Direction.Right;
    [HideInInspector] public float slideDuration = 0.5f;
    [HideInInspector] public float offsetDistance = 1000f;

    private float shineTimer, scaleTimer, rotateTimer, shakeTimer, currentRotVelocity;
    private bool isShining = false;
    private EffectState scaleState, rotateState, shakeState = EffectState.Waiting;

    private RectTransform rect;
    private Image imageComponent;
    private Vector3 initialScale;
    private Vector2 originalPosition;
    private float initialZ = 0f, currentZ;
    private bool isInteracting = false;
    private float currentPulseScale = 1f; // Pulse ki current value save karne ke liye
    private float clickScaleMultiplier = 1f; private float currentJitter = 0f;
    void Awake()
    {
        rect = GetComponent<RectTransform>();
        imageComponent = GetComponent<Image>();
        initialScale = rect.localScale;
        originalPosition = rect.anchoredPosition;
        currentZ = rect.localEulerAngles.z;

       
        ResetTimers();
    }


    void OnEnable()
    {
        StopAllCoroutines();
        if (useRandomImage) ApplyRandomImage();
        if (usePopEffect) StartCoroutine(PopRoutine());
        if (useSlideEffect) StartCoroutine(SlideRoutine());
    }

    public void ResetTimers()
    {
        scaleTimer = Random.Range(0, scaleWait);
        rotateTimer = Random.Range(0, rotateWait);
        shakeTimer = Random.Range(0, shakeWait);
        shineTimer = Random.Range(0, shineWait);
    }

    public void TestScale() { scaleState = EffectState.Animating; scaleTimer = scaleDuration; }
    public void TestRotate() { rotateState = EffectState.Animating; currentRotVelocity = rotateStrength; }
    public void TestShake() { shakeState = EffectState.Animating; shakeTimer = shakeDuration; }
    public void TestPop() { StopAllCoroutines(); StartCoroutine(PopRoutine()); }
    public void TestSlide() { StopAllCoroutines(); StartCoroutine(SlideRoutine()); }

    void Update()
    {
        if (useScale && !isInteracting) HandleScaleLogic();
        rect.localScale = initialScale * (currentPulseScale * clickScaleMultiplier);

        if (useRotate && !isInteracting) HandleRotate();
        if (useShake) HandleShake(); // Shake humesha chal sakta hai

        float finalZ = (rotateState != EffectState.Waiting ? currentZ : initialZ) + currentJitter;
        rect.localRotation = Quaternion.Euler(0, 0, finalZ);
    }

    // [Wait logic and animation methods remain the same as your original script to avoid breaking logic]
    private void HandleShake()
    {
        shakeTimer -= Time.deltaTime;

        // Agar wait khatam ho jaye toh test shake trigger karein
        if (shakeState == EffectState.Waiting && shakeTimer <= 0) TestShake();

        if (shakeState == EffectState.Animating)
        {
            // currentJitter mein value save karein bajaye direct apply karne ke
            currentJitter = Mathf.Sin(Time.time * shakeSpeed) * shakeStrength;

            if (shakeTimer <= 0)
            {
                currentJitter = 0f; // Reset jitter
                shakeState = EffectState.Waiting;
                shakeTimer = shakeWait;
            }
        }
    }
    private void HandleScale() { /* Same as your original */ scaleTimer -= Time.deltaTime; if (scaleState == EffectState.Waiting && scaleTimer <= 0) TestScale(); else if (scaleState == EffectState.Animating) { float progress = 1f - (scaleTimer / scaleDuration); float wave = Mathf.Sin(progress * Mathf.PI); rect.localScale = initialScale * Mathf.Lerp(1f, scaleAmount, wave); if (scaleTimer <= 0) { rect.localScale = initialScale; scaleState = EffectState.Waiting; scaleTimer = scaleWait; } } }
    private void HandleRotate() { if (rotateState == EffectState.Waiting) { rotateTimer -= Time.deltaTime; if (rotateTimer <= 0) TestRotate(); } else if (rotateState == EffectState.Animating) { currentZ += currentRotVelocity * Time.deltaTime; rect.localRotation = Quaternion.Euler(0, 0, currentZ); currentRotVelocity = Mathf.MoveTowards(currentRotVelocity, 0, rotateFriction * 150 * Time.deltaTime); if (currentRotVelocity <= 0.1f) { rotateState = EffectState.Resetting; rotateTimer = 0.5f; } } else if (rotateState == EffectState.Resetting) { rotateTimer -= Time.deltaTime; float lerpZ = Mathf.LerpAngle(currentZ, initialZ, 1f - (rotateTimer / 0.5f)); rect.localRotation = Quaternion.Euler(0, 0, lerpZ); if (rotateTimer <= 0) { currentZ = initialZ; rect.localRotation = Quaternion.Euler(0, 0, initialZ); rotateState = EffectState.Waiting; rotateTimer = rotateWait; } } }
    private void HandleScaleLogic()
    {
        scaleTimer -= Time.deltaTime;
        if (scaleState == EffectState.Waiting && scaleTimer <= 0) TestScale();
        else if (scaleState == EffectState.Animating)
        {
            float progress = 1f - (scaleTimer / scaleDuration);
            float wave = Mathf.Sin(progress * Mathf.PI);
            currentPulseScale = Mathf.Lerp(1f, scaleAmount, wave);

            if (scaleTimer <= 0)
            {
                currentPulseScale = 1f;
                scaleState = EffectState.Waiting;
                scaleTimer = scaleWait;
            }
        }
    }
    private void HandleShine() { if (shineObject == null) return; if (!isShining) { shineTimer -= Time.deltaTime; if (shineTimer <= 0) { isShining = true; shineObject.anchoredPosition = new Vector2(-rect.rect.width, 0); } } else { shineObject.anchoredPosition += new Vector2(shineSpeed * Time.deltaTime, 0); if (shineObject.anchoredPosition.x > rect.rect.width) { isShining = false; shineTimer = shineWait; shineObject.anchoredPosition = new Vector2(-rect.rect.width * 2, 0); } } }
    void ApplyRandomImage() { if (imageComponent != null && randomImages != null && randomImages.Length > 0) imageComponent.sprite = randomImages[Random.Range(0, randomImages.Length)]; }

    IEnumerator PopRoutine()
    {
        transform.localScale = Vector3.zero;
        float elapsed = 0;
        while (elapsed < popDuration * 0.7f) { elapsed += Time.deltaTime; transform.localScale = Vector3.Lerp(Vector3.zero, initialScale * 1.2f, elapsed / (popDuration * 0.7f)); yield return null; }
        elapsed = 0;
        while (elapsed < popDuration * 0.3f) { elapsed += Time.deltaTime; transform.localScale = Vector3.Lerp(initialScale * 1.2f, initialScale, elapsed / (popDuration * 0.3f)); yield return null; }
        transform.localScale = initialScale;
    }

    IEnumerator SlideRoutine()
    {
        Vector2 startPos = originalPosition;
        switch (appearFrom) { case Direction.Left: startPos.x -= offsetDistance; break; case Direction.Right: startPos.x += offsetDistance; break; case Direction.Up: startPos.y += offsetDistance; break; case Direction.Down: startPos.y -= offsetDistance; break; }
        rect.anchoredPosition = startPos;
        float elapsed = 0;
        while (elapsed < slideDuration) { elapsed += Time.deltaTime; float t = elapsed / slideDuration; t = t * t * (3f - 2f * t); rect.anchoredPosition = Vector2.Lerp(startPos, originalPosition, t); yield return null; }
        rect.anchoredPosition = originalPosition;
    }

   public void OnPointerDown(PointerEventData eventData)
{
    // Pulse ko pause nahi karenge, bas multiplier ko chota kar denge
    if(popOnClick)
    clickScaleMultiplier = 0.8f; 
    
    if (playSoundOnClick) SoundManager.PlayTapAudio();

    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (popOnClick)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothPopMultiplier());
        }
        else
        {
            clickScaleMultiplier = 1f;
        }
    }

    // Naya Coroutine jo current scale (0.8) se pop shuru karega
    IEnumerator SmoothPopMultiplier()
    {
        float elapsed = 0;
        // Release par 0.8 se 1.1 tak jayega smoothly
        while (elapsed < popDuration * 0.6f)
        {
            elapsed += Time.deltaTime;
            clickScaleMultiplier = Mathf.Lerp(0.8f, 1.1f, elapsed / (popDuration * 0.6f));
            yield return null;
        }

        elapsed = 0;
        while (elapsed < popDuration * 0.4f)
        {
            elapsed += Time.deltaTime;
            clickScaleMultiplier = Mathf.Lerp(1.1f, 1f, elapsed / (popDuration * 0.4f));
            yield return null;
        }

        clickScaleMultiplier = 1f;
    }
}