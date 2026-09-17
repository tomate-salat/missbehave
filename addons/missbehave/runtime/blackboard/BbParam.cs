using System;
using Godot;

namespace Missbehave;

/// <summary>Untyped view of a <see cref="BbParam{T}"/>, for the editor and for serialization.</summary>
public interface IBbParam {
    Type ValueType { get; }

    /// <summary>The fixed value, used whenever the parameter is not linked to an entry.</summary>
    Variant Literal { get; set; }

    /// <summary>Id of the linked blackboard entry, or empty for a fixed value.</summary>
    string EntryId { get; set; }

    /// <summary>
    /// The linked entry's name when it was last seen by the editor. Display only — the link itself is
    /// <see cref="EntryId"/>, and renaming the entry updates this copy.
    /// </summary>
    string EntryName { get; set; }

    bool IsLinked { get; }

    /// <summary>Reads the current value and remembers the context; called by the node before it runs and ticks.</summary>
    void Refresh(BtContext ctx);
}

/// <summary>
/// A node parameter that is either a fixed value or a link to an entry on the tree's blackboard —
/// which one is chosen in the Inspector, not in code:
/// <code>
/// BbParam&lt;float&gt; Speed { get; set; } = 4f;
/// BbParam&lt;Node3D&gt; Target { get; set; }
///
/// protected override BehaviorStatus Run(BtContext ctx) {
///     Target.Value.GlobalPosition += Vector3.Forward * Speed.Value * (float) ctx.Delta;
/// </code>
/// <see cref="Value"/> is read fresh before <c>BeforeRun</c> and before every tick of the node, so
/// it is the value as of the start of that tick. <see cref="Get"/> reads the blackboard right now —
/// only needed when something earlier in the same tick may just have changed it.
/// No <c>[Export]</c>: Godot cannot export a generic type, so <see cref="ABehaviorNode"/> finds these
/// members by their type and stores them itself. Nor any <c>new()</c>: a parameter left without an
/// initializer is created by the node's constructor, and a plain value converts into one.
/// <para>
/// A class rather than a struct on purpose: members are properties, and a struct property hands out
/// copies — an unlinked <see cref="Set"/> would write into a copy and be lost.
/// </para>
/// </summary>
public sealed class BbParam<[MustBeVariant] T> : IBbParam {
    T _literal;

    /// <summary>What <see cref="Value"/> holds, and whether it has been read yet.</summary>
    T _value;
    bool _refreshed;
    BtContext _ctx;

    public BbParam() { }

    public BbParam(T literal) => _literal = literal;

    /// <summary>
    /// The value as of the start of this node's current tick. Assigning writes it through
    /// <see cref="Set"/> right away, so the blackboard and every later reader see it too. Before the
    /// node first runs — in the editor, say — it is the fixed value.
    /// </summary>
    public T Value {
        get => _refreshed ? _value : _literal;
        set => Set(_ctx, value);
    }

    void IBbParam.Refresh(BtContext ctx) {
        _ctx = ctx;
        _value = Get(ctx);
        _refreshed = true;
    }

    /// <summary>Lets a fixed starting value be written as just the value: <c>= 4f</c>.</summary>
    public static implicit operator BbParam<T>(T literal) => new(literal);

    /// <summary>The fixed value. Also what <see cref="Get"/> falls back on when a linked entry holds nothing.</summary>
    public T Literal {
        get => _literal;
        set => _literal = value;
    }

    public string EntryId { get; set; } = "";
    public string EntryName { get; set; } = "";
    public bool IsLinked => !string.IsNullOrEmpty(EntryId);

    public T Get(BtContext ctx) {
        if (IsLinked && ctx.Blackboard != null && ctx.Blackboard.TryGetById(EntryId, out var value)
            && BbTypes.TryConvert<T>(value, out var typed)) {
            return typed;
        }
        return _literal;
    }

    /// <summary>
    /// Writes to the linked entry. An unlinked parameter keeps the value itself instead, which is
    /// private to the runner — every runner ticks its own copy of the node.
    /// </summary>
    public void Set(BtContext ctx, T value) {
        if (IsLinked && ctx.Blackboard != null) ctx.Blackboard.SetById(EntryId, Variant.From(value));
        else _literal = value;
        if (_refreshed) _value = value;
    }

    public override string ToString() => IsLinked ? EntryName : BbTypes.Format(Variant.From(_literal));

    Type IBbParam.ValueType => typeof(T);

    Variant IBbParam.Literal {
        get => Variant.From(_literal);
        set {
            if (BbTypes.TryConvert<T>(value, out var typed)) _literal = typed;
        }
    }
}
