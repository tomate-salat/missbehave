using Godot;

namespace Missbehave.Demo;

/// <summary>Tiny lookup shared by the demo leaves.</summary>
public static class BtDemoWorld {
    public const string PlayerGroup = "player";

    public static Node3D Player(Node from)
        => from?.GetTree()?.GetFirstNodeInGroup(PlayerGroup) as Node3D;
}
