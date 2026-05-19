using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private string jsonFileName = "Tasks";
    [SerializeField] private GameObject missionSpawnPoints;

    [Header("UI")]
    [SerializeField] private Button hintButton;
    public Button showHintButton;
    [SerializeField] private TypeWriter objectiveText;

    [Header("Finale Settings")]
        [SerializeField] private float survivalTime = 180f;

    public static GameManager Instance;

    public static event Action OnGameStarted;
    public static event Action OnSurvivalStarted;
    public Action OnGameWin;

    public List<TaskData> tasks = new List<TaskData>();

    public int currentTaskIndex = 0;

    public bool isGameStarted = false;
    public bool isInSurvivalMode = false;

    private static bool shouldStartGameOnLoad = false;

    #region UNITY

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        Time.timeScale = 1f;
        isGameStarted = false;

        InteractableObject.OnObjectInteractionDone += HandleInteractableActivated;
        GrannyAI.OnAttackPlayer += HandleGhostAttack;

        CanvasManager.OnGameExit += ExitGame;
        CanvasManager.OnGameRetry += Retry;
        CanvasManager.OnGameRevive += Revive;
    }

    private void OnDestroy()
    {
        InteractableObject.OnObjectInteractionDone -= HandleInteractableActivated;
        GrannyAI.OnAttackPlayer -= HandleGhostAttack;

        CanvasManager.OnGameExit -= ExitGame;
        CanvasManager.OnGameRetry -= Retry;
        CanvasManager.OnGameRevive -= Revive;

        if (Instance == this)
        {
            Instance = null;
            // Reset static events to prevent leaks across scene reloads
            OnGameStarted = null;
            OnSurvivalStarted = null;
            InteractableObject.OnObjectInteractionDone = null;
        }
    }

    private void Start()
    {
        if (tasks == null || tasks.Count == 0)
            LoadTasks();

        currentTaskIndex = 0;

        RestoreCompletedTasks();

        if (hintButton != null)
            hintButton.onClick.AddListener(ShowCurrentTaskHint);

        if (showHintButton != null)
            showHintButton.onClick.AddListener(ShowHint);

        if (shouldStartGameOnLoad)
        {
            shouldStartGameOnLoad = false;
            StartGame();
        }
    }

    #endregion


    #region TASK LOADING

    [Button]
    private void LoadTasks()
    {
        TaskLoader loader = new TaskLoader { jsonFileName = jsonFileName };
        tasks = loader.LoadTasks();

        AssignSpawnPointsFromScene();
    }

    private void AssignSpawnPointsFromScene()
    {
        if (missionSpawnPoints == null)
        {
            Debug.LogWarning("Mission Spawn Points not assigned.");
            return;
        }

        Transform[] spawnChildren = missionSpawnPoints.GetComponentsInChildren<Transform>();

        List<Transform> childSpawns = new List<Transform>();

        foreach (Transform t in spawnChildren)
        {
            if (t != missionSpawnPoints.transform)
                childSpawns.Add(t);
        }

        for (int i = 0; i < tasks.Count; i++)
        {
            if (i < childSpawns.Count)
                tasks[i].taskSpawnPoint = childSpawns[i];
            else
                Debug.LogWarning($"Not enough spawn points for Task {i + 1}");
        }
    }

    private void RestoreCompletedTasks()
    {
        for (int i = 0; i < currentTaskIndex && i < tasks.Count; i++)
        {
            InteractableObject interactable = tasks[i].interactableToComplete;

            if (interactable != null && !interactable.IsInteracted)
            {
                interactable.IsInteracted = true;
                interactable.Activate();
            }
        }
    }

    #endregion


    #region GAME FLOW

    public void StartGame()
    {
        Debug.Log("[GameManager] StartGame requested. isGameStarted=" + isGameStarted);
        if (isGameStarted) return;

        isGameStarted = true;

        Debug.Log("[GameManager] Invoking OnGameStarted event...");
        OnGameStarted?.Invoke();

        // If all tasks already completed
        if (currentTaskIndex >= tasks.Count)
        {
            Debug.Log("[GameManager] All tasks completed, starting survival finale.");
            StartSurvivalFinale();
            return;
        }

        Debug.Log("[GameManager] Updating task info.");
        UpdateTask();
    }

    public void Retry()
    {
        StopAllCoroutines();

        isGameStarted = false;
        isInSurvivalMode = false;

        StartGame();
    }

    public void Revive()
    {
        // Revive does the same as Retry (spawn to last save object point)
        Retry();
    }

    public void NewGame()
    {
        Debug.Log("[GameManager] NewGame called");
        currentTaskIndex = 0;
        Retry();
    }

    private void HandleGhostAttack()
    {
        isGameStarted = false;
        GameEnd();
    }

    #endregion


    #region SURVIVAL FINALE

    private void StartSurvivalFinale()
    {
        if (isInSurvivalMode) return;

        isInSurvivalMode = true;

        SoundManager.Instance.PlayGameGrannyMusic();

                objectiveText.ShowText($"SURVIVE UNTIL THE EXIT OPENS! ({Mathf.FloorToInt(survivalTime / 60)}:00)");

        OnSurvivalStarted?.Invoke();

        StartCoroutine(SurvivalRoutine());
    }

    private IEnumerator SurvivalRoutine()
    {
        float timer = survivalTime;

        while (timer > 0)
        {
            if (!isGameStarted)
            {
                Debug.Log("Survival failed");
                isInSurvivalMode = false;
                yield break;
            }

            timer -= Time.deltaTime;

            int minutes = Mathf.FloorToInt(timer / 60);
            int seconds = Mathf.FloorToInt(timer % 60);

            string timeString = string.Format("{0:00}:{1:00}", minutes, seconds);

            objectiveText.uiText.text = $"SURVIVE! EXIT OPENS IN: {timeString}";

            yield return null;
        }

        if (isGameStarted)
        {
            objectiveText.ShowText("EXIT IS OPEN! RUN!");
            WinGame();
        }
    }

    #endregion


    #region WIN / LOSE

    private void WinGame()
    {
        isInSurvivalMode = false;
        isGameStarted = false;

        Debug.Log("Player Won");

        OnGameWin?.Invoke();

        SoundManager.Instance.StopMusic();

        CanvasManager.FadeIn(1.5f, () =>
        {
            UIPanelEnabler.OpenPanel(PanelType.LevelComplete);
            PlayerPrefs.DeleteAll();
        });
    }

    public void GameEnd()
    {
        CanvasManager.FadeIn(.5f, () =>
        {
            UIPanelEnabler.OpenPanel(PanelType.GameOver);
        });
    }

    public void ExitGame()
    {
        isGameStarted = false;

      
    }

    public void ExitAndClearProgress()
    {
        // Just reload the scene to reset everything
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    #endregion


    #region SAVE LOAD
    // Removed as per request (Start always starts new game)
    #endregion


    #region TASK SYSTEM

    public TaskData GetCurrentTask()
    {
        if (currentTaskIndex < tasks.Count)
            return tasks[currentTaskIndex];

        return null;
    }

    private void UpdateTask()
    {
        TaskData task = GetCurrentTask();

        if (task == null) return;

        objectiveText.ShowText(task.GetDescription());
    }

    private void ShowCurrentTaskHint()
    {
        TaskData task = GetCurrentTask();
        if (task == null) return;

        UIPanelEnabler.OpenPanel(PanelType.Hint);
        HintSystem.Instance.ShowHint(task.GetHint());
    }

    public void ShowHint()
    {
        TaskData task = GetCurrentTask();
        if (task == null) return;

        InteractableObject interactable = task.interactableToComplete;
        if (interactable == null) return;

        PickableObject neededPickable = null;
        PickableObject[] allPickables = FindObjectsOfType<PickableObject>(true);
        foreach (var p in allPickables)
        {
            if (p.interactsWith == interactable)
            {
                neededPickable = p;
                break;
            }
        }
        AdsManager.Instance.DisplayRewardedAd(() =>
        {

            if (neededPickable != null && !neededPickable.isPicked)
            {
                CanvasManager.ShowPopup("Pickable is highlighted now");
                if (neededPickable.highlightVFX != null)
                {
                    neededPickable.highlightVFX.SetActive(true);
                    Outline outline = neededPickable.highlightVFX.GetComponent<Outline>();
                    if (outline != null)
                    {
                        StartCoroutine(HighlightOutlineRoutine(outline, neededPickable.highlightVFX));
                    }
                }
                var camLook = FindFirstObjectByType<FirstPersonMobileTools.DynamicFirstPerson.CameraLook>();
                if (camLook != null) camLook.LookAtTarget(neededPickable.transform.position);
            }
            else
            {
                CanvasManager.ShowPopup("interactable is highlighted now");
                if (interactable.highlightVFX != null)
                {
                    interactable.highlightVFX.SetActive(true);
                    Outline outline = interactable.highlightVFX.GetComponent<Outline>();
                    if (outline != null)
                    {
                        StartCoroutine(HighlightOutlineRoutine(outline, interactable.highlightVFX));
                    }
                }
                var camLook = FindFirstObjectByType<FirstPersonMobileTools.DynamicFirstPerson.CameraLook>();
                if (camLook != null) camLook.LookAtTarget(interactable.transform.position);
            }
        });
    }

    private IEnumerator HighlightOutlineRoutine(Outline outline, GameObject highlightObj)
    {
        outline.OutlineMode = Outline.Mode.OutlineAll;
        
        float timer = 0f;
        float duration = 20f;

        while (timer < duration)
        {
            if (outline != null)
            {
                // Ping-pong between 3 and 7
                outline.OutlineWidth = Mathf.PingPong(timer * 8f, 4f) + 3f;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        if (outline != null)
        {
            outline.OutlineMode = Outline.Mode.OutlineVisible;
            outline.OutlineWidth = 2f;
        }

        if (highlightObj != null)
        {
            highlightObj.SetActive(false);
        }
    }

    #endregion


    #region INTERACTABLE EVENTS

    private void HandleInteractableActivated(InteractableObject interactable)
    {
        TaskData current = GetCurrentTask();

        if (current == null || isInSurvivalMode) return;

        if (current.interactableToComplete != interactable)
            return;

        Debug.Log($"Task completed: {interactable.name}");

        currentTaskIndex++;

        if (currentTaskIndex >= tasks.Count)
        {
            StartSurvivalFinale();
            return;
        }

        UpdateTask();
    }

    #endregion


    #region HELPER

    public Vector3 GetSpawnPosition
    {
        get
        {
            int index = Mathf.Clamp(currentTaskIndex, 0, tasks.Count - 1);
            return tasks[index].taskSpawnPoint.position;
        }
    }

    #endregion
}