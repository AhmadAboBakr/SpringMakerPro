# Spring Maker Pro

Procedural spring generation toolkit for Unity. Create helical, curved, path-based, and transform-driven springs with multiple visualization options — LineRenderer or tube mesh.

## Requirements

- Unity 2021.3 or later
- Built-in Render Pipeline or URP (glow shader provided for both)

## Quick Start

1. Create a new GameObject and add one of the spring components: **SimpleSpring**, **CurvedSpring**, **TransformSpring**, or **PathSpring**.
2. Add a visualizer: **LineRendererSpring** (line-based) or **SpringMeshGenerator** (tube mesh).
3. Adjust spring settings in the Inspector — the spring updates automatically.

## Spring Types

**SimpleSpring** — A helical coil along the local Y axis. Configure `Height`, `Windings`, `Radius`, and `PointsPerWinding`.

**CurvedSpring** — A coil that follows a quadratic Bézier curve defined by three editable control points (`StartPoint`, `MiddlePoint`, `EndPoint`). Drag the handles directly in the Scene view.

**TransformSpring** — A coil that follows a cubic Bézier curve between two Transform references. The spring automatically updates when either transform moves or rotates. `DirectionDistance` controls how far the Bézier handles extend.

**PathSpring** — A coil that follows a Catmull-Rom spline with an arbitrary number of control points. Supports closed loops via `ClosedPath` and adjustable spline tension via `Smoothing` (0 = uniform, 0.5 = centripetal, 1 = chordal).

All spring types share `Windings`, `Radius`, `PointsPerWinding`, and `TaperCurve` from the BaseSpring base class.

## Visualization

**LineRendererSpring** — Renders the spring coil as a smoothed line using Unity's LineRenderer. Applies Catmull-Rom interpolation between control points for smooth curves. Requires a LineRenderer component on the same GameObject.

**SpringMeshGenerator** — Extrudes a tube mesh along the spring coil path. Configurable cross-section (`Sides`: 3 = triangle, 4 = square, 6 = hex, 16+ = smooth circle), `TubeRadius` with its own AnimationCurve for tapering, optional end caps, and tangent generation for normal mapping. The editor includes a "Save Mesh Asset" button to export baked meshes.

## Animation

**SpringAnimator** — Oscillates spring properties over time. Independently toggle and configure radius, winding count, and height (SimpleSpring only) animation with separate amplitude and frequency controls. Call `RecaptureBaselines()` after changing base values at runtime.

## Shaders

**Shababeek/SciFi Spring Glow** — Additive glow shader with pulsing, UV scrolling, Fresnel, and noise-based energy effects. Works well with LineRendererSpring. Available for both Built-in RP and URP.

## Taper Curve

Every spring type exposes a `TaperCurve` (AnimationCurve) that multiplies the coil radius along the spring's length. The curve is evaluated from 0 (start) to 1 (end). Set it to a declining curve for a conical spring, a U-shape for an hourglass, or leave it flat (default) for uniform radius.

## Events

`BaseSpring.OnSpringUpdated` (`Action<Vector3[]>`) fires after every recalculation, passing the new control points array. All built-in visualizers subscribe to this event automatically. Use it for custom rendering or gameplay logic.

## Runtime Scripting

All spring properties are accessible via C# at runtime:

```csharp
SimpleSpring spring = GetComponent<SimpleSpring>();
spring.Height = 3f;
spring.Windings = 8;
spring.Radius = 0.3f;

// Subscribe to changes
spring.OnSpringUpdated += points => Debug.Log($"Spring has {points.Length} points");

// Force recalculation
spring.SetDirty();
Vector3[] pts = spring.ControlPoints;
```

Path-based springs expose their evaluation methods for custom sampling:

```csharp
CurvedSpring curved = GetComponent<CurvedSpring>();
Vector3 midpoint = curved.EvaluateBezier(0.5f);
Vector3 tangent = curved.EvaluateBezierTangent(0.5f);
```

## ReactiveVars Integration (Optional)

If you have the [ReactiveVars](https://github.com/Shababeek/ReactiveVars) package installed, add the **ReactiveVarsSpringDriver** component to bind ScriptableVariable assets (FloatVariable, IntVariable) to spring properties. The `SHABABEEK_REACTIVE_VARS` define is set automatically by the assembly definition when the package is detected.

## Folder Structure

```
SpringMaker/
├── Editor/                          # Custom inspectors and scene handles
│   ├── Shababeek.Springs.Editor.asmdef
│   ├── BaseSpringEditor.cs
│   ├── SimpleSpringEditor.cs
│   ├── CurvedSpringEditor.cs
│   ├── TransformSpringEditor.cs
│   ├── LineRendererSpringEditor.cs
│   └── SpringMeshGeneratorEditor.cs
├── Runtime/                         # Core components and shaders
│   ├── Shababeek.Springs.Runtime.asmdef
│   ├── BaseSpring.cs
│   ├── SimpleSpring.cs
│   ├── CurvedSpring.cs
│   ├── TransformSpring.cs
│   ├── PathSpring.cs
│   ├── LineRendererSpring.cs
│   ├── SpringMeshGenerator.cs
│   ├── SpringAnimator.cs
│   ├── Integrations/
│   │   └── ReactiveVarsSpringDriver.cs
│   ├── Scripts/
│   │   └── SpringTextureGenerator.cs
│   ├── Shaders/
│   │   ├── SciFiSpringGlow.shader
│   │   └── URP/
│   │       └── SciFiSpringGlow_URP.shader
│   └── Materials/
│       └── SciFiSpringMaterial.mat
├── Documentation/
│   └── SpringMakerPro_Manual.pdf
└── README.md
```

## Support

For bug reports and feature requests, contact **support@shababeek.com**.

## License

Copyright © 2026 Shababeek. All rights reserved. See LICENSE for details.
