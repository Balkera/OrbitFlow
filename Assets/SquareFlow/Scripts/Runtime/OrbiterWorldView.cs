using System.Collections.Generic;
using TMPro;
using SquareFlow.Core;
using SquareFlow.UI;
using UnityEngine;

namespace SquareFlow.Runtime
{
    public sealed class OrbiterWorldView : MonoBehaviour
    {
        private readonly Dictionary<string, OrbiterView> active = new Dictionary<string, OrbiterView>();
        private readonly Dictionary<string, Color> activeColors = new Dictionary<string, Color>();
        private readonly Dictionary<string, LaunchState> launches = new Dictionary<string, LaunchState>();
        private readonly Queue<OrbiterView> inactive = new Queue<OrbiterView>();
        private readonly List<string> missing = new List<string>();

        private void Update()
        {
            float rotation = SquareFlowVisualMetrics.ActiveOrbiterAmmoParticleRotationDegreesPerSecond * Time.deltaTime;
            foreach (OrbiterView view in active.Values)
                view.ParticleRing.transform.Rotate(0f, 0f, rotation, Space.Self);
        }

        public void Refresh(List<ActiveOrbiter> orbiters, MobileWorldLayout world, SquareFlowTheme theme)
        {
            missing.Clear();
            foreach (string id in active.Keys)
                missing.Add(id);

            for (int i = 0; i < orbiters.Count; i++)
            {
                ActiveOrbiter orbiter = orbiters[i];
                missing.Remove(orbiter.Id);

                if (!active.TryGetValue(orbiter.Id, out OrbiterView view))
                {
                    view = TakeOrbiter(orbiter.Id);
                    active.Add(orbiter.Id, view);
                }

                Vector2 position = world.PathPosition(orbiter.Distance);
                position = LaunchPosition(orbiter.Id, position);
                float tokenSize = world.CellSize * SquareFlowVisualMetrics.ActiveOrbiterTokenScale;
                view.Root.SetActive(true);
                view.Root.transform.position = new Vector3(position.x, position.y, -0.25f);
                view.Root.transform.localScale = Vector3.one * SquareFlowVisualMetrics.ActiveOrbiterWorldScale;
                Color orbiterColor = ColorForShooter(orbiter.Color, orbiter.Wild, theme);
                Sprite tokenSprite = SquareFlowWorldSprites.OrbitForShooter(orbiter.Color, orbiter.Wild);
                bool usesTextureSprite = tokenSprite != SquareFlowWorldSprites.Circle;
                activeColors[orbiter.Id] = orbiterColor;
                view.Glow.color = ColorWithAlpha(orbiterColor, 0.64f);
                view.Token.sprite = tokenSprite;
                view.Token.color = usesTextureSprite ? Color.white : orbiterColor;
                RefreshAmmoParticles(view, orbiter.Ammo, world, orbiterColor);
                RefreshAmmoLabel(view, orbiter, world, tokenSize);
                view.Token.transform.localScale = Vector3.one * tokenSize;
                view.Glow.transform.localScale = Vector3.one * (world.CellSize * SquareFlowVisualMetrics.ActiveOrbiterGlowScale);
            }

            for (int i = 0; i < missing.Count; i++)
                Release(missing[i]);
        }

        public bool TryGetColor(string orbiterId, out Color color)
        {
            if (activeColors.TryGetValue(orbiterId, out color))
                return true;

            color = Color.white;
            return false;
        }

        public void RegisterLaunchSource(string orbiterId, Vector2 sourcePosition)
        {
            if (string.IsNullOrEmpty(orbiterId)) return;
            launches[orbiterId] = new LaunchState(sourcePosition, 0f);
        }

        public void Clear()
        {
            missing.Clear();
            foreach (string id in active.Keys)
                missing.Add(id);

            for (int i = 0; i < missing.Count; i++)
                Release(missing[i]);

            launches.Clear();
        }

        private OrbiterView TakeOrbiter(string id)
        {
            OrbiterView view = inactive.Count > 0 ? inactive.Dequeue() : CreateOrbiter();
            view.Root.name = "WorldOrbiter_" + id;
            view.ParticleRing.transform.localRotation = Quaternion.identity;
            return view;
        }

        private OrbiterView CreateOrbiter()
        {
            GameObject root = new GameObject("WorldOrbiter");
            root.transform.SetParent(transform, false);

            SpriteRenderer glow = CreateRenderer(root.transform, "Glow", SquareFlowWorldSprites.Glow, 5);
            SpriteRenderer token = CreateRenderer(root.transform, "Token", SquareFlowWorldSprites.Circle, 6);
            GameObject particleRing = CreateParticleRing(root.transform);
            TextMeshPro ammoLabel = CreateAmmoLabel(root.transform);
            return new OrbiterView(root, glow, token, particleRing, ammoLabel, new List<SpriteRenderer>());
        }

        private void Release(string id)
        {
            OrbiterView view = active[id];
            active.Remove(id);
            activeColors.Remove(id);
            launches.Remove(id);
            view.Root.SetActive(false);
            inactive.Enqueue(view);
        }

        private Vector2 LaunchPosition(string orbiterId, Vector2 target)
        {
            if (!launches.TryGetValue(orbiterId, out LaunchState launch))
                return target;

            float duration = Mathf.Max(0.01f, SquareFlowVisualMetrics.ActiveOrbiterLaunchDurationSeconds);
            float progress = Mathf.Clamp01(launch.Elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            Vector2 position = Vector2.LerpUnclamped(launch.SourcePosition, target, eased);

            launch.Elapsed += Time.deltaTime;
            if (launch.Elapsed >= duration)
                launches.Remove(orbiterId);
            else
                launches[orbiterId] = launch;

            return position;
        }

        private static SpriteRenderer CreateRenderer(Transform parent, string name, Sprite sprite, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static GameObject CreateParticleRing(Transform parent)
        {
            GameObject go = new GameObject("AmmoParticleRing");
            go.transform.SetParent(parent, false);
            return go;
        }

        private static SpriteRenderer CreateAmmoParticle(Transform parent)
        {
            SpriteRenderer renderer = CreateRenderer(parent, "AmmoParticle", SquareFlowWorldSprites.Circle, 8);
            renderer.sortingOrder = 8;
            return renderer;
        }

        private static TextMeshPro CreateAmmoLabel(Transform parent)
        {
            GameObject go = new GameObject("AmmoLabel");
            go.transform.SetParent(parent, false);
            TextMeshPro label = go.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            MeshRenderer renderer = label.GetComponent<MeshRenderer>();
            renderer.sortingOrder = 9;
            return label;
        }

        private static void RefreshAmmoParticles(OrbiterView view, int ammo, MobileWorldLayout world, Color shooterColor)
        {
            int count = Mathf.Max(0, ammo);
            while (view.AmmoParticles.Count < count)
                view.AmmoParticles.Add(CreateAmmoParticle(view.ParticleRing.transform));

            view.ParticleRing.SetActive(count > 0);
            float radius = world.CellSize * SquareFlowVisualMetrics.ActiveOrbiterAmmoParticleOrbitRadiusScale;
            float size = SquareFlowVisualMetrics.ActiveOrbiterAmmoParticleScale;

            for (int i = 0; i < view.AmmoParticles.Count; i++)
            {
                SpriteRenderer particle = view.AmmoParticles[i];
                bool visible = i < count;
                particle.gameObject.SetActive(visible);
                if (!visible) continue;

                float angle = Mathf.PI * 2f * i / count;
                particle.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -0.12f);
                particle.transform.localScale = Vector3.one * size;
                particle.color = shooterColor;
            }
        }

        private static void RefreshAmmoLabel(OrbiterView view, ActiveOrbiter orbiter, MobileWorldLayout world, float tokenSize)
        {
            int count = Mathf.Max(0, orbiter.Ammo);
            view.AmmoLabel.text = count.ToString();
            view.AmmoLabel.transform.localPosition = new Vector3(0f, 0f, -0.16f);
            view.AmmoLabel.fontSize = SquareFlowVisualMetrics.ActiveOrbiterAmmoLabelFontSize;
            view.AmmoLabel.rectTransform.sizeDelta = Vector2.one * tokenSize * 1.18f;
            view.AmmoLabel.color = AmmoLabelColor(orbiter.Color, orbiter.Wild);
        }

        private static Color ColorForShooter(FlowColor color, bool wild, SquareFlowTheme theme)
        {
            if (wild || color == FlowColor.Wild) return theme.Wild;

            switch (color)
            {
                case FlowColor.Blue:
                    return theme.Blue;
                case FlowColor.Yellow:
                    return theme.Yellow;
                case FlowColor.Green:
                    return theme.Green;
                default:
                    return theme.Red;
            }
        }

        private static Color ColorWithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static Color AmmoLabelColor(FlowColor color, bool wild)
        {
            return wild || color == FlowColor.Wild || color == FlowColor.Yellow
                ? new Color32(26, 23, 64, 255)
                : Color.white;
        }

        private readonly struct OrbiterView
        {
            public OrbiterView(GameObject root, SpriteRenderer glow, SpriteRenderer token, GameObject particleRing, TextMeshPro ammoLabel, List<SpriteRenderer> ammoParticles)
            {
                Root = root;
                Glow = glow;
                Token = token;
                ParticleRing = particleRing;
                AmmoLabel = ammoLabel;
                AmmoParticles = ammoParticles;
            }

            public GameObject Root { get; }
            public SpriteRenderer Glow { get; }
            public SpriteRenderer Token { get; }
            public GameObject ParticleRing { get; }
            public TextMeshPro AmmoLabel { get; }
            public List<SpriteRenderer> AmmoParticles { get; }
        }

        private struct LaunchState
        {
            public LaunchState(Vector2 sourcePosition, float elapsed)
            {
                SourcePosition = sourcePosition;
                Elapsed = elapsed;
            }

            public Vector2 SourcePosition { get; }
            public float Elapsed { get; set; }
        }
    }
}
