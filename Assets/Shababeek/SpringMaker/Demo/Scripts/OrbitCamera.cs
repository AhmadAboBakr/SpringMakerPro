using UnityEngine;

namespace Shababeek.Springs.Demo
{
    /// <summary>
    /// Orbits the camera around a target point with mouse drag and scroll zoom.
    /// </summary>
    public class OrbitCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField, Tooltip("The point the camera orbits around. If null, orbits around world origin")]
        private Transform target;

        [SerializeField, Tooltip("Offset added to the target position")]
        private Vector3 targetOffset = Vector3.up;

        [Header("Orbit")]
        [SerializeField, Tooltip("Horizontal rotation speed")]
        private float orbitSpeed = 5f;

        [SerializeField, Tooltip("Vertical rotation speed")]
        private float verticalSpeed = 3f;

        [SerializeField, Tooltip("Minimum vertical angle in degrees")]
        private float minVerticalAngle = -20f;

        [SerializeField, Tooltip("Maximum vertical angle in degrees")]
        private float maxVerticalAngle = 80f;

        [Header("Zoom")]
        [SerializeField, Tooltip("Current distance from the target")]
        private float distance = 8f;

        [SerializeField, Tooltip("Minimum zoom distance")]
        private float minDistance = 2f;

        [SerializeField, Tooltip("Maximum zoom distance")]
        private float maxDistance = 30f;

        [SerializeField, Tooltip("Scroll wheel zoom speed")]
        private float zoomSpeed = 5f;

        [SerializeField, Tooltip("Smoothing applied to zoom changes")]
        private float zoomSmoothing = 8f;

        [Header("Pan")]
        [SerializeField, Tooltip("Middle-mouse pan speed")]
        private float panSpeed = 0.5f;

        [Header("Auto Rotate")]
        [SerializeField, Tooltip("Automatically rotate when idle")]
        private bool autoRotate = true;

        [SerializeField, Tooltip("Auto-rotation speed in degrees per second")]
        private float autoRotateSpeed = 10f;

        [SerializeField, Tooltip("Seconds of inactivity before auto-rotation resumes")]
        private float autoRotateDelay = 3f;

        [Header("Smoothing")]
        [SerializeField, Tooltip("Damping applied to orbit movement")]
        private float damping = 6f;

        private float _yaw;
        private float _pitch = 25f;
        private float _targetDistance;
        private float _currentDistance;
        private float _lastInputTime;
        private Vector3 _panOffset;

        private void Start()
        {
            _targetDistance = distance;
            _currentDistance = distance;
            _lastInputTime = -autoRotateDelay;

            Vector3 angles = transform.eulerAngles;
            _yaw = angles.y;
            _pitch = angles.x;
        }

        private void LateUpdate()
        {
            HandleOrbitInput();
            HandleZoomInput();
            HandlePanInput();
            HandleAutoRotate();
            ApplyTransform();
        }

        private void HandleOrbitInput()
        {
            if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1)) return;

            float dx = Input.GetAxis("Mouse X") * orbitSpeed;
            float dy = Input.GetAxis("Mouse Y") * verticalSpeed;

            _yaw += dx;
            _pitch -= dy;
            _pitch = Mathf.Clamp(_pitch, minVerticalAngle, maxVerticalAngle);

            _lastInputTime = Time.time;
        }

        private void HandleZoomInput()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.001f) return;

            _targetDistance -= scroll * zoomSpeed;
            _targetDistance = Mathf.Clamp(_targetDistance, minDistance, maxDistance);
            _lastInputTime = Time.time;
        }

        private void HandlePanInput()
        {
            if (!Input.GetMouseButton(2)) return;

            float dx = -Input.GetAxis("Mouse X") * panSpeed;
            float dy = -Input.GetAxis("Mouse Y") * panSpeed;

            _panOffset += transform.right * dx + transform.up * dy;
            _lastInputTime = Time.time;
        }

        private void HandleAutoRotate()
        {
            if (!autoRotate) return;
            if (Time.time - _lastInputTime < autoRotateDelay) return;

            _yaw += autoRotateSpeed * Time.deltaTime;
        }

        private void ApplyTransform()
        {
            _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, Time.deltaTime * zoomSmoothing);

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 pivot = target != null ? target.position + targetOffset : targetOffset;
            pivot += _panOffset;

            Vector3 desiredPos = pivot - rotation * Vector3.forward * _currentDistance;

            transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * damping);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * damping);
        }

        /// <summary>
        /// Resets the camera to its initial orbit state.
        /// </summary>
        public void ResetOrbit()
        {
            _yaw = 0f;
            _pitch = 25f;
            _targetDistance = distance;
            _panOffset = Vector3.zero;
            _lastInputTime = -autoRotateDelay;
        }
    }
}
