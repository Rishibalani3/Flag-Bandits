using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FlagBandits.UI
{
    public class CrosshairUI : MonoBehaviour
    {
        public static CrosshairUI Instance { get; private set; }

        [Header("Theme")]
        [SerializeField] private UITheme theme;

        [Header("Crosshair Elements")]
        [SerializeField] private RectTransform crosshairRoot;
        [SerializeField] private Image centerDot;
        [SerializeField] private Image[] reticleBars;
        [SerializeField] private Image[] hitMarkerBars;

        private GameObject canvasObj;
        private Coroutine hitMarkerCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (theme == null)
            {
                theme = Resources.Load<UITheme>("UITheme");
            }

            EnsureCrosshairHierarchy();
        }

        private void EnsureCrosshairHierarchy()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                canvasObj = new GameObject("HUDCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                canvasObj = canvas.gameObject;
            }

            if (crosshairRoot == null)
            {
                GameObject rootObj = new GameObject("CrosshairRoot", typeof(RectTransform));
                crosshairRoot = rootObj.GetComponent<RectTransform>();
                crosshairRoot.SetParent(canvasObj.transform, false);
                crosshairRoot.anchorMin = new Vector2(0.5f, 0.5f);
                crosshairRoot.anchorMax = new Vector2(0.5f, 0.5f);
                crosshairRoot.pivot = new Vector2(0.5f, 0.5f);
                crosshairRoot.anchoredPosition = Vector2.zero;
                crosshairRoot.sizeDelta = new Vector2(40f, 40f);

                Color crosshairCol = theme != null ? theme.crosshairColor : new Color(1f, 1f, 1f, 0.85f);

                // Center dot
                centerDot = CreateImage(crosshairRoot, "CenterDot", new Vector2(4f, 4f), Vector2.zero, crosshairCol);

                // 4 Reticle bars (+)
                reticleBars = new Image[4];
                float barLength = 10f;
                float barThickness = 2.5f;
                float gap = 6f;

                reticleBars[0] = CreateImage(crosshairRoot, "TopBar", new Vector2(barThickness, barLength), new Vector2(0f, gap + barLength * 0.5f), crosshairCol);
                reticleBars[1] = CreateImage(crosshairRoot, "BottomBar", new Vector2(barThickness, barLength), new Vector2(0f, -(gap + barLength * 0.5f)), crosshairCol);
                reticleBars[2] = CreateImage(crosshairRoot, "LeftBar", new Vector2(barLength, barThickness), new Vector2(-(gap + barLength * 0.5f), 0f), crosshairCol);
                reticleBars[3] = CreateImage(crosshairRoot, "RightBar", new Vector2(barLength, barThickness), new Vector2(gap + barLength * 0.5f, 0f), crosshairCol);

                // 4 Hit marker diagonal bars (X)
                hitMarkerBars = new Image[4];
                float hitLength = 12f;
                float hitThickness = 2.5f;
                float hitOffset = 9f;

                hitMarkerBars[0] = CreateDiagonalImage(crosshairRoot, "Hit_TR", hitThickness, hitLength, new Vector2(hitOffset, hitOffset), -45f);
                hitMarkerBars[1] = CreateDiagonalImage(crosshairRoot, "Hit_TL", hitThickness, hitLength, new Vector2(-hitOffset, hitOffset), 45f);
                hitMarkerBars[2] = CreateDiagonalImage(crosshairRoot, "Hit_BR", hitThickness, hitLength, new Vector2(hitOffset, -hitOffset), 45f);
                hitMarkerBars[3] = CreateDiagonalImage(crosshairRoot, "Hit_BL", hitThickness, hitLength, new Vector2(-hitOffset, -hitOffset), -45f);

                SetHitMarkerVisible(false);
            }
        }

        private Image CreateImage(RectTransform parent, string name, Vector2 size, Vector2 position, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image img = obj.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private Image CreateDiagonalImage(RectTransform parent, string name, float thickness, float length, Vector2 position, float rotationZ)
        {
            Image img = CreateImage(parent, name, new Vector2(thickness, length), position, Color.white);
            img.rectTransform.localEulerAngles = new Vector3(0f, 0f, rotationZ);
            return img;
        }

        public static void TriggerHitMarker()
        {
            if (Instance != null)
            {
                Instance.ShowHitMarker();
            }
        }

        public void ShowHitMarker()
        {
            if (hitMarkerCoroutine != null)
            {
                StopCoroutine(hitMarkerCoroutine);
            }
            hitMarkerCoroutine = StartCoroutine(HitMarkerRoutine());
        }

        private IEnumerator HitMarkerRoutine()
        {
            SetHitMarkerVisible(true);
            yield return new WaitForSeconds(0.12f);
            SetHitMarkerVisible(false);
            hitMarkerCoroutine = null;
        }

        private void SetHitMarkerVisible(bool visible)
        {
            if (hitMarkerBars == null) return;
            Color markerColor = theme != null ? theme.hitMarkerColor : Color.white;
            markerColor.a = visible ? 1f : 0f;

            for (int i = 0; i < hitMarkerBars.Length; i++)
            {
                if (hitMarkerBars[i] != null)
                {
                    hitMarkerBars[i].color = markerColor;
                }
            }
        }

        private void OnDestroy()
        {
            if (canvasObj != null)
            {
                Destroy(canvasObj);
            }
        }
    }
}
