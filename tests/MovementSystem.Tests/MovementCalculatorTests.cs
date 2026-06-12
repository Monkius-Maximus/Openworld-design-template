using System.Numerics;
using MovementSystem.Core;
using Xunit;

namespace MovementSystem.Tests;

public class MovementCalculatorTests
{
    private const int Precision = 4;

    [Fact]
    public void W_with_camera_at_rest_moves_negative_z()
    {
        // convenção Input.GetVector do Godot: W = (0, -1)
        Vector3 dir = MovementCalculator.DirectionFromInput(new Vector2(0f, -1f), cameraYaw: 0f);
        Assert.Equal(0f, dir.X, Precision);
        Assert.Equal(-1f, dir.Z, Precision);
    }

    [Fact]
    public void Input_rotates_with_camera_yaw()
    {
        // câmera girada 90° CCW: "cima na tela" passa a ser -X
        Vector3 dir = MovementCalculator.DirectionFromInput(new Vector2(0f, -1f), cameraYaw: MathF.PI / 2f);
        Assert.Equal(-1f, dir.X, Precision);
        Assert.Equal(0f, dir.Z, Precision);
    }

    [Fact]
    public void Oversized_input_is_normalized()
    {
        Vector3 dir = MovementCalculator.DirectionFromInput(new Vector2(1f, 1f), cameraYaw: 0f);
        Assert.Equal(1f, dir.Length(), Precision);
    }

    [Fact]
    public void Zero_input_gives_zero_direction()
        => Assert.Equal(Vector3.Zero, MovementCalculator.DirectionFromInput(Vector2.Zero, cameraYaw: 1.3f));

    [Fact]
    public void Moving_into_facing_has_full_speed()
    {
        // yaw 0 encara -Z; mover-se para -Z é "de frente"
        float m = MovementCalculator.DirectionalSpeedMultiplier(0f, new Vector3(0f, 0f, -1f));
        Assert.Equal(1f, m, Precision);
    }

    [Fact]
    public void Backpedal_is_slowest()
    {
        float m = MovementCalculator.DirectionalSpeedMultiplier(0f, new Vector3(0f, 0f, 1f));
        Assert.Equal(MovementThresholds.BackpedalMultiplier, m, Precision);
    }

    [Fact]
    public void Strafe_sits_between_forward_and_backpedal()
    {
        float m = MovementCalculator.DirectionalSpeedMultiplier(0f, new Vector3(1f, 0f, 0f));
        Assert.Equal(MovementThresholds.StrafeMultiplier, m, Precision);
        Assert.True(m < 1f);
        Assert.True(m > MovementThresholds.BackpedalMultiplier);
    }

    [Fact]
    public void Standing_still_has_no_penalty()
        => Assert.Equal(1f, MovementCalculator.DirectionalSpeedMultiplier(0.7f, Vector3.Zero), Precision);

    [Fact]
    public void Accelerate_does_not_overshoot()
    {
        var desired = new Vector3(0f, 0f, -3f);
        Vector3 v = Vector3.Zero;
        for (int i = 0; i < 120; i++)
            v = MovementCalculator.Accelerate(v, desired, 1f / 60f);

        Assert.Equal(desired.Z, v.Z, Precision);
        Assert.True(v.Length() <= desired.Length() + 1e-4f);
    }

    [Fact]
    public void Accelerate_moves_toward_target_each_step()
    {
        var desired = new Vector3(0f, 0f, -3f);
        Vector3 v = MovementCalculator.Accelerate(Vector3.Zero, desired, 1f / 60f);
        Assert.True(v.Z < 0f);
        Assert.True(v.Length() < desired.Length());
    }

    [Fact]
    public void Braking_reaches_zero()
    {
        Vector3 v = new(0f, 0f, -6f);
        for (int i = 0; i < 120; i++)
            v = MovementCalculator.Accelerate(v, Vector3.Zero, 1f / 60f);
        Assert.Equal(0f, v.Length(), Precision);
    }
}
