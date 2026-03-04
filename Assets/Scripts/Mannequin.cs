using UnityEngine;
using System;

public class Mannequin : MonoBehaviour
{
    private Vector3 startPosition;
    private Quaternion startRotation;

    public float moveSpeed = 1.5f;
    public float attackDistance = 1.2f;

    public AudioSource moveAudio;

    public Action OnAttackPlayer;

    private Transform player;

    public void Initialize(Transform playerTarget)
    {
        player = playerTarget;

        startPosition = transform.position;
        startRotation = transform.rotation;

        if (moveAudio != null)
            moveAudio.Stop();
    }

    public void MoveTowardsPlayer()
    {
        if (player == null) return;

        if (moveAudio != null && !moveAudio.isPlaying)
            moveAudio.Play();

        transform.position = Vector3.MoveTowards(
            transform.position,
            player.position,
            moveSpeed * Time.deltaTime
        );

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir);
            transform.rotation = targetRotation;
        }

        if (Vector3.Distance(transform.position, player.position) <= attackDistance)
        {
            OnAttackPlayer?.Invoke();
        }
    }

    public void StopMoving()
    {
        if (moveAudio != null && moveAudio.isPlaying)
            moveAudio.Stop();
    }

    public void ResetMannequin()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;

        StopMoving();
    }
}