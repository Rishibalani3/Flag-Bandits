using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using FlagBandits.Player;
using FlagBandits.Network;
using FlagBandits.UI;

namespace FlagBandits.Combat
{
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerShooter : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private WeaponSettings weaponSettings;

        [Header("References")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private LineRenderer tracerPrefab;

        private PlayerInput playerInput;
        private PlayerMovement playerMovement;
        private NetworkShooter networkShooter;
        private InputAction attackAction;
        private Camera mainCamera;
        private float nextFireTime;
        private Material tracerMaterial;
        private Material flashMaterial;

        public WeaponSettings CurrentWeapon => weaponSettings;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            playerMovement = GetComponent<PlayerMovement>();
            networkShooter = GetComponent<NetworkShooter>();
            mainCamera = Camera.main;

            if (playerInput != null && playerInput.actions != null)
            {
                attackAction = playerInput.actions["Attack"];
            }

            if (firePoint == null)
            {
                GameObject defaultFirePoint = new GameObject("DefaultFirePoint");
                defaultFirePoint.transform.SetParent(transform);
                defaultFirePoint.transform.localPosition = new Vector3(0.35f, 1.3f, 0.6f);
                firePoint = defaultFirePoint.transform;
            }

            // Create shared material for high-visibility unlit tracers
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
            tracerMaterial = new Material(unlitShader);
            tracerMaterial.color = new Color(1f, 0.95f, 0.3f, 1f);

            flashMaterial = new Material(unlitShader);
            flashMaterial.color = new Color(1f, 0.9f, 0.5f, 1f);
        }

        private void Update()
        {
            if (weaponSettings == null || attackAction == null)
            {
                return;
            }

            bool wantsToFire = weaponSettings.isAutomatic
                ? attackAction.IsPressed()
                : attackAction.WasPressedThisFrame();

            if (wantsToFire && Time.time >= nextFireTime)
            {
                Shoot();
            }
        }

        private void Shoot()
        {
            nextFireTime = Time.time + (1f / Mathf.Max(0.01f, weaponSettings.fireRate));

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // 1. Immediately turn player body to face camera aim direction
            if (playerMovement != null)
            {
                playerMovement.FaceAimDirection(true);
            }
            else if (mainCamera != null)
            {
                Vector3 flatCamForward = mainCamera.transform.forward;
                flatCamForward.y = 0f;
                if (flatCamForward.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(flatCamForward, Vector3.up);
                }
            }

            Vector3 startPos = firePoint.position;
            Vector3 aimTarget;

            // 2. Raycast from camera center (through crosshair) into the world
            if (mainCamera != null)
            {
                Ray camRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                if (TryGetFirstHitNotSelf(camRay, weaponSettings.maxRange, out RaycastHit camHit))
                {
                    aimTarget = camHit.point;
                }
                else
                {
                    aimTarget = camRay.GetPoint(weaponSettings.maxRange);
                }
            }
            else
            {
                aimTarget = startPos + transform.forward * weaponSettings.maxRange;
            }

            // 3. Fire from gun muzzle toward aimTarget
            Vector3 shootDirection = (aimTarget - startPos).normalized;
            Vector3 hitPoint = aimTarget;

            Ray gunRay = new Ray(startPos, shootDirection);
            if (TryGetFirstHitNotSelf(gunRay, weaponSettings.maxRange, out RaycastHit gunHit))
            {
                hitPoint = gunHit.point;

                IDamageable damageable = gunHit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    // Try networked damage path first (shooter → victim RPC).
                    NetworkHealth victimNetHealth = gunHit.collider.GetComponentInParent<NetworkHealth>();
                    if (victimNetHealth != null && networkShooter != null)
                    {
                        networkShooter.SendDamageToVictim(
                            victimNetHealth,
                            weaponSettings.damage,
                            transform.position,
                            weaponSettings.maxRange
                        );
                    }
                    else
                    {
                        // Offline fallback — direct damage.
                        damageable.TakeDamage(weaponSettings.damage, gunHit.point, gunHit.normal);
                    }
                    CrosshairUI.TriggerHitMarker();
                }

                Rigidbody rb = gunHit.collider.attachedRigidbody;
                if (rb != null && !rb.isKinematic)
                {
                    rb.AddForceAtPosition(shootDirection * weaponSettings.impactForce, gunHit.point, ForceMode.Impulse);
                }
            }

            // 4. Muzzle flash effect at gun barrel
            SpawnMuzzleFlash(startPos, shootDirection);

            // 5. High-visibility bullet tracer from muzzle to hitPoint
            float duration = weaponSettings.tracerDuration > 0f ? weaponSettings.tracerDuration : 0.05f;
            StartCoroutine(SpawnTracerRoutine(startPos, hitPoint, duration));

            // 6. Notify NetworkShooter so remote clients see the shot.
            if (networkShooter != null)
            {
                networkShooter.OnLocalShot(startPos, hitPoint);
            }
        }

        private void SpawnMuzzleFlash(Vector3 position, Vector3 direction)
        {
            GameObject flashObj = new GameObject("MuzzleFlash");
            flashObj.transform.position = position;
            flashObj.transform.rotation = Quaternion.LookRotation(direction);

            // Visual flash quad
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "FlashVisual";
            quad.transform.SetParent(flashObj.transform, false);
            quad.transform.localScale = Vector3.one * 0.3f;
            quad.transform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));

            Destroy(quad.GetComponent<Collider>());
            var quadRenderer = quad.GetComponent<MeshRenderer>();
            if (quadRenderer != null && flashMaterial != null)
            {
                quadRenderer.material = flashMaterial;
                quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            // Small dynamic point light for instant illumination
            Light flashLight = flashObj.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = new Color(1f, 0.85f, 0.4f);
            flashLight.range = 3.5f;
            flashLight.intensity = 3.0f;
            flashLight.shadows = LightShadows.None;

            Destroy(flashObj, 0.04f);
        }

        private IEnumerator SpawnTracerRoutine(Vector3 start, Vector3 end, float duration)
        {
            LineRenderer tracer;
            if (tracerPrefab != null)
            {
                tracer = Instantiate(tracerPrefab, start, Quaternion.identity);
            }
            else
            {
                GameObject lineObj = new GameObject("BulletTracer");
                tracer = lineObj.AddComponent<LineRenderer>();
                tracer.material = tracerMaterial;
                tracer.startWidth = 0.12f;
                tracer.endWidth = 0.05f;
                tracer.startColor = new Color(1f, 0.95f, 0.4f, 1f);
                tracer.endColor = new Color(1f, 0.55f, 0.1f, 0.8f);
                tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tracer.receiveShadows = false;
            }

            tracer.positionCount = 2;
            tracer.SetPosition(0, start);
            tracer.SetPosition(1, end);

            yield return new WaitForSeconds(duration);

            if (tracer != null)
            {
                Destroy(tracer.gameObject);
            }
        }

        private bool TryGetFirstHitNotSelf(Ray ray, float maxDistance, out RaycastHit closestHit)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, weaponSettings.hitLayers);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            Transform rootTransform = transform.root;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform.root != rootTransform)
                {
                    closestHit = hits[i];
                    return true;
                }
            }

            closestHit = default;
            return false;
        }

        public void SetWeaponSettings(WeaponSettings newSettings)
        {
            weaponSettings = newSettings;
        }
    }
}
