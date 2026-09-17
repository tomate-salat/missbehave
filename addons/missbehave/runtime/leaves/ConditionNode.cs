using Godot;

namespace Missbehave;

/// <summary>
/// Base class for leaves that only inspect the world. Override <see cref="Check"/> and return
/// whether the condition holds; a condition never returns Running.
/// </summary>
[GlobalClass, Tool, Icon("res://addons/missbehave/icons/condition.svg")]
public abstract partial class ConditionNode : ALeafNode {
    /// <summary>Inverts the result, so a condition can be reused without an extra decorator.</summary>
    [Export]
    public bool Negate { get; set; }

    protected abstract bool Check(BtContext ctx);

    protected override BehaviorStatus Tick(BtContext ctx) {
        var result = Check(ctx);
        if (Negate) result = !result;
        return result ? BehaviorStatus.Success : BehaviorStatus.Failure;
    }

    public override string GetSummary() => Negate ? "negated" : "";
}
