using Godot;

namespace Missbehave;

/// <summary>
/// A sequence that runs all of its children in a shuffled order. Still fails at the first failure;
/// only the order in which the children are visited is randomised.
/// </summary>
[GlobalClass, Tool, Icon("res://addons/missbehave/icons/sequence_random.svg")]
public partial class SequenceRandomNode : ARandomizedCompositeNode {
    protected override BehaviorStatus Tick(BtContext ctx) {
        EnsureOrder();

        for (var s = Slot; s < Order.Length; s++) {
            var i = Order[s];
            var status = TickChild(i, ctx);

            if (status == BehaviorStatus.Running) {
                RunningChild = i;
                Slot = s;
                return BehaviorStatus.Running;
            }
            RunningChild = -1;
            if (status == BehaviorStatus.Failure) {
                Slot = 0;
                return BehaviorStatus.Failure;
            }
        }

        Slot = 0;
        return BehaviorStatus.Success;
    }
}
