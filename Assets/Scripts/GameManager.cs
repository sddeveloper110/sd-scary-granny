using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera menuCamera;
    [SerializeField] private string jsonFileName = "Tasks";
    [SerializeField] private GameObject missionSpawnPoints;

    [Header("UI")]
    [SerializeField] private Button hintButton;
    [SerializeField] private TypeWriter objectiveText;

    [Header("Finale Settings")]
    [SerializeField] private float survivalTime = 120f;

    public static GameManager Instance;

    public static event Action OnGameStarted;
    public static event Action OnSurvivalStarted;
    public Action OnGameWin;

    public List<TaskData> tasks = new List<TaskData>();

    public int currentTaskIndex = 0;

    public bool isGameStarted = false;
    private bool isInSurvivalMode = false;

    #region UNITY

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        isGameStarted = false;

        InteractableObject.OnObjectInteractionDone += HandleInteractableActivated;
        GhostAi.OnAttackEnemy += HandleGhostAttack;

        CanvasManager.OnGameExit += ExitGame;
        CanvasManager.OnGameRetry += Retry;
    }

    private void OnDestroy()
    {
        InteractableObject.OnObjectInteractionDone -= HandleInteractableActivated;
        GhostAi.OnAttackEnemy -= HandleGhostAttack;

        CanvasManager.OnGameExit -= ExitGame;
        CanvasManager.OnGameRetry -= Retry;
    }

    private void Start()
    {
        if (tasks == null || tasks.Count == 0)
            LoadTasks();

        LoadProgress();

        RestoreCompletedTasks();

        if (hintButton != null)
            hintButton.onClick.AddListener(ShowCurrentTaskHint);
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
        if (isGameStarted) return;

        isGameStarted = true;

        if (menuCamera != null)
            menuCamera.gameObject.SetActive(false);

        OnGameStarted?.Invoke();

        // If all tasks already completed
        if (currentTaskIndex >= tasks.Count)
        {
            StartSurvivalFinale();
            return;
        }

        UpdateTask();
    }

    public void Retry()
    {
        StopAllCoroutines();

        isGameStarted = false;
        isInSurvivalMode = false;

        StartGame();
    }

    private void HandleGhostAttack()
    {
        isGameStarted = false;
    }

    #endregion


    #region SURVIVAL FINALE

    private void StartSurvivalFinale()
    {
        if (isInSurvivalMode) return;

        isInSurvivalMode = true;

        SoundManager.Instance.PlayGameGrannyMusic();

        objectiveText.ShowText("SURVIVE UNTIL THE EXIT OPENS! (2:00)");

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
            CanvasManager.EnablePanel(PanelType.LevelComplete);
            PlayerPrefs.DeleteAll();
        });
    }

    public void GameEnd()
    {
        CanvasManager.FadeIn(.5f, () =>
        {
            CanvasManager.EnablePanel(PanelType.GameOver);
        });
    }

    public void ExitGame()
    {
        SaveProgress();

        isGameStarted = false;

        if (menuCamera != null)
            menuCamera.gameObject.SetActive(true);
    }

    #endregion


    #region SAVE LOAD

    private void SaveProgress()
    {
        PlayerPrefs.SetInt("TaskIndex", currentTaskIndex);
    }

    private void LoadProgress()
    {
        currentTaskIndex = PlayerPrefs.GetInt("TaskIndex", 0);

        if (currentTaskIndex < 0)
            currentTaskIndex = 0;

        if (currentTaskIndex > tasks.Count)
            currentTaskIndex = tasks.Count;
    }

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

        CanvasManager.EnablePanel(PanelType.Hint);
        HintSystem.Instance.ShowHint(task.GetHint());
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

        SaveProgress();

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