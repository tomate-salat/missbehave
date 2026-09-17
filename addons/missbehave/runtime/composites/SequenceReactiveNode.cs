using Godot;

namespace Missbehave;

/// <summary>
/// Like <see cref="SequenceNode"/>, but re-checks every earlier child on each tick instead of
/// resuming. Use it when the preconditions in front of a long-running action must keep holding.
/// </summary>
[GlobalClass, Tool, Icon("res://addons/missbehave/icons/sequence_reactive.svg")]
public partial class SequenceReactiveNode : ACompositeNode {
    protected override BehaviorStatus Tick(BtContext ctx) {
        for (var i = 0; i < Children.Count; i++) {
            var status = TickChild(i, ctx);

            if (status == BehaviorStatus.Running) {
                InterruptStaleRunning(ctx, i);
                RunningChild = i;
                return BehaviorStatus.Running;
            }
            if (status == BehaviorStatus.Failure) {
                InterruptStaleRunning(ctx, i);
                return BehaviorStatus.Failure;
            }
        }

        InterruptStaleRunning(ctx, Children.Count - 1);
        return BehaviorStatus.Success;
    }
}
