using UnityEngine;
using System.Collections.Generic;
using FirstPersonMobileTools.DynamicFirstPerson;

public class MannequinRoomController : MonoBehaviour
{
    public List<Mannequin> mannequins = new List<Mannequin>();

    public Camera playerCamera;
    public Transform player;

    public float lookThreshold = 0.85f;
    public float approachCooldown = 10f;

    private bool playerInside = false;
    private float playerEnterTime = 0f;
    private bool roomLocked = false; // 🔥 freeze state

    private void OnEnable()
    {
        CanvasManager.OnGameStart += ResetRoom;
        CanvasManager.OnGameRetry += ResetRoom;
    }

    private void OnDisable()
    {
        CanvasManager.OnGameStart -= ResetRoom;
        CanvasManager.OnGameRetry -= ResetRoom;
    }

    private void Start()
    {
        foreach (var m in mannequins)
        {
            m.Initialize(player);

            // 🔥 listen to attack event
            m.OnAttackPlayer += HandleMannequinAttack;
        }
    }

    private void Update()
    {
        if (!playerInside || roomLocked || Time.timeScale == 0) return;

        // Cooldown before mannequins start moving
        if (Time.time < playerEnterTime + approachCooldown) return;

        foreach (var mannequin in mannequins)
        {
            if (!IsLookingAtMannequin(mannequin))
            {
                mannequin.MoveTowardsPlayer();
            }
            else
            {
                mannequin.StopMoving();
            }
        }
    }

    bool IsLookingAtMannequin(Mannequin mannequin)
    {
        Vector3 dirToMannequin =
            (mannequin.transform.position - playerCamera.transform.position).normalized;

        float dot = Vector3.Dot(playerCamera.transform.forward, dirToMannequin);

        return dot > lookThreshold;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<MovementController>() != null)
        {
            if (!playerInside)
            {
                playerInside = true;
                playerEnterTime = Time.time;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<MovementController>() != null)
        {
            playerInside = false;
        }
    }

    void HandleMannequinAttack()
    {
        roomLocked = true;

        foreach (var mannequin in mannequins)
        {
            mannequin.StopMoving();
        }

        // Trigger game over via GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameEnd();
        }

        Debug.Log("Player attacked by mannequin");
    }

    public void ResetRoom()
    {
        playerInside = false;
        roomLocked = false;

        foreach (var mannequin in mannequins)
        {
            mannequin.ResetMannequin();
        }
    }
}