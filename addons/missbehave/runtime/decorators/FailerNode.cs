using Godot;

namespace Missbehave;

/// <summary>Always reports Failure once the child finishes, whatever the child returned.</summary>
[GlobalClass, Tool, Icon("res://addons/missbehave/icons/failer.svg")]
public partial class FailerNode : ADecoratorNode {
    protected override BehaviorStatus Tick(BtContext ctx) {
        var status = TickChild(ctx);
        return status == BehaviorStatus.Running ? BehaviorStatus.Running : BehaviorStatus.Failure;
    }
}
