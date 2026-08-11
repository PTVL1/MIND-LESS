using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseMenuRoot;

    [Header("Input")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    [Header("Cursor")]
    [SerializeField] private bool showCursorWhilePaused = true;
    [SerializeField] private bool lockCursorDuringGameplay;

    private bool isPaused;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        Time.timeScale = 1f;
        SetPaused(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            SetPaused(!isPaused);
        }
    }

    public void ResumeGame()
    {
        SetPaused(false);
    }

    public void PauseGame()
    {
        SetPaused(true);
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(paused);
        }

        if (showCursorWhilePaused)
        {
            Cursor.visible = paused || !lockCursorDuringGameplay;
            Cursor.lockState = paused || !lockCursorDuringGameplay
                ? CursorLockMode.None
                : CursorLockMode.Locked;
        }
    }

    private void OnDestroy()
    {
        if (isPaused)
        {
            Time.timeScale = 1f;
        }
    }
}
