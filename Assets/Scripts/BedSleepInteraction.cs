using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BedSleepInteraction : MonoBehaviour
{
    [Header("Sleep")]
    [SerializeField] private KeyCode sleepKey = KeyCode.F;
    [SerializeField] private float interactionRadius = 2.5f;
    [SerializeField] private float sleepDuration = 5f;
    [SerializeField] private Transform sleepPoint;
    [SerializeField] private float fallbackSleepHeight = 0.75f;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Text sleepProgressText;

    private Coroutine sleepRoutine;
    private bool movementWasEnabledBeforeSleep;
    private CharacterController characterController;
    private bool controllerWasEnabledBeforeSleep;
    private Vector3 playerPositionBeforeSleep;
    private Quaternion playerRotationBeforeSleep;

    private void Awake()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (playerMovement == null && player != null)
        {
            playerMovement = player.GetComponentInParent<PlayerMovement>();
        }

        if (player != null)
        {
            characterController = player.GetComponentInParent<CharacterController>();
        }

        SetProgressVisible(false);
    }

    private void Update()
    {
        if (sleepRoutine != null || DayNightCycle.Instance == null || !DayNightCycle.Instance.IsNight)
        {
            return;
        }

        if (IsPlayerClose() && Input.GetKeyDown(sleepKey))
        {
            sleepRoutine = StartCoroutine(Sleep());
        }
    }

    private IEnumerator Sleep()
    {
        float elapsed = 0f;
        movementWasEnabledBeforeSleep = playerMovement != null && playerMovement.enabled;
        playerPositionBeforeSleep = player.position;
        playerRotationBeforeSleep = player.rotation;
        controllerWasEnabledBeforeSleep = characterController != null && characterController.enabled;

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        Vector3 restingPosition = sleepPoint != null
            ? sleepPoint.position
            : transform.position + Vector3.up * fallbackSleepHeight;
        Quaternion restingRotation = sleepPoint != null
            ? sleepPoint.rotation
            : transform.rotation * Quaternion.Euler(0f, 0f, 90f);
        player.SetPositionAndRotation(restingPosition, restingRotation);

        SetProgressVisible(true);

        while (elapsed < sleepDuration)
        {
            if (!IsPlayerClose() || DayNightCycle.Instance == null || !DayNightCycle.Instance.IsNight)
            {
                FinishSleeping();
                yield break;
            }

            elapsed += Time.deltaTime;
            if (sleepProgressText != null)
            {
                float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, sleepDuration));
                sleepProgressText.text = "Sleeping: " + Mathf.RoundToInt(progress * 100f) + "%";
            }

            yield return null;
        }

        DayNightCycle.Instance.SkipNight();
        FinishSleeping();
    }

    private bool IsPlayerClose()
    {
        return player != null &&
            (player.position - transform.position).sqrMagnitude <= interactionRadius * interactionRadius;
    }

    private void FinishSleeping()
    {
        RestorePlayerPose();

        if (playerMovement != null)
        {
            playerMovement.enabled = movementWasEnabledBeforeSleep;
        }

        SetProgressVisible(false);
        sleepRoutine = null;
    }

    private void RestorePlayerPose()
    {
        if (player != null)
        {
            player.SetPositionAndRotation(playerPositionBeforeSleep, playerRotationBeforeSleep);
        }

        if (characterController != null)
        {
            characterController.enabled = controllerWasEnabledBeforeSleep;
        }
    }

    private void SetProgressVisible(bool visible)
    {
        if (sleepProgressText != null)
        {
            sleepProgressText.gameObject.SetActive(visible);
        }
    }

    private void OnDisable()
    {
        bool wasSleeping = sleepRoutine != null;
        if (sleepRoutine != null)
        {
            StopCoroutine(sleepRoutine);
            sleepRoutine = null;
        }

        if (wasSleeping && playerMovement != null)
        {
            playerMovement.enabled = movementWasEnabledBeforeSleep;
        }

        if (wasSleeping)
        {
            RestorePlayerPose();
        }

        SetProgressVisible(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
