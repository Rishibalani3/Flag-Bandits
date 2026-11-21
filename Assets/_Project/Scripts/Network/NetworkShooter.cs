using UnityEngine;
using Fusion;

namespace FlagBandits.Network
{
    /// <summary>
    /// Networked shooting. The local PlayerShooter handles input and local raycasting.
    /// This component broadcasts visual effects (tracers, muzzle flash) to remote clients
    /// and sends damage RPCs to victims for server-authoritative validation.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkShooter : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform firePoint;

        // Networked tick counter — incremented each shot so remotes detect new shots.
        [Networked] public int ShotCount { get; set; }
        [Networked] public Vector3 LastHitPoint { get; set; }
        [Networked] public Vector3 LastFireOrigin { get; set; }

        private int localShotCount;
        private Material tracerMaterial;
        private Material flashMaterial;

        private void Awake()
        {
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");

            tracerMaterial = new Material(unlitShader);
            tracerMaterial.color = new Color(1f, 0.95f, 0.3f, 1f);

            flashMaterial = new Material(unlitShader);
            flashMaterial.color = new Color(1f, 0.9f, 0.5f, 1f);
        }

        public override void Spawned()
        {
            localShotCount = ShotCount;

            if (firePoint == null)
            {
                // Find the DefaultFirePoint child created by PlayerShooter
                var fp = transform.Find("DefaultFirePoint");
                if (fp != null) firePoint = fp;
            }
        }

        public override void Render()
        {
            // Detect new shots from remote players by watching ShotCount changes.
            if (!HasStateAuthority && ShotCount != localShotCount)
            {
                // A new shot was fired by the remote owner — show visuals.
                SpawnRemoteTracer(LastFireOrigin, LastHitPoint);
                SpawnRemoteMuzzleFlash(LastFireOrigin, (LastHitPoint - LastFireOrigin).normalized);
                localShotCount = ShotCount;
            }
        }

        /// <summary>
        /// Called by the LOCAL PlayerShooter when the local player fires.
        /// Updates networked state so remotes can render visuals.
        /// </summary>
        public void OnLocalShot(Vector3 origin, Vector3 hitPoint)
        {
            if (!HasStateAuthority) return;

            LastFireOrigin = origin;
            LastHitPoint = hitPoint;
            ShotCount++;
        }

        /// <summary>
        /// Send a damage RPC to the victim. The victim's client validates and applies.
        /// In Shared Mode, any client can call RPCs on any NetworkObject.
        /// </summary>
        public void SendDamageToVictim(NetworkHealth victimHealth, float damage, Vector3 shooterPos, float maxRange)
        {
            if (victimHealth == null) return;

            // In Shared Mode, we call an RPC that runs on the victim's State Authority.
            victimHealth.RPC_ReceiveDamage(damage, shooterPos, maxRange);
        }

        // ---- Remote Visual Effects ----

        private void SpawnRemoteTracer(Vector3 start, Vector3 end)
        {
            GameObject lineObj = new GameObject("RemoteTracer");
            var tracer = lineObj.AddComponent<LineRenderer>();
            tracer.material = tracerMaterial;
            tracer.startWidth = 0.12f;
            tracer.endWidth = 0.05f;
            tracer.startColor = new Color(1f, 0.95f, 0.4f, 1f);
            tracer.endColor = new Color(1f, 0.55f, 0.1f, 0.8f);
            tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tracer.receiveShadows = false;
            tracer.positionCount = 2;
            tracer.SetPosition(0, start);
            tracer.SetPosition(1, end);

            Destroy(lineObj, 0.05f);
        }

        private void SpawnRemoteMuzzleFlash(Vector3 position, Vector3 direction)
        {
            GameObject flashObj = new GameObject("RemoteMuzzleFlash");
            flashObj.transform.position = position;
            flashObj.transform.rotation = Quaternion.LookRotation(direction);

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "FlashVisual";
            quad.transform.SetParent(flashObj.transform, false);
            quad.transform.localScale = Vector3.one * 0.3f;
            quad.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            Destroy(quad.GetComponent<Collider>());
            var quadRenderer = quad.GetComponent<MeshRenderer>();
            if (quadRenderer != null && flashMaterial != null)
            {
                quadRenderer.material = flashMaterial;
                quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            Light flashLight = flashObj.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = new Color(1f, 0.85f, 0.4f);
            flashLight.range = 3.5f;
            flashLight.intensity = 3.0f;
            flashLight.shadows = LightShadows.None;

            Destroy(flashObj, 0.04f);
        }
    }
}
