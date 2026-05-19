using System.Collections.Generic;
using SquareFlow.Core;
using SquareFlow.UI;
using UnityEngine;

namespace SquareFlow.Runtime
{
    public sealed class OrbiterWorldView : MonoBehaviour
    {
        private readonly Dictionary<string, OrbiterView> active = new Dictionary<string, OrbiterView>();
        private readonly List<string> missing = new List<string>();

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
                    view = CreateOrbiter(orbiter.Id);
                    active.Add(orbiter.Id, view);
                }

                Vector2 position = world.PathPosition(orbiter.Distance);
                view.Root.SetActive(true);
                view.Root.transform.position = new Vector3(position.x, position.y, -0.25f);
                view.Glow.color = ColorWithAlpha(ColorForShooter(orbiter.Color, orbiter.Wild, theme), 0.64f);
                view.Token.color = ColorForShooter(orbiter.Color, orbiter.Wild, theme);
                float tokenSize = world.CellSize * SquareFlowVisualMetrics.ActiveOrbiterTokenScale;
                view.Token.transform.localScale = Vector3.one * tokenSize;
                view.Glow.transform.localScale = Vector3.one * (world.CellSize * SquareFlowVisualMetrics.ActiveOrbiterGlowScale);
            }

            for (int i = 0; i < missing.Count; i++)
                active[missing[i]].Root.SetActive(false);
        }

        public bool TryGetColor(string orbiterId, out Color color)
        {
            if (active.TryGetValue(orbiterId, out OrbiterView view))
            {
                color = view.Token.color;
                return true;
            }

            color = Color.white;
            return false;
        }

        public void Clear()
        {
            foreach (OrbiterView view in active.Values)
                view.Root.SetActive(false);
        }

        private OrbiterView CreateOrbiter(string id)
        {
            GameObject root = new GameObject("WorldOrbiter_" + id);
            root.transform.SetParent(transform, false);

            SpriteRenderer glow = CreateRenderer(root.transform, "Glow", SquareFlowWorldSprites.Glow, 5);
            SpriteRenderer token = CreateRenderer(root.transform, "Token", SquareFlowWorldSprites.Circle, 6);
            return new OrbiterView(root, glow, token);
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

        private readonly struct OrbiterView
        {
            public OrbiterView(GameObject root, SpriteRenderer glow, SpriteRenderer token)
            {
                Root = root;
                Glow = glow;
                Token = token;
            }

            public GameObject Root { get; }
            public SpriteRenderer Glow { get; }
            public SpriteRenderer Token { get; }
        }
    }
}
