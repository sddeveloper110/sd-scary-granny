using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UiFeelMaster : MonoBehaviour
{
    private enum EffectState { Waiting, Animating, Resetting }

    public enum Direction { Left, Right, Up, Down }

    [Header("--- Toggles ---")]
    public bool useScale = true;
    public bool useRotate = true;
    public bool useShake = true;
    public bool useShine = false;

    [Header("--- OnEnable Effects ---")]
    public bool useRandomImage = false;
    public bool usePopEffect = false;
    public bool useSlideEffect = false;

    // Scale settings
    [HideInInspector] public float scaleAmount = 1.15f;
    [HideInInspector] public float scaleDuration = 0.5f;
    [HideInInspector] public float scaleWait = 3.0f;

    // Rotate settings
    [HideInInspector] public float rotateStrength = 800f;
    [HideInInspector] public float rotateFriction = 3.0f;
    [HideInInspector] public float rotateWait = 5.0f;

    // Shake settings
    [HideInInspector] public float shakeStrength = 5.0f;
    [HideInInspector] public float shakeSpeed = 60.0f;
    [HideInInspector] public float shakeDuration = 0.4f;
    [HideInInspector] public float shakeWait = 6.0f;

    // Shine settings
    [HideInInspector] public RectTransform shineObject;
    [HideInInspector] public float shineSpeed = 500f;
    [HideInInspector] public float shineWait = 3f;

    // Random Image settings
    [HideInInspector] public Sprite[] randomImages;

    // Pop Effect settings
    [HideInInspector] public float popDuration = 0.3f;

    // Slide Effect settings
    [HideInInspector] public Direction appearFrom = Direction.Right;
    [HideInInspector] public float slideDuration = 0.5f;
    [HideInInspector] public float offsetDistance = 1000f;

    // Private variables for continuous effects
    private float shineTimer;
    private bool isShining = false;
    private EffectState scaleState = EffectState.Waiting;
    private float scaleTimer;
    private float currentRotVelocity;
    private EffectState rotateState = EffectState.Waiting;
    private float rotateTimer;
    private EffectState shakeState = EffectState.Waiting;
    private float shakeTimer;

    private RectTransform rect;
    private Image imageComponent;
    private Vector3 initialScale;
    private Vector2 originalPosition;
    private float initialZ = 0f;
    private float currentZ;

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
        // Reset state before starting animations
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

    // Public methods so the Editor Button can trigger them
    public void TestScale() { scaleState = EffectState.Animating; scaleTimer = scaleDuration; }
    public void TestRotate() { rotateState = EffectState.Animating; currentRotVelocity = rotateStrength; }
    public void TestShake() { shakeState = EffectState.Animating; shakeTimer = shakeDuration; }
    public void TestPop() { StopAllCoroutines(); StartCoroutine(PopRoutine()); }
    public void TestSlide() { StopAllCoroutines(); StartCoroutine(SlideRoutine()); }

    void Update()
    {
        if (useScale) HandleScale();
        if (useRotate) HandleRotate();
        if (useShake) HandleShake();
        if (useShine) HandleShine();
    }

    private void HandleScale()
    {
        scaleTimer -= Time.deltaTime;
        if (scaleState == EffectState.Waiting && scaleTimer <= 0) TestScale();
        else if (scaleState == EffectState.Animating)
        {
            float progress = 1f - (scaleTimer / scaleDuration);
            float wave = Mathf.Sin(progress * Mathf.PI);
            rect.localScale = initialScale * Mathf.Lerp(1f, scaleAmount, wave);
            if (scaleTimer <= 0)
            {
                rect.localScale = initialScale;
                scaleState = EffectState.Waiting;
                scaleTimer = scaleWait;
            }
        }
    }

    private void HandleRotate()
    {
        if (rotateState == EffectState.Waiting)
        {
            rotateTimer -= Time.deltaTime;
            if (rotateTimer <= 0) TestRotate();
        }
        else if (rotateState == EffectState.Animating)
        {
            currentZ += currentRotVelocity * Time.deltaTime;
            rect.localRotation = Quaternion.Euler(0, 0, currentZ);
            currentRotVelocity = Mathf.MoveTowards(currentRotVelocity, 0, rotateFriction * 150 * Time.deltaTime);
            if (currentRotVelocity <= 0.1f)
            {
                rotateState = EffectState.Resetting;
                rotateTimer = 0.5f;
            }
        }
        else if (rotateState == EffectState.Resetting)
        {
            rotateTimer -= Time.deltaTime;
            float lerpZ = Mathf.LerpAngle(currentZ, initialZ, 1f - (rotateTimer / 0.5f));
            rect.localRotation = Quaternion.Euler(0, 0, lerpZ);
            if (rotateTimer <= 0)
            {
                currentZ = initialZ;
                rect.localRotation = Quaternion.Euler(0, 0, initialZ);
                rotateState = EffectState.Waiting;
                rotateTimer = rotateWait;
            }
        }
    }

    private void HandleShake()
    {
        shakeTimer -= Time.deltaTime;
        if (shakeState == EffectState.Waiting && shakeTimer <= 0) TestShake();
        else if (shakeState == EffectState.Animating)
        {
            float jitter = Mathf.Sin(Time.time * shakeSpeed) * shakeStrength;
            rect.localRotation = Quaternion.Euler(0, 0, initialZ + jitter);
            if (shakeTimer <= 0)
            {
                rect.localRotation = Quaternion.Euler(0, 0, initialZ);
                shakeState = EffectState.Waiting;
                shakeTimer = shakeWait;
            }
        }
    }

    private void HandleShine()
    {
        if (shineObject == null) return;

        if (!isShining)
        {
            shineTimer -= Time.deltaTime;
            if (shineTimer <= 0)
            {
                isShining = true;
                shineObject.anchoredPosition = new Vector2(-rect.rect.width, 0);
            }
        }
        else
        {
            shineObject.anchoredPosition += new Vector2(shineSpeed * Time.deltaTime, 0);

            if (shineObject.anchoredPosition.x > rect.rect.width)
            {
                isShining = false;
                shineTimer = shineWait;
                shineObject.anchoredPosition = new Vector2(-rect.rect.width * 2, 0);
            }
        }
    }

    // OnEnable Effects
    void ApplyRandomImage()
    {
        if (imageComponent != null && randomImages != null && randomImages.Length > 0)
        {
            imageComponent.sprite = randomImages[Random.Range(0, randomImages.Length)];
        }
    }

    IEnumerator PopRoutine()
    {
        transform.localScale = Vector3.zero;
        float elapsed = 0;

        // Scale up to 1.2
        while (elapsed < popDuration * 0.7f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.zero, initialScale * 1.2f, elapsed / (popDuration * 0.7f));
            yield return null;
        }

        // Settle back to 1.0
        elapsed = 0;
        while (elapsed < popDuration * 0.3f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(initialScale * 1.2f, initialScale, elapsed / (popDuration * 0.3f));
            yield return null;
        }

        transform.localScale = initialScale;
    }

    IEnumerator SlideRoutine()
    {
        Vector2 startPos = originalPosition;

        // Set starting offset based on chosen direction
        switch (appearFrom)
        {
            case Direction.Left: startPos.x -= offsetDistance; break;
            case Direction.Right: startPos.x += offsetDistance; break;
            case Direction.Up: startPos.y += offsetDistance; break;
            case Direction.Down: startPos.y -= offsetDistance; break;
        }

        rect.anchoredPosition = startPos;
        float elapsed = 0;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            // Using SmoothStep for a "natural" feeling slide
            float t = elapsed / slideDuration;
            t = t * t * (3f - 2f * t);
            rect.anchoredPosition = Vector2.Lerp(startPos, originalPosition, t);
            yield return null;
        }

        rect.anchoredPosition = originalPosition;
    }
}