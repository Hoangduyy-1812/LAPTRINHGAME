---
name: unity-dev-rules
description: Production-ready Unity 6+ AI Agent rules and coding conventions covering C# MonoBehaviour lifecycle, architecture, performance, UI, input system, networking, and testing.
---

# Unity AI Agent Development Rules

A comprehensive, production-grade rule set for Unity 6+ and C# development in game projects.

## Included Rule Modules

1. **Unity Core & C# Standards** (ules/unity-core.md)
   - Naming conventions (PascalCase, private _fields, properties).
   - MonoBehaviour lifecycle order (Awake -> OnEnable -> Start -> FixedUpdate -> Update -> LateUpdate -> OnDisable -> OnDestroy).
   - Modern C# 12 and Unity 6 features (Awaitable, expression-bodied members, pattern matching).
   - [SerializeField] best practices and ScriptableObject data architecture.

2. **Unity Architecture** (ules/unity-architecture.md)
   - Decoupled event-driven architecture with ScriptableObjects.
   - Interface segregation and dependency injection patterns.
   - Separation of concerns between Logic, Presentation, and Data.

3. **Performance & Memory Optimization** (ules/unity-performance.md)
   - Zero-allocation patterns in Update loops (cache WaitForSeconds, avoid LINQ / string concatenation in hot paths).
   - Object pooling for projectiles, VFX, and audio sources.
   - Physics optimization: Layer Collision Matrix, NonAlloc raycasts (Physics2D.OverlapCircleNonAlloc).
   - Draw call reduction: Sprite atlases, dynamic batching, GPU instancing.

4. **Unity Input System** (ules/unity-input.md)
   - Unity New Input System (InputActionAsset, C# generated classes).
   - Multi-device support (keyboard/mouse, gamepad split-screen controls).

5. **Unity UI & HUD** (ules/unity-ui.md)
   - UI Toolkit vs Canvas architecture.
   - Canvas optimization (splitting static vs dynamic canvases).
   - Responsive layout anchors and safe area handling.

6. **Unity Networking** (ules/unity-networking.md)
   - Netcode for GameObjects (NGO) server-authoritative architecture.
   - NetworkVariable synchronization and ServerRpc/ClientRpc patterns.
   - Client prediction and interpolation.

7. **Unity Testing & QA** (ules/unity-testing.md)
   - Unity Test Framework (EditMode and PlayMode tests).
   - Automated integration tests and regression verification.

8. **Code Organization** (ules/code-organization.md)
   - Clean folder hierarchy (Assets/Scripts/Core, Assets/Scripts/Gameplay, Assets/Scripts/UI, etc.).
   - Assembly Definitions (.asmdef) for faster compilation and clean boundaries.

9. **Unity ECS & DOTS** (ules/unity-ecs.md)
   - Entities, Components, System groups, Burst compiler, and Job System conventions.

---

## Detailed Rules Reference

All detailed rule specifications are located in the ules/ subfolder of this skill and synced to .agent/rules/.
