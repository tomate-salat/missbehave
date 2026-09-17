using Godot;

namespace Missbehave;

/// <summary>
/// Runs the child until it has succeeded <see cref="Repetitions"/> times, then reports Success.
/// A failing child fails the repeater immediately. The counter resets on BeforeRun and Interrupt.
/// </summary>
[GlobalClass, Tool, Icon("res://addons/missbehave/icons/repeater.svg")]
public partial class RepeaterNode : ADecoratorNode {
    [Export(PropertyHint.Range, "1,100,1,or_greater")]
    public int Repetitions { get; set; } = 1;

    int _count;

    public override string GetSummary() => $"x{Repetitions}";

    public override void BeforeRun(BtContext ctx) => _count = 0;

    protected override BehaviorStatus Tick(BtContext ctx) {
        if (_count >= Repetitions) return BehaviorStatus.Success;

        var status = TickChild(ctx);
        if (status == BehaviorStatus.Running) return BehaviorStatus.Running;

        _count++;
        if (status == BehaviorStatus.Failure) return BehaviorStatus.Failure;

        return _count >= Repetitions ? BehaviorStatus.Success : BehaviorStatus.Running;
    }

    public override void Interrupt(BtContext ctx) {
        _count = 0;
        base.Interrupt(ctx);
    }

    protected override void OnCloned() {
        base.OnCloned();
        _count = 0;
    }
}
