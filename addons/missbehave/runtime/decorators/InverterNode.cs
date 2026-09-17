using Godot;

namespace Missbehave;

/// <summary>Swaps Success and Failure. Running passes through unchanged.</summary>
[GlobalClass, Tool, Icon("res://addons/missbehave/icons/inverter.svg")]
public partial class InverterNode : ADecoratorNode {
    protected override BehaviorStatus Tick(BtContext ctx) => TickChild(ctx) switch {
        BehaviorStatus.Success => BehaviorStatus.Failure,
        BehaviorStatus.Failure => BehaviorStatus.Success,
        _ => BehaviorStatus.Running,
    };
}
