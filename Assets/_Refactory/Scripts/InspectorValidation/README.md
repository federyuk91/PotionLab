# Inspector Validation

The Inspector Validation system checks required scene references before a build and reports missing assignments in the Unity Console.

Use it to make Inspector dependencies explicit while avoiding runtime fallback code such as `FindObjectOfType`, singleton recovery, or repeated `GetComponent` calls.

## Overview

Add `[RequiredInspectorReference]` to serialized Unity object fields that must be assigned in the Inspector.

During build validation, the system:

1. Opens each enabled scene in Build Settings.
2. Checks every `MonoBehaviour` for missing required references.
3. Optionally tries to resolve missing references automatically.
4. Saves the scene only if an automatic assignment was made.
5. Logs auto-resolved references and unresolved references.

You can also run validation manually from:

`Tools > Inspector Validation > Validate Build Scenes`

## Basic Usage

```csharp
using InspectorValidation;
using UnityEngine;

public class ExampleController : MonoBehaviour
{
    [SerializeField, RequiredInspectorReference]
    private GameObject target;
}
```

If `target` is not assigned, the validator reports the missing reference.

## Severity

Use `Severity` to classify a missing reference in the validation report.

```csharp
[SerializeField]
[RequiredInspectorReference(Severity.Error)]
private GameObject target;
```

Available values:

| Value | Description |
| --- | --- |
| `Warning` | The reference is important but does not necessarily block the build. |
| `Error` | The reference is required for correct behavior. |

The current implementation reports the severity in the Console. It does not stop the build automatically.

## Custom Message

Pass a message to explain why the reference is required.

```csharp
[SerializeField]
[RequiredInspectorReference(Severity.Error, "Required to update the HP and MP UI.")]
private CharacterStats characterStats;
```

If `message` is `null`, the validator still logs the missing reference, but without extra explanation.

## Automatic Resolution

Use `ResolveMode` to tell the validator how to assign a missing reference before reporting it.

```csharp
[SerializeField]
[RequiredInspectorReference(ResolveMode.Local)]
private Animator animator;
```

Available values:

| Value | Description |
| --- | --- |
| `None` | Do not attempt automatic resolution. This is the default. |
| `Local` | Assigns the component from the same GameObject using `GetComponent<T>()`. |
| `SceneSingleton` | Finds exactly one component of the field type in the owning scene, including inactive objects. |

### Local

Use `Local` only when the required component must be on the same GameObject.

```csharp
[SerializeField]
[RequiredInspectorReference(ResolveMode.Local)]
private Collider2D triggerCollider;
```

### SceneSingleton

Use `SceneSingleton` only for components that should have a single instance in the scene.

```csharp
[SerializeField]
[RequiredInspectorReference(ResolveMode.SceneSingleton)]
private GameManager gameManager;
```

Rules:

- Searches only in the scene that owns the component.
- Includes inactive GameObjects.
- Assigns the reference only if exactly one matching component exists.
- Leaves the field unassigned if no matching component exists.
- Leaves the field unassigned and logs a warning if more than one matching component exists.

## Combining Resolution And Severity

You can specify both automatic resolution and severity.

```csharp
[SerializeField]
[RequiredInspectorReference(
    ResolveMode.SceneSingleton,
    Severity.Error,
    "Required for level flow.")]
private GameManager gameManager;
```

Defaults:

```csharp
ResolveMode = ResolveMode.None
Severity = Severity.Warning
Message = null
```

## CompileReference Fallback

For uncommon cases that cannot be represented with `ResolveMode`, a component can expose a parameterless `CompileReference` method.

```csharp
#if UNITY_EDITOR
private bool CompileReference()
{
    bool changed = false;

    // Custom editor-only assignment logic.

    return changed;
}
#endif
```

Validation order:

1. Try automatic resolution from `ResolveMode`.
2. If references are still missing, call `CompileReference()` when present.
3. Run the final validation report.

`CompileReference()` can return:

| Return type | Behavior |
| --- | --- |
| `bool` | Return `true` when the scene should be saved. |
| `void` | Treated as a possible change. |

Keep `CompileReference()` inside `#if UNITY_EDITOR`.

## Console Output

When a reference is assigned automatically, the validator logs:

- scene path;
- GameObject hierarchy path;
- component name;
- field name;
- strategy used.

Example:

```text
INSPECTOR VALIDATION: Auto-compiled missing Inspector references.
Scene: Assets/Scenes/Level01.unity
- Player/UI / CharacterUIController.characterStats [SceneSingleton]
```

When a reference remains missing, the validator logs:

```text
INSPECTOR VALIDATION: Missing Inspector references found in enabled build scenes.
Scene: Assets/Scenes/Level01.unity
- Lever / TriggerLeva.dialogManager [Warning]
```

## Recommended Patterns

Use `ResolveMode.Local` for:

- `Animator`;
- `Collider2D`;
- `Rigidbody2D`;
- local helper components.

Use `ResolveMode.SceneSingleton` for:

- scene-level managers;
- scene-level controllers;
- one-per-scene services.

Use manual Inspector assignment for:

- specific UI objects;
- VFX children;
- arrays and lists;
- spell targets;
- transforms that are not unique;
- references where multiple valid objects may exist.

## Limitations

- Automatic resolution supports fields derived from `UnityEngine.Component`.
- `SceneSingleton` does not search assets, prefabs, or unloaded scenes.
- The validator scans enabled scenes from Build Settings.
- Automatic assignments are editor-time changes and can modify scene files.
- Ambiguous `SceneSingleton` references are not assigned.

## Best Practices

- Prefer explicit Inspector references for runtime behavior.
- Use automatic resolution only when the correct target is deterministic.
- Review auto-compiled logs after validation.
- Do not use runtime searches to compensate for missing Inspector setup.
- Keep project-specific assignment rules inside `CompileReference()` only when standard modes are insufficient.
