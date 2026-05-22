using UnityEngine;

namespace SquareFlow.Effects
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SquareFlowAudio : MonoBehaviour
    {
        private const int SampleRate = 44100;
        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
        }

        public void PlayTone(float frequency, float duration, float volume)
        {
            if (frequency <= 0f || duration <= 0f || volume <= 0f) return;

            if (source == null)
            {
                source = GetComponent<AudioSource>();
                if (source == null) return;
                source.playOnAwake = false;
            }

            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            float[] samples = new float[sampleCount];
            float amplitude = Mathf.Clamp01(volume);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float fade = Mathf.Min(1f, i / (SampleRate * 0.01f), (sampleCount - i) / (SampleRate * 0.02f));
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * amplitude * Mathf.Clamp01(fade);
            }

            AudioClip clip = AudioClip.Create("SquareFlowTone", sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            source.PlayOneShot(clip);
        }
    }
}
