using Godot;

namespace Missbehave;

/// <summary>
/// Base of nodes that run several children in some order. The index of the child left mid-run is
/// plain per-instance state on the runtime clone — no blackboard bookkeeping required.
/// </summary>
[GlobalClass, Tool]
public abstract partial class ACompositeNode : ABehaviorNode {
    public override int MinChildren => 1;
    public override int MaxChildren => int.MaxValue;
    public override string Category => BtCategory.Composite;

    /// <summary>Index of the child that returned Running last tick, or -1.</summary>
    protected int RunningChild { get; set; } = -1;

    protected override void OnCloned() => RunningChild = -1;

    public override void Interrupt(BtContext ctx) {
        ClearRunning(ctx);
    }

    /// <summary>
    /// Ticks one child, calling <c>BeforeRun</c> unless it is the child we are resuming, and
    /// <c>AfterRun</c> once it finishes.
    /// </summary>
    protected BehaviorStatus TickChild(int index, BtContext ctx) {
        var child = Children[index];
        if (child == null) return BehaviorStatus.Failure;

        if (index != RunningChild) child.BeforeRunInternal(ctx);
        var status = child.TickInternal(ctx);
        if (status != BehaviorStatus.Running) child.AfterRun(ctx);
        return status;
    }

    /// <summary>Abandons the child left mid-run, unless it is <paramref name="except"/>.</summary>
    protected void ClearRunning(BtContext ctx, int except = -1) {
        if (RunningChild >= 0 && RunningChild != except && RunningChild < Children.Count) {
            Children[RunningChild]?.Interrupt(ctx);
        }
        RunningChild = -1;
    }

    /// <summary>
    /// For reactive composites, which re-tick every child from the front. A child at or before
    /// <paramref name="lastTickedIndex"/> already ran this tick and finished on its own, so only a
    /// running child beyond that point is stale and needs interrupting.
    /// </summary>
    protected void InterruptStaleRunning(BtContext ctx, int lastTickedIndex) {
        if (RunningChild > lastTickedIndex && RunningChild < Children.Count) {
            Children[RunningChild]?.Interrupt(ctx);
        }
        RunningChild = -1;
    }
}
