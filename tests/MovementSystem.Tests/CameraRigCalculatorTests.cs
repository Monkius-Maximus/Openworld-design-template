using System.Numerics;
using MovementSystem.Core;
using Xunit;

namespace MovementSystem.Tests;

public class CameraRigCalculatorTests
{
    private const int Precision = 4;

    [Fact]
    public void SmoothFollow_converges_to_target()
    {
        var target = new Vector3(10f, 0f, -5f);
        Vector3 pos = Vector3.Zero;
        for (int i = 0; i < 600; i++)
            pos = CameraRigCalculator.SmoothFollow(pos, target, MovementThresholds.CameraFollowSharpness, 1f / 60f);

        Assert.Equal(target.X, pos.X, 2);
        Assert.Equal(target.Z, pos.Z, 2);
    }

    [Fact]
    public void SmoothFollow_never_passes_the_target()
    {
        var target = new Vector3(10f, 0f, 0f);
        Vector3 pos = CameraRigCalculator.SmoothFollow(Vector3.Zero, target, 100f, 1f);
        Assert.True(pos.X <= target.X);
        Assert.True(pos.X > 0f);
    }

    [Fact]
    public void AimAnchor_moves_a_fraction_toward_cursor()
    {
        var player = new Vector3(0f, 1f, 0f);
        var aim = new Vector3(2f, 1f, 0f);
        Vector3 anchor = CameraRigCalculator.AimAnchor(player, aim, factor: 0.5f, maxOffset: 10f);

        Assert.Equal(1f, anchor.X, Precision);
        Assert.Equal(player.Y, anchor.Y, Precision); // look-ahead só no plano XZ
    }

    [Fact]
    public void AimAnchor_clamps_to_max_offset()
    {
        var player = Vector3.Zero;
        var aim = new Vector3(100f, 0f, 0f);
        Vector3 anchor = CameraRigCalculator.AimAnchor(
            player, aim, MovementThresholds.AimLookAheadFactor, MovementThresholds.AimLookAheadMax);

        Assert.Equal(MovementThresholds.AimLookAheadMax, (anchor - player).Length(), Precision);
    }

    [Fact]
    public void Zoom_in_shrinks_size_and_clamps_at_min()
    {
        float size = MovementThresholds.CameraMinSize + 1f;
        size = CameraRigCalculator.ApplyZoomSteps(size, steps: 1);
        Assert.True(size < MovementThresholds.CameraMinSize + 1f);

        for (int i = 0; i < 50; i++)
            size = CameraRigCalculator.ApplyZoomSteps(size, steps: 1);
        Assert.Equal(MovementThresholds.CameraMinSize, size, Precision);
    }

    [Fact]
    public void Zoom_out_clamps_at_max()
    {
        float size = MovementThresholds.CameraMaxSize - 1f;
        for (int i = 0; i < 50; i++)
            size = CameraRigCalculator.ApplyZoomSteps(size, steps: -1);
        Assert.Equal(MovementThresholds.CameraMaxSize, size, Precision);
    }
}
