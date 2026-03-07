using UnityEngine;

namespace Shababeek.Springs
{
    /// <summary>
    /// Generates a helical spring along the local Y axis
    /// </summary>
    public class SimpleSpring : BaseSpring
    {
        [Header("Simple Spring Settings")]
        [SerializeField, Tooltip("Height of the spring along the Y axis")]
        private float height = 2f;

        /// <summary>
        /// Gets or sets the spring height
        /// </summary>
        public float Height
        {
            get => height;
            set
            {
                if (Mathf.Approximately(height, value)) return;
                height = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// Generates helical spring points
        /// </summary>
        public override void CalculatePoints()
        {
            controlPoints = new Vector3[TotalPoints];

            for (int i = 0; i < TotalPoints; i++)
            {
                float t = (float)i / Mathf.Max(1, TotalPoints - 1);
                float angle = 2f * Mathf.PI * i / pointsPerWinding;
                float taperRadius = radius * EvaluateTaper(t);

                controlPoints[i] = new Vector3(
                    taperRadius * Mathf.Cos(angle),
                    t * height,
                    taperRadius * Mathf.Sin(angle));
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            height = Mathf.Max(0.01f, height);
        }
    }
}
