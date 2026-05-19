using System.Collections.Generic;
using SquareFlow.Core;
using SquareFlow.UI;
using UnityEngine;

namespace SquareFlow.Runtime
{
    public sealed class OrbitRingWorldView : MonoBehaviour
    {
        private readonly List<SpriteRenderer> segments = new List<SpriteRenderer>();

        public void Bind(BoardLayout board, MobileWorldLayout world, SquareFlowTheme theme)
        {
            if (board == null || !world.IsValid)
            {
                SetActiveCount(0);
                return;
            }

            float segmentLengthLayout = Mathf.Max(12f, board.Cell * SquareFlowVisualMetrics.OrbitLineSegmentLengthScale);
            float spacingLayout = segmentLengthLayout * SquareFlowVisualMetrics.OrbitLineSegmentSpacingMultiplier;
            float segmentLengthWorld = segmentLengthLayout * world.WorldUnitsPerLayoutPixel;
            int count = Mathf.Max(96, Mathf.CeilToInt(board.Perimeter / Mathf.Max(1f, spacingLayout)));
            SetActiveCount(count);

            for (int i = 0; i < count; i++)
            {
                float distance = board.Perimeter * i / count;
                Vector2 position = world.PathPosition(distance);
                Vector2 before = world.PathPosition(distance - spacingLayout * 0.45f);
                Vector2 after = world.PathPosition(distance + spacingLayout * 0.45f);
                float angle = Mathf.Atan2(after.y - before.y, after.x - before.x) * Mathf.Rad2Deg;

                SpriteRenderer renderer = segments[i];
                renderer.color = ColorWithAlpha(theme.Score, 0.62f);
                renderer.transform.position = new Vector3(position.x, position.y, 0.2f);
                renderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                renderer.transform.localScale = new Vector3(segmentLengthWorld, Mathf.Max(0.035f, board.Cell * SquareFlowVisualMetrics.OrbitLineSegmentThicknessScale * world.WorldUnitsPerLayoutPixel), 1f);
            }
        }

        public void Clear()
        {
            SetActiveCount(0);
        }

        private void SetActiveCount(int count)
        {
            while (segments.Count < count)
                segments.Add(CreateSegment());

            for (int i = 0; i < segments.Count; i++)
                segments[i].gameObject.SetActive(i < count);
        }

        private SpriteRenderer CreateSegment()
        {
            GameObject go = new GameObject("OrbitSegment");
            go.transform.SetParent(transform, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = SquareFlowWorldSprites.Square;
            renderer.sortingOrder = -1;
            return renderer;
        }

        private static Color ColorWithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
