using Godot;

namespace Missbehave.Tests;

/// <summary>Test leaf that stops its own runner mid-tick and remembers which runner it saw.</summary>
[GlobalClass, Tool]
public partial class BtProbeStopAction : ActionNode {
    public BehaviorTreeRunner SeenRunner { get; private set; }

    protected override BehaviorStatus Run(BtContext ctx) {
        SeenRunner = ctx.Runner;
        ctx.Runner?.Stop();
        return BehaviorStatus.Success;
    }
}
