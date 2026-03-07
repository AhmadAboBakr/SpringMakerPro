#if SHABABEEK_REACTIVE_VARS
using UnityEngine;
using Shababeek.ReactiveVars;
using System;

namespace Shababeek.Springs.Integrations
{
    /// <summary>
    /// Drives spring properties from ReactiveVars ScriptableVariables
    /// </summary>
    [RequireComponent(typeof(BaseSpring))]
    public class ReactiveVarsSpringDriver : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField, Tooltip("The spring to drive (auto-detected if empty)")]
        private BaseSpring spring;

        [Header("Variable Bindings")]
        [SerializeField, Tooltip("Drives the spring radius")]
        private FloatVariable radiusVariable;

        [SerializeField, Tooltip("Drives the number of windings")]
        private IntVariable windingsVariable;

        [SerializeField, Tooltip("Drives the points per winding")]
        private IntVariable pointsPerWindingVariable;

        [Header("SimpleSpring Bindings")]
        [SerializeField, Tooltip("Drives the height (SimpleSpring only)")]
        private FloatVariable heightVariable;

        [Header("TransformSpring Bindings")]
        [SerializeField, Tooltip("Drives the direction distance (TransformSpring only)")]
        private FloatVariable directionDistanceVariable;

        private SimpleSpring _simpleSpring;
        private TransformSpring _transformSpring;
        private IDisposable _radiusSub;
        private IDisposable _windingsSub;
        private IDisposable _ppwSub;
        private IDisposable _heightSub;
        private IDisposable _dirDistSub;

        private void Awake()
        {
            if (spring == null)
                spring = GetComponent<BaseSpring>();

            _simpleSpring = spring as SimpleSpring;
            _transformSpring = spring as TransformSpring;
        }

        private void OnEnable()
        {
            SubscribeAll();
        }

        private void OnDisable()
        {
            UnsubscribeAll();
        }

        private void SubscribeAll()
        {
            if (radiusVariable != null)
                _radiusSub = radiusVariable.OnValueChanged.Subscribe(v => spring.Radius = v);

            if (windingsVariable != null)
                _windingsSub = windingsVariable.OnValueChanged.Subscribe(v => spring.Windings = v);

            if (pointsPerWindingVariable != null)
                _ppwSub = pointsPerWindingVariable.OnValueChanged.Subscribe(v => spring.PointsPerWinding = v);

            if (heightVariable != null && _simpleSpring != null)
                _heightSub = heightVariable.OnValueChanged.Subscribe(v => _simpleSpring.Height = v);

            if (directionDistanceVariable != null && _transformSpring != null)
                _dirDistSub = directionDistanceVariable.OnValueChanged.Subscribe(v => _transformSpring.DirectionDistance = v);
        }

        private void UnsubscribeAll()
        {
            _radiusSub?.Dispose();
            _windingsSub?.Dispose();
            _ppwSub?.Dispose();
            _heightSub?.Dispose();
            _dirDistSub?.Dispose();
        }
    }
}
#endif
