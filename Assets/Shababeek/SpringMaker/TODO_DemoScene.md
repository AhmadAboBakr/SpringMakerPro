# Spring Maker Pro — Demo Scene TODO

## Scene Setup

- [x] Create scene `Demos/SpringMakerDemo.unity`
- [x] Set up a clean skybox / solid dark background
- [ ] Add a ground plane with a subtle grid material
- [ ] Place a directional light + ambient settings that show off the springs well
- [ ] Add a free-look camera (Cinemachine or simple orbit script) so users can explore

---

## Spring Type Showcases

Each spring type should have its own labelled area in the scene (use 3D Text or TextMeshPro world-space labels).

### SimpleSpring

- [ ] Basic vertical coil with LineRendererSpring visualization
- [ ] Second one with SpringMeshGenerator (tube mesh) to show solid coil
- [ ] Third with TaperCurve applied (wider at base, narrow at top)
- [ ] One animated with SpringAnimator (oscillating radius + height)

### CurvedSpring

- [ ] Static curved spring between two points
- [ ] One with handles visible in editor (show Bezier control point)
- [ ] Tube mesh variant for comparison vs LineRenderer

### TransformSpring

- [ ] Two GameObjects connected by a spring that updates live
- [ ] Make one target draggable at runtime (simple drag script or UI slider) so users see the spring follow
- [ ] Show both LineRenderer and tube mesh variants side by side

### PathSpring

- [ ] 4-5 waypoint path forming an interesting shape (S-curve or spiral staircase)
- [ ] LineRenderer visualization with glow shader (SciFiSpringGlow)
- [ ] Tube mesh visualization for comparison

---

## Visualization Showcases

### LineRendererSpring

- [ ] Show with default material
- [ ] Show with SciFiSpringGlow shader (sci-fi energy coil look)
- [ ] Demonstrate different smoothing subdivision levels

### SpringMeshGenerator (Tube Mesh)

- [ ] Low poly tube (sides = 4, square cross-section)
- [ ] Medium poly tube (sides = 8)
- [ ] High poly tube (sides = 16, smooth cylinder)
- [ ] Tube with radius AnimationCurve (thick-to-thin taper)

### SpringMeshDeformer (Shader-Based)

- [ ] Take a simple cube mesh and deform it along a spring
- [ ] Take a cylinder mesh and wrap it along the coil
- [ ] Show a custom mesh (e.g., a chain link or vine) deformed along a spring
- [ ] Demonstrate radialScale and uvOffset properties

---

## Animation & Interactivity

- [ ] SpringAnimator: one spring breathing (radius oscillation), one bouncing (height oscillation), one pulsing (windings oscillation)
- [ ] Runtime control panel (UI Canvas):
  - Slider for Radius
  - Slider for Windings
  - Slider for Height (SimpleSpring)
  - Slider for PointsPerWinding
  - Toggle between LineRenderer / Tube Mesh / Shader Deform
- [ ] Draggable transform target for TransformSpring (OnMouseDrag or pointer events)

---

## Shaders

- [ ] SciFiSpringGlow: energy beam / sci-fi cable look (set up with emissive color + bloom post-processing if URP/HDRP, or just additive blending for built-in)
- [ ] SpringSurface: show on a deformed mesh with a tiled texture so the UV mapping is visible

---

## ReactiveVars Integration (Optional Section)

- [ ] If ReactiveVars package is present, add a small area showing:
  - FloatVariable controlling spring radius via ReactiveVarsSpringDriver
  - A UI slider bound to the FloatVariable
  - Label: "Requires Shababeek.ReactiveVars package"
- [ ] If not present, display a note or just skip this area

---

## Polish

- [ ] Group all demo objects under organized hierarchy:
  ```
  DemoScene
  ├── Environment (ground, lights, camera)
  ├── SimpleSpring_Demos
  ├── CurvedSpring_Demos
  ├── TransformSpring_Demos
  ├── PathSpring_Demos
  ├── Visualization_Demos
  ├── Animation_Demos
  ├── UI_Canvas
  └── ReactiveVars_Demos (optional)
  ```
- [ ] Add world-space labels / signs for each section
- [ ] Color-code springs per section (different materials/colors so it's visually clear)
- [ ] Make sure everything looks good at play-time without needing to click anything
- [ ] Test with Built-in Render Pipeline (primary target)
- [ ] Test with URP if feasible (shaders may need adaptation)

---

## Pre-Submission Checklist

- [ ] Demo scene loads without errors
- [ ] No missing references or pink materials
- [ ] All springs animate/update correctly in Play mode
- [ ] UI sliders work and spring responds in real-time
- [ ] TransformSpring drag interaction works
- [ ] Scene hierarchy is clean and well-organized
- [ ] Take screenshots for Asset Store listing from this scene
- [ ] Record a short GIF/video showing the interactive demo
