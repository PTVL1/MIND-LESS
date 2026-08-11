using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyDeathRagdollFade : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MindLess.Gameplay.HealthSystem healthSystem;
    [SerializeField] private MonoBehaviour[] behavioursToDisable;

    [Header("Timing")]
    [SerializeField] private float delayBeforeFade = 3f;
    [SerializeField] private float fadeDuration = 2f;

    [Header("Fade")]
    [SerializeField] private string baseColorProperty = "_BaseColor";
    [SerializeField] private string fallbackColorProperty = "_Color";

    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;
    private Renderer[] renderers;
    private Color[] originalColors;
    private bool hasDied;

    private void Awake()
    {
        if (healthSystem == null)
        {
            healthSystem = GetComponent<MindLess.Gameplay.HealthSystem>();
        }

        if (healthSystem != null)
        {
            healthSystem.SetDestroyOnDeath(false);
        }

        ragdollBodies = GetComponentsInChildren<Rigidbody>(true);
        ragdollColliders = GetComponentsInChildren<Collider>(true);
        renderers = GetComponentsInChildren<Renderer>(true);
        originalColors = new Color[renderers.Length];

        CacheOriginalColors();
        SetRagdollEnabled(false);
    }

    private void OnEnable()
    {
        if (healthSystem != null)
        {
            healthSystem.Died += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (healthSystem != null)
        {
            healthSystem.Died -= HandleDeath;
        }
    }

    private void HandleDeath()
    {
        if (hasDied)
        {
            return;
        }

        hasDied = true;

        DisableLiveComponents();
        SetRagdollEnabled(true);

        StartCoroutine(FadeAndDestroy());
    }

    private void DisableLiveComponents()
    {
        if (behavioursToDisable != null)
        {
            for (int i = 0; i < behavioursToDisable.Length; i++)
            {
                if (behavioursToDisable[i] != null)
                {
                    behavioursToDisable[i].enabled = false;
                }
            }
        }

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }

        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
        }
    }

    private void SetRagdollEnabled(bool enabled)
    {
        for (int i = 0; i < ragdollBodies.Length; i++)
        {
            Rigidbody body = ragdollBodies[i];
            if (body == null)
            {
                continue;
            }

            if (body.gameObject == gameObject)
            {
                body.isKinematic = !enabled;
                body.useGravity = enabled;
                continue;
            }

            body.isKinematic = !enabled;
            body.useGravity = enabled;
        }

        for (int i = 0; i < ragdollColliders.Length; i++)
        {
            Collider col = ragdollColliders[i];
            if (col == null)
            {
                continue;
            }

            if (col.gameObject == gameObject)
            {
                col.enabled = true;
            }
            else
            {
                col.enabled = enabled;
            }
        }
    }

    private IEnumerator FadeAndDestroy()
    {
        yield return new WaitForSeconds(delayBeforeFade);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float alpha = Mathf.Lerp(1f, 0f, t);
            ApplyAlpha(alpha);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void CacheOriginalColors()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rendererRef = renderers[i];
            if (rendererRef == null || rendererRef.sharedMaterial == null)
            {
                originalColors[i] = Color.white;
                continue;
            }

            Material mat = rendererRef.material;
            if (mat.HasProperty(baseColorProperty))
            {
                originalColors[i] = mat.GetColor(baseColorProperty);
            }
            else if (mat.HasProperty(fallbackColorProperty))
            {
                originalColors[i] = mat.GetColor(fallbackColorProperty);
            }
            else
            {
                originalColors[i] = Color.white;
            }
        }
    }

    private void ApplyAlpha(float alpha)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rendererRef = renderers[i];
            if (rendererRef == null)
            {
                continue;
            }

            Material mat = rendererRef.material;
            Color color = originalColors[i];
            color.a = alpha;

            if (mat.HasProperty(baseColorProperty))
            {
                mat.SetColor(baseColorProperty, color);
            }

            if (mat.HasProperty(fallbackColorProperty))
            {
                mat.SetColor(fallbackColorProperty, color);
            }
        }
    }
}
