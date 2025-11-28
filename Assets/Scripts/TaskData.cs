using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerializableVector3
{
    public float x, y, z;
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

[System.Serializable]
public class SerializableRotation
{
    public float x, y, z;
    public Quaternion ToQuaternion() => Quaternion.Euler(x, y, z);
}

[System.Serializable]
public class TaskJsonData
{
    public string taskName;
    public string description;
    public string hint;
    public SerializableVector3 spawnPosition;
    public SerializableRotation spawnRotation;
}

[System.Serializable]
public class TaskJsonWrapper
{
    public TaskJsonData[] tasks;
}

[System.Serializable]
public class TaskData
{
    public string taskName;
    public string description;
    public string hint;
    public Transform taskSpawnPoint;

    [Header("Task Completion Binding")]
    public InteractableObject interactableToComplete;

    public string GetTaskName() => taskName;
    public string GetDescription() => description;
    public string GetHint() => hint;
    public Transform GetSpawnPoint() => taskSpawnPoint;
}

public class TaskLoader : MonoBehaviour
{
    [Header("JSON Config")]
    [SerializeField] public string jsonFileName = "TasksJson";

    /// <summary>
    /// Load tasks from JSON file and create TaskData list
    /// </summary>
    public List<TaskData> LoadTasks()
    {
        List<TaskData> tasks = new List<TaskData>();

        TextAsset jsonText = Resources.Load<TextAsset>(jsonFileName);
        if (jsonText == null)
        {
            Debug.LogError($"JSON file '{jsonFileName}' not found in Resources!");
            return tasks;
        }

        TaskJsonWrapper wrapper = JsonUtility.FromJson<TaskJsonWrapper>(WrapJsonArray(jsonText.text));

        foreach (var t in wrapper.tasks)
        {
            TaskData task = new TaskData
            {
                taskName = t.taskName,
                description = t.description,
                hint = t.hint,
                
            };
            tasks.Add(task);
        }

        return tasks;
    }


    // Wrap JSON array for Unity JsonUtility
    private string WrapJsonArray(string rawJson)
    {
        return "{ \"tasks\": " + rawJson + "}";
    }
}
