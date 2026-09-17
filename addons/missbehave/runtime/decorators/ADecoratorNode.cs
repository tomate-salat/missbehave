using Godot;

namespace Missbehave;

/// <summary>Base of nodes that wrap exactly one child and modify its result or its scheduling.</summary>
[GlobalClass, Tool]
public abstract partial class ADecoratorNode : ABehaviorNode {
    public override int MinChildren => 1;
    public override int MaxChildren => 1;
    public override string Category => BtCategory.Decorator;

    protected bool ChildIsRunning { get; set; }

    protected ABehaviorNode Child => Children.Count > 0 ? Children[0] : null;

    protected override void OnCloned() => ChildIsRunning = false;

    /// <summary>Ticks the wrapped child, handling its BeforeRun/AfterRun bracketing.</summary>
    protected BehaviorStatus TickChild(BtContext ctx) {
        var child = Child;
        if (child == null) return BehaviorStatus.Failure;

        if (!ChildIsRunning) child.BeforeRunInternal(ctx);
        var status = child.TickInternal(ctx);

        if (status == BehaviorStatus.Running) {
            ChildIsRunning = true;
        }
        else {
            ChildIsRunning = false;
            child.AfterRun(ctx);
        }
        return status;
    }

    public override void Interrupt(BtContext ctx) {
        if (ChildIsRunning) Child?.Interrupt(ctx);
        ChildIsRunning = false;
    }
}
