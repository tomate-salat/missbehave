using Godot;

namespace Missbehave;

/// <summary>
/// Runs children front to back and fails at the first failure. Succeeds only when all children
/// succeed. A child that returns Running is resumed on the next tick, without re-checking the
/// children before it.
/// </summary>
[GlobalClass, Tool, Icon("res://addons/missbehave/icons/sequence.svg")]
public partial class SequenceNode : ACompositeNode {
    protected override BehaviorStatus Tick(BtContext ctx) {
        var start = RunningChild < 0 ? 0 : RunningChild;

        for (var i = start; i < Children.Count; i++) {
            var status = TickChild(i, ctx);

            if (status == BehaviorStatus.Running) {
                RunningChild = i;
                return BehaviorStatus.Running;
            }
            if (status == BehaviorStatus.Failure) {
                RunningChild = -1;
                return BehaviorStatus.Failure;
            }
        }

        RunningChild = -1;
        return BehaviorStatus.Success;
    }
}
