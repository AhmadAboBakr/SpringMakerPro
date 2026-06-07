# Changelog

## [1.0.0] - 2026-06-06

### Added
- SimpleSpring: helical coil along local Y axis
- CurvedSpring: coil along quadratic Bezier curve with three editable control points
- TransformSpring: coil along cubic Bezier curve between two Transform references
- PathSpring: coil along Catmull-Rom spline with arbitrary waypoints and closed loop support
- LineRendererSpring: line-based visualization with Catmull-Rom smoothing
- SpringMeshGenerator: tube mesh extrusion with configurable cross-section, caps, and tangents
- SpringAnimator: oscillation of radius, windings, and height with independent amplitude/frequency
- TaperCurve: AnimationCurve-based radius multiplier for conical and hourglass shapes
- SciFi Spring Glow shader for Built-in RP and URP (additive glow, pulsing, Fresnel, noise)
- Spring Line shader for URP (gradient color, edge glow, flow animation, noise)
- SpringTextureGenerator: procedural energy texture generation
- ReactiveVars integration (optional, auto-detected via assembly definition)
- Custom editors with scene handles for all spring types
- Demo scene with orbit camera
- PDF manual with full API reference
