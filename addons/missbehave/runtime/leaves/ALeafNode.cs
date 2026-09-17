using Godot;

namespace Missbehave;

/// <summary>Base of every node that does actual work and has no children.</summary>
[GlobalClass, Tool]
public abstract partial class ALeafNode : ABehaviorNode {
    public override int MinChildren => 0;
    public override int MaxChildren => 0;
    public override string Category => BtCategory.Leaf;
}
