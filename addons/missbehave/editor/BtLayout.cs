#if TOOLS
using System;
using System.Collections.Generic;
using Godot;

namespace Missbehave.Editor;

/// <summary>
/// Reingold-Tilford tree layout. Beehave (https://github.com/bitbrain/beehave) lays out its debugger
/// graph with this algorithm and the result looks great — compact, balanced, parents centred over
/// their children — so Missbehave arranges its trees the same way.
/// See https://rachel53461.wordpress.com/2014/04/20/algorithm-for-drawing-trees/
/// <para>
/// Pure apart from writing <see cref="BtLayoutNode.X"/>/<see cref="BtLayoutNode.Y"/>: placing the
/// graph nodes is the caller's job, which keeps this testable without an editor.
/// </para>
/// </summary>
public sealed class BtLayoutNode {
    public float X;
    public float Y;
    public float Mod;

    public BtLayoutNode Parent;
    public readonly List<BtLayoutNode> Children = [];

    public Control Item;

    public BtLayoutNode(Control item = null, BtLayoutNode parent = null) {
        Item = item;
        Parent = parent;
    }

    /// <summary>Extent along the axis siblings are spread on — across, since the tree grows downwards.</summary>
    public float LayoutSize => Item.Size.X;

    public bool IsLeaf => Children.Count == 0;

    public bool IsMostLeft => Parent == null || ReferenceEquals(Parent.Children[0], this);

    public bool IsMostRight => Parent == null || ReferenceEquals(Parent.Children[^1], this);

    public BtLayoutNode PreviousSibling {
        get {
            if (Parent == null || IsMostLeft) return null;
            return Parent.Children[Parent.Children.IndexOf(this) - 1];
        }
    }

    public BtLayoutNode NextSibling {
        get {
            if (Parent == null || IsMostRight) return null;
            return Parent.Children[Parent.Children.IndexOf(this) + 1];
        }
    }

    public BtLayoutNode MostLeftSibling {
        get {
            if (Parent == null) return null;
            return IsMostLeft ? this : Parent.Children[0];
        }
    }

    public BtLayoutNode MostLeftChild => Children.Count == 0 ? null : Children[0];
    public BtLayoutNode MostRightChild => Children.Count == 0 ? null : Children[^1];

    public BtLayoutNode AddChild(Control item) {
        var child = new BtLayoutNode(item, this);
        Children.Add(child);
        return child;
    }
}

public static class BtLayout {
    const float SiblingDistance = 20f + 30f;
    /// <summary>Room between a parent and its children, enough for the wires' sideways run to stand clear of both.</summary>
    const float LevelDistance = 90f;

    /// <summary>
    /// Assigns X/Y to every node of the tree rooted at <paramref name="root"/>, top to bottom:
    /// siblings side by side, each level below the last.
    /// <paramref name="editorScale"/> is passed in rather than read from EditorInterface so the
    /// algorithm stays usable outside the editor.
    /// </summary>
    public static void UpdatePositions(BtLayoutNode root, float editorScale = 1f) {
        InitializeNodes(root, 0);
        CalculateInitialX(root);

        CheckAllChildrenOnScreen(root);
        CalculateFinalPositions(root, 0);

        CalculateY(root, 0, editorScale);
    }

    static void InitializeNodes(BtLayoutNode node, int depth) {
        node.X = -1;
        node.Y = depth;
        node.Mod = 0;

        foreach (var child in node.Children) InitializeNodes(child, depth + 1);
    }

    static void CalculateInitialX(BtLayoutNode node) {
        foreach (var child in node.Children) CalculateInitialX(child);

        if (node.IsLeaf) {
            if (!node.IsMostLeft) {
                var previous = node.PreviousSibling;
                node.X = previous.X + previous.LayoutSize + SiblingDistance;
            }
            else {
                node.X = 0;
            }
        }
        else {
            float mid;
            if (node.Children.Count % 2 == 1) {
                // With an odd count the middle child sits straight below, so its wire runs without a
                // bend. Centring over the outer edges would drift off it whenever the boxes differ in width.
                var middle = node.Children[node.Children.Count / 2];
                mid = middle.X + (middle.LayoutSize - node.LayoutSize) / 2f;
            }
            else {
                var left = node.MostLeftChild;
                var right = node.MostRightChild;
                mid = (left.X + right.X + right.LayoutSize - node.LayoutSize) / 2f;
            }

            if (node.IsMostLeft) {
                node.X = mid;
            }
            else {
                var previous = node.PreviousSibling;
                node.X = previous.X + previous.LayoutSize + SiblingDistance;
                node.Mod = node.X - mid;
            }
        }

        if (!node.IsLeaf && !node.IsMostLeft) CheckForConflicts(node);
    }

    static void CalculateFinalPositions(BtLayoutNode node, float modSum) {
        node.X += modSum;
        modSum += node.Mod;

        foreach (var child in node.Children) CalculateFinalPositions(child, modSum);
    }

    static void CheckAllChildrenOnScreen(BtLayoutNode node) {
        var contour = new Dictionary<int, float>();
        GetLeftContour(node, 0, contour);

        var shift = 0f;
        foreach (var value in contour.Values) {
            if (value + shift < 0) shift = -value;
        }

        if (shift > 0) {
            node.X += shift;
            node.Mod += shift;
        }
    }

    static void CheckForConflicts(BtLayoutNode node) {
        const float minDistance = SiblingDistance;
        var shiftValue = 0f;
        BtLayoutNode shiftSibling = null;

        var nodeContour = new Dictionary<int, float>();
        GetLeftContour(node, 0, nodeContour);
        var nodeMaxLevel = MaxKey(nodeContour);

        var sibling = node.MostLeftSibling;
        while (sibling != null && !ReferenceEquals(sibling, node)) {
            var siblingContour = new Dictionary<int, float>();
            GetRightContour(sibling, 0, siblingContour);

            var upper = Math.Min(MaxKey(siblingContour), nodeMaxLevel);
            for (var level = (int) node.Y + 1; level <= upper; level++) {
                // The GDScript original indexes both contours blindly; a level present in one but
                // not the other would throw here, so skip those instead.
                if (!nodeContour.TryGetValue(level, out var left)) continue;
                if (!siblingContour.TryGetValue(level, out var right)) continue;

                var distance = left - right;
                if (distance + shiftValue < minDistance) {
                    shiftValue = minDistance - distance;
                    shiftSibling = sibling;
                }
            }

            sibling = sibling.NextSibling;
        }

        if (shiftValue > 0) {
            node.X += shiftValue;
            node.Mod += shiftValue;
            CenterNodesBetween(shiftSibling, node);
        }
    }

    static void CenterNodesBetween(BtLayoutNode leftNode, BtLayoutNode rightNode) {
        if (leftNode?.Parent == null) return;

        var siblings = leftNode.Parent.Children;
        var leftIndex = siblings.IndexOf(leftNode);
        var rightIndex = siblings.IndexOf(rightNode);

        var between = rightIndex - leftIndex - 1;
        if (between <= 0) return;

        // Extra room is split evenly across the gaps so the nodes in between end up equally spaced.
        var toAllocate = rightNode.X - leftNode.X - leftNode.LayoutSize;
        for (var i = leftIndex + 1; i < rightIndex; i++) toAllocate -= siblings[i].LayoutSize;
        var spacing = toAllocate / (between + 1);

        var previous = leftNode;
        var middle = leftNode.NextSibling;
        while (middle != null && !ReferenceEquals(middle, rightNode)) {
            var desiredX = previous.X + previous.LayoutSize + spacing;
            var offset = desiredX - middle.X;
            middle.X += offset;
            middle.Mod += offset;
            previous = middle;
            middle = middle.NextSibling;
        }
    }

    static void GetLeftContour(BtLayoutNode node, float modSum, Dictionary<int, float> values) {
        var left = node.X + modSum;
        var depth = (int) node.Y;
        values[depth] = values.TryGetValue(depth, out var existing) ? Mathf.Min(existing, left) : left;

        modSum += node.Mod;
        foreach (var child in node.Children) GetLeftContour(child, modSum, values);
    }

    static void GetRightContour(BtLayoutNode node, float modSum, Dictionary<int, float> values) {
        var right = node.X + modSum + node.LayoutSize;
        var depth = (int) node.Y;
        values[depth] = values.TryGetValue(depth, out var existing) ? Mathf.Max(existing, right) : right;

        modSum += node.Mod;
        foreach (var child in node.Children) GetRightContour(child, modSum, values);
    }

    static void CalculateY(BtLayoutNode node, float offset, float editorScale) {
        node.Y = offset;

        var maxSize = node.Item.Size.Y;
        var sibling = node.MostLeftSibling;
        while (sibling != null) {
            maxSize = Mathf.Max(sibling.Item.Size.Y, maxSize);
            sibling = sibling.NextSibling;
        }

        foreach (var child in node.Children) {
            CalculateY(child, maxSize + offset + LevelDistance * editorScale, editorScale);
        }
    }

    static int MaxKey(Dictionary<int, float> values) {
        var max = int.MinValue;
        foreach (var key in values.Keys) {
            if (key > max) max = key;
        }
        return max;
    }
}
#endif
