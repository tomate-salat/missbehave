using Godot;

namespace Missbehave;

/// <summary>
/// Keeps the child going: reports Running while the child succeeds or runs, and Success once the
/// child finally fails. Use it to loop a branch until something breaks it.
/// </summary>
[GlobalClass, Tool, Icon("res://addons/missbehave/icons/until_fail.svg")]
public partial class UntilFailNode : ADecoratorNode {
    protected override BehaviorStatus Tick(BtContext ctx) => TickChild(ctx) switch {
        BehaviorStatus.Failure => BehaviorStatus.Success,
        _ => BehaviorStatus.Running,
    };
}
