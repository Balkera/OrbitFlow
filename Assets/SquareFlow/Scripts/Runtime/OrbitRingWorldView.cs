using SquareFlow.Core;
using SquareFlow.UI;
using UnityEngine;

namespace SquareFlow.Runtime
{
    public sealed class OrbitRingWorldView : MonoBehaviour
    {
        private LineRenderer ring;
        private Material ringMaterial;
        private static readonly Color32 OrbitRingColor = new Color32(96, 247, 255, 150);

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
            ring.startWidth = Mathf.Max(0.012f, board.Cell * SquareFlowVisualMetrics.OrbitRingThicknessScale * world.WorldUnitsPerLayoutPixel);
            ring.endWidth = ring.startWidth;
            ring.startColor = OrbitRingColor;
            ring.endColor = ring.startColor;
            ring.numCapVertices = 8;
            ring.numCornerVertices = 8;
            ring.sortingOrder = -1;

            for (int i = 0; i < count; i++)
            {
                Vector2 position = RoundedOrbitPosition(world, i, count);
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

        private static Vector2 RoundedOrbitPosition(MobileWorldLayout world, int index, int count)
        {
            Rect bounds = world.OrbitBounds;
            float radius = Mathf.Min(bounds.width, bounds.height) * 0.16f;
            radius = Mathf.Min(radius, bounds.width * 0.5f, bounds.height * 0.5f);
            float straightWidth = Mathf.Max(0f, bounds.width - radius * 2f);
            float straightHeight = Mathf.Max(0f, bounds.height - radius * 2f);
            float perimeter = straightWidth * 2f + straightHeight * 2f + Mathf.PI * 2f * radius;
            float distance = perimeter * index / count;

            return PointOnRoundedRect(bounds, radius, straightWidth, straightHeight, distance);
        }

        private static Vector2 PointOnRoundedRect(Rect bounds, float radius, float straightWidth, float straightHeight, float distance)
        {
            float arc = Mathf.PI * 0.5f * radius;
            float xMin = bounds.xMin;
            float xMax = bounds.xMax;
            float yMin = bounds.yMin;
            float yMax = bounds.yMax;

            if (distance < straightWidth)
                return new Vector2(xMin + radius + distance, yMax);
            distance -= straightWidth;

            if (distance < arc)
                return ArcPoint(xMax - radius, yMax - radius, radius, 90f, distance / arc);
            distance -= arc;

            if (distance < straightHeight)
                return new Vector2(xMax, yMax - radius - distance);
            distance -= straightHeight;

            if (distance < arc)
                return ArcPoint(xMax - radius, yMin + radius, radius, 0f, distance / arc);
            distance -= arc;

            if (distance < straightWidth)
                return new Vector2(xMax - radius - distance, yMin);
            distance -= straightWidth;

            if (distance < arc)
                return ArcPoint(xMin + radius, yMin + radius, radius, 270f, distance / arc);
            distance -= arc;

            if (distance < straightHeight)
                return new Vector2(xMin, yMin + radius + distance);
            distance -= straightHeight;

            return ArcPoint(xMin + radius, yMax - radius, radius, 180f, distance / arc);
        }

        private static Vector2 ArcPoint(float centerX, float centerY, float radius, float startDegrees, float progress)
        {
            float angle = (startDegrees - progress * 90f) * Mathf.Deg2Rad;
            return new Vector2(centerX + Mathf.Cos(angle) * radius, centerY + Mathf.Sin(angle) * radius);
        }
    }
}
