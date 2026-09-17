using Godot;

namespace Missbehave;

/// <summary>
/// Runs children front to back and succeeds at the first success. Fails only when every child
/// fails. A child that returns Running is resumed on the next tick.
/// </summary>
[GlobalClass, Tool, Icon("res://addons/missbehave/icons/selector.svg")]
public partial class SelectorNode : ACompositeNode {
    protected override BehaviorStatus Tick(BtContext ctx) {
        var start = RunningChild < 0 ? 0 : RunningChild;

        for (var i = start; i < Children.Count; i++) {
            var status = TickChild(i, ctx);

            if (status == BehaviorStatus.Running) {
                RunningChild = i;
                return BehaviorStatus.Running;
            }
            if (status == BehaviorStatus.Success) {
                RunningChild = -1;
                return BehaviorStatus.Success;
            }
        }

        RunningChild = -1;
        return BehaviorStatus.Failure;
    }
}
