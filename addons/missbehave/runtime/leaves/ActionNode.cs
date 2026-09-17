using Godot;

namespace Missbehave;

/// <summary>
/// Base class for leaves that change the world. Subclass it, add <c>[Export]</c> parameters and
/// override <see cref="Run"/>:
/// <code>
/// [GlobalClass]
/// public partial class FollowTarget : ActionNode {
///     [Export] public float StopDistance { get; set; } = 1f;
///     protected override BehaviorStatus Run(BtContext ctx) { ... }
/// }
/// </code>
/// Remember <c>[GlobalClass]</c> — without it the node cannot be saved into a tree resource.
/// </summary>
[GlobalClass, Tool, Icon("res://addons/missbehave/icons/action.svg")]
public abstract partial class ActionNode : ALeafNode {
    protected abstract BehaviorStatus Run(BtContext ctx);

    protected override BehaviorStatus Tick(BtContext ctx) => Run(ctx);
}
