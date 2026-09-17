using Godot;

namespace Missbehave;

/// <summary>
/// Like <see cref="SelectorNode"/>, but re-evaluates from the first child on every tick instead of
/// resuming. A higher-priority branch that becomes viable takes over immediately, interrupting the
/// lower-priority branch that was running. This is the usual root of a reactive AI.
/// </summary>
[GlobalClass, Tool, Icon("res://addons/missbehave/icons/selector_reactive.svg")]
public partial class SelectorReactiveNode : ACompositeNode {
    protected override BehaviorStatus Tick(BtContext ctx) {
        for (var i = 0; i < Children.Count; i++) {
            var status = TickChild(i, ctx);

            if (status == BehaviorStatus.Running) {
                InterruptStaleRunning(ctx, i);
                RunningChild = i;
                return BehaviorStatus.Running;
            }
            if (status == BehaviorStatus.Success) {
                InterruptStaleRunning(ctx, i);
                return BehaviorStatus.Success;
            }
        }

        InterruptStaleRunning(ctx, Children.Count - 1);
        return BehaviorStatus.Failure;
    }
}
