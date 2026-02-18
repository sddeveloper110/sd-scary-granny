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
    [SerializeField] private int finaleTaskIndex = 9; // Task 10 is index 9
    [SerializeField] private float survivalTime = 120f; // 2 Minutes

    public static event Action OnSurvivalStarted;
    public Action OnGameWin;

    private bool isInSurvivalMode = false;

    public List<TaskData> tasks = new List<TaskData>();
    public int currentTaskIndex = 0;

    public static GameManager Instance;

    public bool isGameStarted = false;

    public static event Action OnGameStarted;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        isGameStarted = false;

        // Subscribe to global event
        InteractableObject.OnObjectInteractionDone += HandleInteractableActivated;
        GhostAi.OnAttackEnemy += () => isGameStarted = false;
        CanvasManager.OnGameExit += ExitGame;
        CanvasManager.OnGameRetry += Retry;
    }

    private void OnDestroy()
    {
        InteractableObject.OnObjectInteractionDone -= HandleInteractableActivated;
        GhostAi.OnAttackEnemy -= () => isGameStarted = false;
        CanvasManager.OnGameExit -= ExitGame;
        CanvasManager.OnGameRetry -= Retry;
    }

    private void Start()
    {
        if (tasks == null)
            LoadTasks();
        LoadProgress();


        if (hintButton != null)
            hintButton.onClick.AddListener(ShowCurrentTaskHint);
    }

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
            Debug.LogWarning("Mission Spawn Points object is not assigned.");
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

    #region Progression

    public TaskData GetCurrentTask() =>
        currentTaskIndex < tasks.Count ? tasks[currentTaskIndex] : null;

    public void CompleteCurrentTask()
    {
        if (currentTaskIndex >= tasks.Count - 1)
        {
            Debug.Log("All tasks completed.");
            return;
        }
        currentTaskIndex++;
        SaveProgress();
        UpdateTask();


    }

    #endregion

    #region Game Flow

    public void StartGame()
    {
        if (isGameStarted) return;

        isGameStarted = true;

        if (menuCamera != null)
            menuCamera.gameObject.SetActive(false);
        isGameStarted = true;
        OnGameStarted?.Invoke();
        UpdateTask();
    }

    public void Retry()
    {
        StopAllCoroutines(); // Stop survival timer if retrying
        isInSurvivalMode = false;
        isGameStarted = false;
        StartGame();
    }

    private void StartSurvivalFinale()
    {
        if (isInSurvivalMode) return;

        isInSurvivalMode = true;

        SoundManager.Instance.PlayGameGrannyMusic();

        // Update UI to let the player know they must survive
        objectiveText.ShowText("SURVIVE UNTIL THE EXIT OPENS! (2:00)");

        OnSurvivalStarted?.Invoke();

        // Start the countdown
        StartCoroutine(SurvivalRoutine());
    }

    private IEnumerator SurvivalRoutine()
    {
        float timer = survivalTime;

        while (timer > 0)
        {
            // 1. Check if player died
            if (!isGameStarted)
            {
                Debug.Log("Survival Failed: Player caught.");
                isInSurvivalMode = false;
                yield break;
            }

            timer -= Time.deltaTime;

            // 2. Format time to Minutes:Seconds (e.g., 01:45)
            int minutes = Mathf.FloorToInt(timer / 60);
            int seconds = Mathf.FloorToInt(timer % 60);
            string timeString = string.Format("{0:00}:{1:00}", minutes, seconds);

            // 3. Update the Objective Text with the countdown
            // Hum TypeWriter ko bypass karke direct text set kar sakte hain 
            // ya phir har second update bhej sakte hain
            objectiveText.uiText.text = ($"SURVIVE! EXIT OPENS IN: {timeString}");

            yield return null;
        }

        // Timer complete hone ke baad check
        if (isGameStarted)
        {
            objectiveText.ShowText("EXIT IS OPEN! RUN!");
            WinGame();
        }
    }
    private void WinGame()
    {
        isInSurvivalMode = false;
        isGameStarted = false; // Game logic stop kar do kyunke jeet gaye hain

        Debug.Log("Survivor! You won the game.");
        OnGameWin?.Invoke();

        // Music change to "Win/Happy" if you have it
        SoundManager.Instance.StopMusic();

        // Trigger Win UI
        CanvasManager.FadeIn(1.5f, () => {
            // Aapka Win Panel yahan enable hoga
            CanvasManager.EnablePanel(PanelType.LevelComplete);
            PlayerPrefs.DeleteAll();
            // Note: Aapka UI script check kar sakta hai ke agar OnGameWin invoke hua hai 
            // toh text "YOU ESCAPED" dikhaye bajaye "GAME OVER" ke.
        });
    }
    public void GameEnd()
    {
        CanvasManager.FadeIn(.5f, () => { CanvasManager.EnablePanel(PanelType.GameOver); });
    }

    public void ExitGame()
    {
        SaveProgress();

        // Reset game state
        isGameStarted = false;

        // Optional: enable menu camera again
        if (menuCamera != null)
            menuCamera.gameObject.SetActive(true);
    }

    #endregion

    #region Save/Load

    private void SaveProgress()
    {
        PlayerPrefs.SetInt("TaskIndex", currentTaskIndex);
    }

    private void LoadProgress()
    {
        currentTaskIndex = PlayerPrefs.GetInt("TaskIndex", 0);
        if (currentTaskIndex >= tasks.Count)
            currentTaskIndex = tasks.Count - 1;

        
    }

    #endregion

    #region UI + Hints

    public void CompleteTaskByName(string taskName)
    {
        for (int i = 0; i < tasks.Count; i++)
        {
            if (tasks[i].taskName.Equals(taskName, StringComparison.OrdinalIgnoreCase))
            {
                if (i == currentTaskIndex)
                {
                    CompleteCurrentTask();
                    return;
                }
                else
                {
                    Debug.Log($"Task '{taskName}' exists but is not the current task.");
                    return;
                }
            }
        }
        Debug.LogWarning($"Task '{taskName}' not found in task list.");
    }

    private void ShowCurrentTaskHint()
    {
        TaskData task = GetCurrentTask();
        if (task == null) return;

        CanvasManager.EnablePanel(PanelType.Hint);
        HintSystem.Instance.ShowHint(task.GetHint());
    }



    private void UpdateTask()
    {
        TaskData task = GetCurrentTask();
        if (task == null) return;

        objectiveText.ShowText(task.GetDescription());
    }


    #endregion

    #region Interactable Events

    [Button]
    private void HandleInteractableActivated(InteractableObject interactable)
    {
        TaskData current = GetCurrentTask();
        if (current == null || isInSurvivalMode) return;

        if (current.interactableToComplete == interactable)
        {
            // CHECK FOR FINALE
            if (currentTaskIndex >= finaleTaskIndex)
            {
                StartSurvivalFinale();
            }
            else if (current.interactableToComplete == interactable)
            {
                Debug.Log($"Task completed by interactable: {interactable.name}");
                CompleteCurrentTask();
            }
            else
            {
                Debug.Log($"Interactable {interactable.name} activated but does NOT belong to current task.");
            }
        }
    }
    #endregion

        #region Helper

    public Vector3 GetSpawnPosition
    {
        get => tasks[currentTaskIndex].taskSpawnPoint.position;
    }

    #endregion
}
