using Godot;

namespace Missbehave.Tests;

/// <summary>Stand-in for a real game asset referenced by a leaf, e.g. weapon stats.</summary>
[GlobalClass, Tool]
public partial class BtProbeAsset : Resource {
    [Export]
    public int Value { get; set; }
}
