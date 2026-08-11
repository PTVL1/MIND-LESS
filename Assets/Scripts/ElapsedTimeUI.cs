using UnityEngine;
using UnityEngine.UI;

public class ElapsedTimeUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text elapsedTimeText;
    [SerializeField] private string prefix = "Time: ";
    [SerializeField] private bool showMilliseconds;

    private float elapsedSeconds;
    private bool isTracking = true;

    public float ElapsedSeconds => elapsedSeconds;

    private void Awake()
    {
        if (elapsedTimeText == null)
        {
            elapsedTimeText = GetComponent<Text>();
        }

        RefreshText();
    }

    private void Update()
    {
        if (!isTracking)
        {
            return;
        }

        // Time.deltaTime becomes zero while the pause menu sets timeScale to zero,
        // so time spent paused is not counted as gameplay time.
        elapsedSeconds += Time.deltaTime;
        RefreshText();
    }

    public void StartTracking()
    {
        isTracking = true;
    }

    public void StopTracking()
    {
        isTracking = false;
    }

    public void ResetTimer()
    {
        elapsedSeconds = 0f;
        RefreshText();
    }

    private void RefreshText()
    {
        if (elapsedTimeText == null)
        {
            return;
        }

        int totalSeconds = Mathf.FloorToInt(elapsedSeconds);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;

        string formattedTime = hours > 0
            ? string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds)
            : string.Format("{0:00}:{1:00}", minutes, seconds);

        if (showMilliseconds)
        {
            int milliseconds = Mathf.FloorToInt((elapsedSeconds % 1f) * 1000f);
            formattedTime += string.Format(".{0:000}", milliseconds);
        }

        elapsedTimeText.text = prefix + formattedTime;
    }
}
