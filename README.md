# Unnamed tick update manager thingamajig
This is a game tick manager for Unity. It allows you to bypass some performance costs and inflexibility imposed by Unity's default update logic architecture. The repo here contains the main scripts, as well as a performance benchmark demo. Please note that the documentation is a work in progress.

## Features
### Current
- Significantly faster than Unity's built-in update events (in a build, near-empty updates were tested at ~14x faster).
- Gives control over tick ordering. For example, you could process player status before processing the UI.
  - Another use case can be for parallel processing. You can use tick ordering to await jobs/threads.
- Supports arbitrary code, not just GameObjects & Components.
- Implemented as simple interfaces.
- Easily extendable.

### Planned
- Further performance improvements.
- Debugging tools.

## Setup
- Move LoopComponent.cs & UpdateManager.cs to your project.
- <b>IMPORTANT!</b> You need to modify the script execution order so that UpdateManager runs before your scripts.
  - Edit -> Project Settings -> Script Execution Order -> (add UpdateManager and drag it above Default Time)
- Attach UpdateManager to a new empty GameObject in your first loaded scene. Now it's good to go!

## Tutorial
To link your logic into the system, inherit from LoopComponent and implement one or more update interfaces. The interfaces you can implement are: ILoopEarlyUpdate, ILoopUpdate, ILoopLateUpdate, and ILoopFixedUpdate.

```c#
public class Jeff : LoopComponent, ILoopUpdate
{
    # Optional - specify the update order. Higher values are called last.
    public override int UpdateOrder => 100;

    public void LoopUpdate()
    {
        DoCoolStuff();
    }
}
```

If you use OnEnable or OnDisable, you need to call the base method!

```c#
public class Jeff : LoopComponent, ILoopUpdate
{
    public override int UpdateOrder => 100;

    public void LoopUpdate()
    {
        DoCoolStuff();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ScreamReallyLoud();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Die();
    }
}
```

Rather than using magic numbers for the update order, it is recommended to initialize the system with named loop entries. This will be explained when I update the docs.
