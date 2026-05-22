using System.Collections;
using System.Collections.Generic;
using SquareFlow.UI;
using UnityEngine;

namespace SquareFlow.Runtime
{
    public sealed class WorldEffectsController : MonoBehaviour
    {
        public const int MaxConcurrentShots = 8;
        public const int MaxLinePoolSize = MaxConcurrentShots;
        public const int MaxCirclePoolSize = MaxConcurrentShots * 2;
        public const int MaxEffectChildCount = MaxLinePoolSize + MaxCirclePoolSize;

        private readonly Queue<SpriteRenderer> linePool = new Queue<SpriteRenderer>();
        private readonly Queue<SpriteRenderer> circlePool = new Queue<SpriteRenderer>();
        private readonly List<SpriteRenderer> clearBuffer = new List<SpriteRenderer>();
        private int activeShotCount;

        public void PlayShot(Vector2 start, Vector2 end, Color color, bool heavyImpact)
        {
            if (!gameObject.activeInHierarchy) return;
            if (activeShotCount >= MaxConcurrentShots) return;

            activeShotCount++;
            StartCoroutine(PlayShotLifecycle(start, end, color, heavyImpact));
        }

        public void Clear()
        {
            StopAllCoroutines();
            activeShotCount = 0;

            linePool.Clear();
            circlePool.Clear();

            clearBuffer.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                SpriteRenderer renderer = transform.GetChild(i).GetComponent<SpriteRenderer>();
                if (renderer != null)
                    clearBuffer.Add(renderer);
            }

            for (int i = 0; i < clearBuffer.Count; i++)
            {
                SpriteRenderer renderer = clearBuffer[i];
                renderer.gameObject.SetActive(false);
                ReleaseToPool(renderer);
            }

            clearBuffer.Clear();
        }

        private IEnumerator PlayShotLifecycle(Vector2 start, Vector2 end, Color color, bool heavyImpact)
        {
            yield return AnimateShot(start, end, color, heavyImpact);
            activeShotCount = Mathf.Max(0, activeShotCount - 1);
        }

        private IEnumerator AnimateShot(Vector2 start, Vector2 end, Color color, bool heavyImpact)
        {
            float distance = Vector2.Distance(start, end);
            if (distance <= 0.01f) yield break;

            SpriteRenderer streak = Take(linePool, "WorldShotStreak", SquareFlowWorldSprites.Square, 10);
            SpriteRenderer glow = Take(circlePool, "WorldShotGlow", SquareFlowWorldSprites.Glow, 11);
            SpriteRenderer core = Take(circlePool, "WorldShotCore", SquareFlowWorldSprites.Circle, 12);
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
            float duration = heavyImpact ? 0.16f : 0.12f;
            Vector2 direction = (end - start).normalized;
            float trailLength = Mathf.Min(distance, SquareFlowVisualMetrics.ShotBulletTrailLength);
            streak.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            streak.transform.localScale = new Vector3(trailLength, heavyImpact ? 0.08f : 0.055f, 1f);

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOut(t);
                Vector2 position = Vector2.Lerp(start, end, eased);
                Vector2 trailCenter = position - direction * trailLength * 0.5f;
                streak.color = ColorWithAlpha(color, Mathf.Lerp(0.55f, 0.12f, t));
                glow.color = ColorWithAlpha(color, Mathf.Lerp(0.68f, 0.1f, t));
                core.color = ColorWithAlpha(Color.white, Mathf.Lerp(1f, 0.36f, t));
                streak.transform.position = new Vector3(trailCenter.x, trailCenter.y, -0.45f);
                glow.transform.position = new Vector3(position.x, position.y, -0.5f);
                core.transform.position = new Vector3(position.x, position.y, -0.55f);
                glow.transform.localScale = Vector3.one * (heavyImpact ? 0.48f : 0.36f);
                core.transform.localScale = Vector3.one * 0.12f;
                yield return null;
            }

            Release(streak);
            Release(glow);
            Release(core);
            yield return AnimateImpact(end, color, heavyImpact);
        }

        private IEnumerator AnimateImpact(Vector2 position, Color color, bool heavyImpact)
        {
            SpriteRenderer pulse = Take(circlePool, "WorldImpactPulse", SquareFlowWorldSprites.Glow, 13);
            float duration = heavyImpact ? 0.28f : 0.2f;

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOut(t);
                pulse.transform.position = new Vector3(position.x, position.y, -0.5f);
                pulse.transform.localScale = Vector3.one * Mathf.Lerp(0.32f, heavyImpact ? 1.0f : 0.72f, eased);
                pulse.color = ColorWithAlpha(color, Mathf.Lerp(0.42f, 0f, t));
                yield return null;
            }

            Release(pulse);
        }

        private SpriteRenderer Take(Queue<SpriteRenderer> pool, string name, Sprite sprite, int order)
        {
            SpriteRenderer renderer = null;
            while (pool.Count > 0 && renderer == null)
                renderer = pool.Dequeue();

            if (renderer == null)
                renderer = CreateRenderer(name, sprite, order);

            renderer.gameObject.name = name;
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            renderer.gameObject.SetActive(true);
            return renderer;
        }

        private SpriteRenderer CreateRenderer(string name, Sprite sprite, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private void Release(SpriteRenderer renderer)
        {
            renderer.gameObject.SetActive(false);
            ReleaseToPool(renderer);
        }

        private void ReleaseToPool(SpriteRenderer renderer)
        {
            if (renderer == null) return;

            Queue<SpriteRenderer> pool = renderer.sprite == SquareFlowWorldSprites.Square ? linePool : circlePool;
            int maxPoolSize = renderer.sprite == SquareFlowWorldSprites.Square ? MaxLinePoolSize : MaxCirclePoolSize;
            if (pool.Count < maxPoolSize)
            {
                pool.Enqueue(renderer);
                return;
            }

            DestroyRenderer(renderer);
        }

        private static void DestroyRenderer(SpriteRenderer renderer)
        {
            if (renderer == null) return;

            GameObject go = renderer.gameObject;
            go.SetActive(false);
            go.transform.SetParent(null, false);

            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
        }

        private void OnDisable()
        {
            Clear();
        }

        private static float EaseOut(float t)
        {
            float inverse = 1f - Mathf.Clamp01(t);
            return 1f - inverse * inverse;
        }

        private static Color ColorWithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
