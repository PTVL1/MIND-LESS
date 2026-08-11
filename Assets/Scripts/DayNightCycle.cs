using System;
using UnityEngine;
using UnityEngine.UI;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    [Header("Time")]
    [SerializeField] private float dayDuration = 120f;
    [SerializeField] private float nightDuration = 90f;
    [SerializeField] private bool startAtNight;

    [Header("Lighting")]
    [SerializeField] private Light sun;
    [SerializeField] private float daySunIntensity = 1f;
    [SerializeField] private float nightSunIntensity = 0.08f;
    [SerializeField] private Color dayAmbientColor = new Color(0.65f, 0.65f, 0.65f);
    [SerializeField] private Color nightAmbientColor = new Color(0.05f, 0.07f, 0.15f);
    [SerializeField] private Color daySunColor = new Color(1f, 0.96f, 0.82f);
    [SerializeField] private Color nightSunColor = new Color(0.25f, 0.35f, 0.65f);

    [Header("Sky")]
    [SerializeField] private Material sunnyDaySkybox;
    [SerializeField] private Material darkNightSkybox;
    [SerializeField] private float fallbackDaySkyExposure = 1.2f;
    [SerializeField] private float fallbackNightSkyExposure = 0.08f;

    [Header("UI")]
    [SerializeField] private Text dayCounterText;
    [SerializeField] private Text phaseText;

    private float phaseElapsed;
    private bool isNight;
    private int currentDay = 1;
    private Material originalSkybox;
    private Material runtimeDaySkybox;
    private Material runtimeNightSkybox;

    public bool IsNight => isNight;
    public int CurrentDay => currentDay;
    public float PhaseProgress => phaseElapsed / Mathf.Max(0.1f, isNight ? nightDuration : dayDuration);

    public event Action<int> DayStarted;
    public event Action<int> NightStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        isNight = startAtNight;
        originalSkybox = RenderSettings.skybox;
        CreateFallbackSkyboxes();

        if (sun == null)
        {
            sun = RenderSettings.sun;
        }

        RefreshUI();
        ApplySkybox();
        UpdateLighting();
    }

    private void Start()
    {
        if (isNight)
        {
            NightStarted?.Invoke(currentDay);
        }
        else
        {
            DayStarted?.Invoke(currentDay);
        }
    }

    private void Update()
    {
        phaseElapsed += Time.deltaTime;
        float duration = Mathf.Max(0.1f, isNight ? nightDuration : dayDuration);

        if (phaseElapsed >= duration)
        {
            if (isNight)
            {
                BeginNextDay();
            }
            else
            {
                BeginNight();
            }
        }

        UpdateLighting();
    }

    public bool SkipNight()
    {
        if (!isNight)
        {
            return false;
        }

        BeginNextDay();
        return true;
    }

    private void BeginNight()
    {
        isNight = true;
        phaseElapsed = 0f;
        RefreshUI();
        ApplySkybox();
        NightStarted?.Invoke(currentDay);
    }

    private void BeginNextDay()
    {
        isNight = false;
        phaseElapsed = 0f;
        currentDay++;
        RefreshUI();
        ApplySkybox();
        DayStarted?.Invoke(currentDay);
    }

    private void RefreshUI()
    {
        if (dayCounterText != null)
        {
            dayCounterText.text = "Day " + currentDay;
        }

        if (phaseText != null)
        {
            phaseText.text = isNight ? "Night" : "Day";
        }
    }

    private void UpdateLighting()
    {
        float progress = Mathf.Clamp01(PhaseProgress);
        float sunAngle = isNight
            ? Mathf.Lerp(90f, 270f, progress)
            : Mathf.Lerp(-90f, 90f, progress);

        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
            sun.intensity = isNight ? nightSunIntensity : daySunIntensity;
            sun.color = isNight ? nightSunColor : daySunColor;
        }

        RenderSettings.ambientLight = isNight ? nightAmbientColor : dayAmbientColor;
    }

    private void CreateFallbackSkyboxes()
    {
        if (originalSkybox == null)
        {
            return;
        }

        runtimeDaySkybox = new Material(originalSkybox);
        runtimeNightSkybox = new Material(originalSkybox);
        ConfigureFallbackSkybox(runtimeDaySkybox, fallbackDaySkyExposure, Color.white);
        ConfigureFallbackSkybox(runtimeNightSkybox, fallbackNightSkyExposure, new Color(0.08f, 0.12f, 0.25f));
    }

    private void ConfigureFallbackSkybox(Material skybox, float exposure, Color tint)
    {
        if (skybox.HasProperty("_Exposure"))
        {
            skybox.SetFloat("_Exposure", exposure);
        }

        if (skybox.HasProperty("_Tint"))
        {
            skybox.SetColor("_Tint", tint);
        }

        if (skybox.HasProperty("_SkyTint"))
        {
            skybox.SetColor("_SkyTint", tint);
        }
    }

    private void ApplySkybox()
    {
        Material targetSkybox = isNight
            ? (darkNightSkybox != null ? darkNightSkybox : runtimeNightSkybox)
            : (sunnyDaySkybox != null ? sunnyDaySkybox : runtimeDaySkybox);

        if (targetSkybox != null)
        {
            RenderSettings.skybox = targetSkybox;
            DynamicGI.UpdateEnvironment();
        }
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        Instance = null;
        RenderSettings.skybox = originalSkybox;

        Destroy(runtimeDaySkybox);
        Destroy(runtimeNightSkybox);
    }
}
