using System.Collections;
using UnityEngine;

namespace FlagBandits.Combat
{
    [RequireComponent(typeof(Health))]
    public class TargetDummy : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color damageFlashColor = Color.red;
        [SerializeField] private float flashDuration = 0.12f;

        private Health health;
        private Color originalColor = Color.white;
        private Material targetMaterial;
        private Coroutine flashCoroutine;

        private void Awake()
        {
            health = GetComponent<Health>();

            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            if (targetRenderer != null)
            {
                targetMaterial = targetRenderer.material;
                if (targetMaterial.HasProperty("_BaseColor"))
                {
                    originalColor = targetMaterial.GetColor("_BaseColor");
                }
                else if (targetMaterial.HasProperty("_Color"))
                {
                    originalColor = targetMaterial.color;
                }
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDamaged += HandleDamaged;
                health.OnDeath += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDamaged -= HandleDamaged;
                health.OnDeath -= HandleDeath;
            }
        }

        private void HandleDamaged(float amount, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (targetMaterial != null)
            {
                if (flashCoroutine != null)
                {
                    StopCoroutine(flashCoroutine);
                }
                flashCoroutine = StartCoroutine(FlashRoutine());
            }
        }

        private void HandleDeath(Vector3 deathPoint)
        {
            // Reset after 1.5 seconds for easy repeat testing
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            SetColor(damageFlashColor);
            yield return new WaitForSeconds(flashDuration);
            SetColor(originalColor);
            flashCoroutine = null;
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(1.5f);
            health.ResetHealth();
            SetColor(originalColor);
        }

        private void SetColor(Color c)
        {
            if (targetMaterial == null) return;
            if (targetMaterial.HasProperty("_BaseColor"))
            {
                targetMaterial.SetColor("_BaseColor", c);
            }
            else if (targetMaterial.HasProperty("_Color"))
            {
                targetMaterial.color = c;
            }
        }
    }
}
