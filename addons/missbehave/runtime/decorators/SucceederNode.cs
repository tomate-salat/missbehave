using Godot;

namespace Missbehave;

/// <summary>Always reports Success once the child finishes, whatever the child returned.</summary>
[GlobalClass, Tool, Icon("res://addons/missbehave/icons/succeeder.svg")]
public partial class SucceederNode : ADecoratorNode {
    protected override BehaviorStatus Tick(BtContext ctx) {
        var status = TickChild(ctx);
        return status == BehaviorStatus.Running ? BehaviorStatus.Running : BehaviorStatus.Success;
    }
}
