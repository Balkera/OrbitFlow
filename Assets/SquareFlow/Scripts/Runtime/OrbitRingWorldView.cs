using SquareFlow.Core;
using SquareFlow.UI;
using UnityEngine;

namespace SquareFlow.Runtime
{
    public sealed class OrbitRingWorldView : MonoBehaviour
    {
        private LineRenderer ring;
        private Material ringMaterial;

        private void OnDestroy()
        {
            if (ringMaterial == null) return;

            if (Application.isPlaying)
                Destroy(ringMaterial);
            else
                DestroyImmediate(ringMaterial);
        }

        public void Bind(BoardLayout board, MobileWorldLayout world, SquareFlowTheme theme)
        {
            if (board == null || !world.IsValid)
            {
                if (ring != null)
                    ring.gameObject.SetActive(false);
                return;
            }

            EnsureRing();

            int count = SquareFlowVisualMetrics.OrbitRingPointCount;
            ring.gameObject.SetActive(true);
            ring.positionCount = count;
            ring.loop = true;
            ring.useWorldSpace = true;
            ring.startWidth = Mathf.Max(0.035f, board.Cell * SquareFlowVisualMetrics.OrbitRingThicknessScale * world.WorldUnitsPerLayoutPixel);
            ring.endWidth = ring.startWidth;
            ring.startColor = ColorWithAlpha(theme.Score, 0.86f);
            ring.endColor = ring.startColor;
            ring.numCapVertices = 6;
            ring.numCornerVertices = 6;
            ring.sortingOrder = -1;

            for (int i = 0; i < count; i++)
            {
                float distance = board.Perimeter * i / count;
                Vector2 position = world.PathPosition(distance);
                ring.SetPosition(i, new Vector3(position.x, position.y, 0.2f));
            }
        }

        public void Clear()
        {
            if (ring != null)
                ring.gameObject.SetActive(false);
        }

        private void EnsureRing()
        {
            if (ring != null) return;

            GameObject go = new GameObject("OrbitRing");
            go.transform.SetParent(transform, false);
            ring = go.AddComponent<LineRenderer>();
            ringMaterial = new Material(Shader.Find("Sprites/Default"));
            ringMaterial.name = "SquareFlowOrbitRingMaterial";
            ring.sharedMaterial = ringMaterial;
        }

        private static Color ColorWithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
