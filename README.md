# Unity Zero-Alloc Signal Bus

This Signal Bus allows sending and receiving events in Unity without producing GC allocations at runtime.

## 🔑 Features
- Zero allocation when calling `Fire` / `Subscribe` / `Unsubscribe` (aside from initial allocations).
- Supports subscribing/unsubscribing while firing using a pending queue.
- Works with any payload type (`struct` or `class`).
- You can create multiple `SignalBus` instances (global / scene-scoped / test).

## 📦 Installation
Copy the runtime files into your Unity project under `Packages/` or `Assets/`.

## 🚀 Usage

### 1. Define a payload
```csharp
public struct PlayerHit
{
    public int AttackerId;
    public float Damage;
}
```

### 2. Subscribe and fire signals

```csharp
var bus = new BattleTurn.SignalBus.SignalBus();
var pipe = bus.For<PlayerHit>();
pipe.Subscribe(listener);
pipe.Fire(new PlayerHit { AttackerId = 1, Damage = 5f });
```

### Notes
- Runtime types live under the `BattleTurn.SignalBus` namespace.
- Editor types (benchmarks/tools) live under `BattleTurn.SignalBus.Editor`.

If you want, I can add a tiny Example scene and a README usage snippet to the package.
