using UnityEngine;

namespace FlagBandits.UI
{
    [CreateAssetMenu(fileName = "UITheme", menuName = "Flag Bandits/UI Theme")]
    public class UITheme : ScriptableObject
    {
        [Header("Background & Panels")]
        public Color bg = new Color32(0x1B, 0x1F, 0x3B, 0xFF);
        public Color panel = new Color32(0x2A, 0x2F, 0x5A, 0xFF);
        public Color panelLight = new Color32(0x3A, 0x40, 0x75, 0xFF);

        [Header("Accent & Brand")]
        public Color accent = new Color32(0xFF, 0xC9, 0x3C, 0xFF);

        [Header("Text")]
        public Color text = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        public Color textDim = new Color32(0xB8, 0xC0, 0xE0, 0xFF);

        [Header("Feedback")]
        public Color success = new Color32(0x3D, 0xDC, 0x84, 0xFF);
        public Color danger = new Color32(0xFF, 0x4D, 0x5E, 0xFF);

        [Header("Teams")]
        public Color teamBlue = new Color32(0x2D, 0x8C, 0xFF, 0xFF);
        public Color teamOrange = new Color32(0xFF, 0x8A, 0x1F, 0xFF);

        [Header("Crosshair")]
        public Color crosshairColor = new Color32(0xFF, 0xFF, 0xFF, 0xDD);
        public Color hitMarkerColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        public Color killMarkerColor = new Color32(0xFF, 0x4D, 0x5E, 0xFF);
    }
}
