using Sirenix.OdinInspector;
using System;
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

    public List<TaskData> tasks = new List<TaskData>();
    public int currentTaskIndex = 0;

    public static GameManager Instance;

    public bool isGameStarted = false;

    public Action OnGameStarted;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        isGameStarted = false;

        // Subscribe to global event
        InteractableObject.OnObjectInteractionDone += HandleInteractableActivated;
    }

    private void OnDestroy()
    {
        InteractableObject.OnObjectInteractionDone -= HandleInteractableActivated;
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

        OnGameStarted?.Invoke();
        UpdateTask();
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

        UIManager.Instance.ShowPanel("HintPanel");
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
        if (current == null) return;

        if (current.interactableToComplete == interactable)
        {
            Debug.Log($"Task completed by interactable: {interactable.name}");
            CompleteCurrentTask();
        }
        else
        {
            Debug.Log($"Interactable {interactable.name} activated but does NOT belong to current task.");
        }
    }

    #endregion
}
