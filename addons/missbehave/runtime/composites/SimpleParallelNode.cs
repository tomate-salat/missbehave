using Godot;

namespace Missbehave;

/// <summary>
/// Runs exactly two children side by side. The first child is the primary one and decides the
/// result; the second runs as a background subtree whose status is ignored. Think "fire the weapon
/// while aiming at the player": the aim keeps running for as long as the firing does.
/// </summary>
[GlobalClass, Tool, Icon("res://addons/missbehave/icons/parallel.svg")]
public partial class SimpleParallelNode : ACompositeNode {
    public override int MinChildren => 2;
    public override int MaxChildren => 2;

    /// <summary>How often the secondary subtree may restart. 0 means indefinitely.</summary>
    [Export(PropertyHint.Range, "0,20,1,or_greater")]
    public int SecondaryRepeatCount { get; set; }

    /// <summary>
    /// When set, the node waits for the secondary subtree to finish its current run before
    /// reporting the primary result, instead of cutting it off.
    /// </summary>
    [Export]
    public bool DelayMode { get; set; }

    bool _primaryRunning;
    bool _primaryFinished;
    BehaviorStatus _primaryResult = BehaviorStatus.Success;
    bool _secondaryRunning;
    int _secondaryRepeatsLeft;

    public override string GetSummary() => DelayMode ? "parallel (delayed)" : "parallel";

    public override void BeforeRun(BtContext ctx) {
        _secondaryRepeatsLeft = SecondaryRepeatCount;
        Reset();
    }

    public override void AfterRun(BtContext ctx) => Reset();

    protected override BehaviorStatus Tick(BtContext ctx) {
        if (Children.Count < 2 || Children[0] == null || Children[1] == null) return BehaviorStatus.Failure;

        var primary = Children[0];
        var secondary = Children[1];

        if (!_primaryFinished) {
            if (!_primaryRunning) primary.BeforeRunInternal(ctx);
            var status = primary.TickInternal(ctx);

            if (status == BehaviorStatus.Running) {
                _primaryRunning = true;
            }
            else {
                _primaryRunning = false;
                primary.AfterRun(ctx);
                _primaryFinished = true;
                _primaryResult = status;

                if (!DelayMode) {
                    if (_secondaryRunning) secondary.Interrupt(ctx);
                    Reset();
                    return status;
                }
            }
        }

        if (SecondaryRepeatCount == 0 || _secondaryRepeatsLeft > 0) {
            if (!_secondaryRunning) secondary.BeforeRunInternal(ctx);
            var status = secondary.TickInternal(ctx);

            if (status == BehaviorStatus.Running) {
                _secondaryRunning = true;
            }
            else {
                _secondaryRunning = false;
                secondary.AfterRun(ctx);

                if (DelayMode && _primaryFinished) {
                    var result = _primaryResult;
                    Reset();
                    return result;
                }
                if (_secondaryRepeatsLeft > 0) _secondaryRepeatsLeft--;
            }
        }

        return BehaviorStatus.Running;
    }

    public override void Interrupt(BtContext ctx) {
        if (_primaryRunning) Children[0]?.Interrupt(ctx);
        if (_secondaryRunning) Children[1]?.Interrupt(ctx);
        Reset();
        RunningChild = -1;
    }

    protected override void OnCloned() {
        base.OnCloned();
        Reset();
        _secondaryRepeatsLeft = 0;
    }

    void Reset() {
        _primaryRunning = false;
        _primaryFinished = false;
        _secondaryRunning = false;
    }
}
