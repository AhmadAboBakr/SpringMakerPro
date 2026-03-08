using UnityEngine;

namespace Shababeek.Springs
{
    /// <summary>
    /// Animates spring properties over time (oscillation, stretch, compress)
    /// </summary>
    [RequireComponent(typeof(BaseSpring))]
    public class SpringAnimator : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField, Tooltip("The spring to animate (auto-detected if left empty)")]
        private BaseSpring spring;

        [Header("Radius Animation")]
        [SerializeField, Tooltip("Enable radius oscillation")]
        private bool animateRadius;

        [SerializeField, Tooltip("Amount added/subtracted from the base radius")]
        private float radiusAmplitude = 0.1f;

        [SerializeField, Tooltip("Oscillation speed in cycles per second")]
        private float radiusFrequency = 1f;

        [Header("Windings Animation")]
        [SerializeField, Tooltip("Enable winding count oscillation")]
        private bool animateWindings;

        [SerializeField, Tooltip("Min winding count during animation")]
        private int windingsMin = 3;

        [SerializeField, Tooltip("Max winding count during animation")]
        private int windingsMax = 8;

        [SerializeField, Tooltip("Oscillation speed in cycles per second")]
        private float windingsFrequency = 0.5f;

        [Header("Height Animation (SimpleSpring only)")]
        [SerializeField, Tooltip("Enable height oscillation")]
        private bool animateHeight;

        [SerializeField, Tooltip("Amount added/subtracted from the base height")]
        private float heightAmplitude = 0.5f;

        [SerializeField, Tooltip("Oscillation speed in cycles per second")]
        private float heightFrequency = 1f;

        private float _baseRadius;
        private float _baseHeight;
        private SimpleSpring _simpleSpring;

        private void Awake()
        {
            if (spring == null)
                spring = GetComponent<BaseSpring>();

            _simpleSpring = spring as SimpleSpring;
        }

        private void Start()
        {
            _baseRadius = spring.Radius;
            if (_simpleSpring != null)
                _baseHeight = _simpleSpring.Height;
        }

        private void Update()
        {
            float time = Time.time;

            if (animateRadius)
            {
                float radiusOffset = Mathf.Sin(time * radiusFrequency * 2f * Mathf.PI) * radiusAmplitude;
                spring.Radius = Mathf.Max(0.01f, _baseRadius + radiusOffset);
            }

            if (animateWindings)
            {
                float windingT = (Mathf.Sin(time * windingsFrequency * 2f * Mathf.PI) + 1f) * 0.5f;
                spring.Windings = Mathf.RoundToInt(Mathf.Lerp(windingsMin, windingsMax, windingT));
            }

            if (animateHeight && _simpleSpring != null)
            {
                float heightOffset = Mathf.Sin(time * heightFrequency * 2f * Mathf.PI) * heightAmplitude;
                _simpleSpring.Height = Mathf.Max(0.01f, _baseHeight + heightOffset);
            }
        }

        /// <summary>
        /// Resets animation baselines to current spring values
        /// </summary>
        public void RecaptureBaselines()
        {
            if (spring != null)
                _baseRadius = spring.Radius;
            if (_simpleSpring != null)
                _baseHeight = _simpleSpring.Height;
        }
    }
}
