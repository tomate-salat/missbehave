using Godot;

namespace Missbehave.Tests;

/// <summary>Leaf holding a reference to a shared asset, to prove cloning does not deep-copy it.</summary>
[GlobalClass, Tool]
public partial class BtProbeAssetUser : ActionNode {
    [Export]
    public BtProbeAsset Asset { get; set; }

    protected override BehaviorStatus Run(BtContext ctx) => BehaviorStatus.Success;
}
