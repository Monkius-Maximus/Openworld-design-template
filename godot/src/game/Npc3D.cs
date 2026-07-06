using System;
using BuildSystem.Movement;
using Godot;

namespace BuildSystem.Game;

/// <summary>
/// NPC do mundo 3D: corpo estático com uma Area3D de proximidade. Quando o
/// jogador entra no raio, o GameManager mostra o prompt de interação (F) e
/// abre o menu que conversa com o GameWorld via <c>CharacterId</c>.
/// </summary>
public partial class Npc3D : StaticBody3D
{
    public const string GroupName = "npcs";

    /// <summary>Id do personagem no CharacterRegistry do mundo.</summary>
    [Export] public string CharacterId { get; set; } = string.Empty;

    [Export] public string DisplayName { get; set; } = string.Empty;

    [Export] public Area3D ProximityArea { get; set; } = null!;

    public bool PlayerInRange { get; private set; }

    public override void _Ready()
    {
        if (string.IsNullOrWhiteSpace(CharacterId))
            throw new InvalidOperationException($"{nameof(Npc3D)} '{Name}' requires {nameof(CharacterId)}.");
        if (ProximityArea is null)
            throw new InvalidOperationException($"{nameof(Npc3D)} '{Name}' requires {nameof(ProximityArea)}.");

        AddToGroup(GroupName);
        ProximityArea.BodyEntered += OnBodyEntered;
        ProximityArea.BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is PlayerController3D)
            PlayerInRange = true;
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is PlayerController3D)
            PlayerInRange = false;
    }
}
