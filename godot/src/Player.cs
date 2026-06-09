using Godot;

namespace BuildSystem;

public partial class Player : CharacterBody2D
{
    /// <summary>
    /// Único gancho para suspender o controle (D25). O veículo futuro usa o mesmo.
    /// </summary>
    public bool ControlEnabled { get; set; } = true;

    [Export] public float Speed { get; set; } = 200f;

    public override void _PhysicsProcess(double delta)
    {
        if (!ControlEnabled)
        {
            Velocity = Vector2.Zero;
            return;
        }

        Velocity = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down") * Speed;
        MoveAndSlide();
    }
}
