using UnityEngine;

namespace FlagBandits.Gameplay
{
    /// <summary>
    /// Identifies a team's base territory, holding the flag pedestal and spawn point.
    /// Used for CTF flag capture triggers, flag return pedestals, and team spawn anchoring.
    /// </summary>
    public class TeamBaseZone : MonoBehaviour
    {
        [Header("Team Identification")]
        [SerializeField] private Team team = Team.TeamA;

        [Header("Pedestal & Spawn References")]
        [SerializeField] private Transform flagPedestal;
        [SerializeField] private Transform spawnPoint;

        public Team Team => team;
        public Transform FlagPedestal => flagPedestal != null ? flagPedestal : transform;
        public Transform SpawnPoint => spawnPoint != null ? spawnPoint : transform;

        private void OnDrawGizmos()
        {
            Gizmos.color = team == Team.TeamA
                ? new Color(0.2f, 0.5f, 1f, 0.35f)
                : new Color(1f, 0.25f, 0.25f, 0.35f);
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }
    }
}
