using UnityEngine;
using FlagBandits.Gameplay;

namespace FlagBandits.UI
{
    /// <summary>
    /// Lightweight top-center OnGUI status strip displaying Blue and Red flag states
    /// for Phase 3 gameplay verification before Phase 8 uGUI replacement.
    /// </summary>
    public class CtfDebugHud : MonoBehaviour
    {
        [SerializeField] private bool showHud = true;

        private void OnGUI()
        {
            if (!showHud) return;

            var blueFlag = NetworkFlag.FindFlag(Team.TeamA);
            var redFlag = NetworkFlag.FindFlag(Team.TeamB);
            if (blueFlag == null && redFlag == null) return;

            float width = Mathf.Clamp(Screen.width * 0.42f, 340f, 560f);
            float height = 38f;
            float x = (Screen.width - width) * 0.5f;
            float y = 12f;

            GUI.color = new Color(0.08f, 0.1f, 0.18f, 0.85f);
            GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);

            float halfWidth = width * 0.5f;
            DrawFlagBadge(new Rect(x + 4f, y + 4f, halfWidth - 6f, height - 8f), "BLUE FLAG", blueFlag, new Color(0.2f, 0.5f, 1f, 0.9f));
            DrawFlagBadge(new Rect(x + halfWidth + 2f, y + 4f, halfWidth - 6f, height - 8f), "RED FLAG", redFlag, new Color(0.95f, 0.25f, 0.25f, 0.9f));
            GUI.color = Color.white;
        }

        private static void DrawFlagBadge(Rect rect, string title, NetworkFlag flag, Color accentColor)
        {
            GUI.color = new Color(accentColor.r * 0.35f, accentColor.g * 0.35f, accentColor.b * 0.35f, 0.9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            GUI.color = Color.white;
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 13
            };

            string statusText = FormatFlagStatus(flag);
            GUI.Label(rect, $"{title}: {statusText}", style);
        }

        private static string FormatFlagStatus(NetworkFlag flag)
        {
            if (flag == null || flag.Object == null || !flag.Object.IsValid) return "---";

            switch (flag.State)
            {
                case FlagState.AtBase:
                    return "AT BASE";
                case FlagState.Carried:
                    return $"CARRIED ({flag.CarrierPlayer})";
                case FlagState.Dropped:
                    return $"DROPPED ({flag.GetRemainingReturnTime():F1}s)";
                default:
                    return flag.State.ToString();
            }
        }
    }
}
