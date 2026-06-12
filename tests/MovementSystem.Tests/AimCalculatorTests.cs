using System.Numerics;
using MovementSystem.Core;
using Xunit;

namespace MovementSystem.Tests;

public class AimCalculatorTests
{
    private const int Precision = 4;

    [Fact]
    public void Ray_pointing_down_hits_plane_below()
    {
        var origin = new Vector3(2f, 10f, -3f);
        var direction = new Vector3(0f, -1f, 0f);

        Assert.True(AimCalculator.TryIntersectPlane(origin, direction, 0f, out Vector3 hit));
        Assert.Equal(2f, hit.X, Precision);
        Assert.Equal(0f, hit.Y, Precision);
        Assert.Equal(-3f, hit.Z, Precision);
    }

    [Fact]
    public void Diagonal_ray_lands_where_expected()
    {
        // desce 1 em Y a cada 1 em X: partindo de y=4, anda 4 em X até o plano y=0
        var origin = new Vector3(0f, 4f, 0f);
        var direction = Vector3.Normalize(new Vector3(1f, -1f, 0f));

        Assert.True(AimCalculator.TryIntersectPlane(origin, direction, 0f, out Vector3 hit));
        Assert.Equal(4f, hit.X, Precision);
        Assert.Equal(0f, hit.Y, Precision);
    }

    [Fact]
    public void Parallel_ray_misses()
        => Assert.False(AimCalculator.TryIntersectPlane(new Vector3(0f, 5f, 0f), new Vector3(1f, 0f, 0f), 0f, out _));

    [Fact]
    public void Plane_behind_ray_misses()
        => Assert.False(AimCalculator.TryIntersectPlane(new Vector3(0f, 5f, 0f), new Vector3(0f, 1f, 0f), 0f, out _));

    [Fact]
    public void Yaw_zero_faces_negative_z()
        => Assert.Equal(0f, AimCalculator.YawFromDirection(new Vector3(0f, 0f, -1f)), Precision);

    [Fact]
    public void Yaw_half_pi_faces_negative_x()
        => Assert.Equal(MathF.PI / 2f, AimCalculator.YawFromDirection(new Vector3(-1f, 0f, 0f)), Precision);

    [Fact]
    public void Yaw_towards_coincident_points_fails()
        => Assert.False(AimCalculator.TryYawTowards(new Vector3(1f, 0f, 1f), new Vector3(1f, 5f, 1f), out _));

    [Fact]
    public void Yaw_towards_target_ignores_height()
    {
        Assert.True(AimCalculator.TryYawTowards(Vector3.Zero, new Vector3(0f, 3f, -10f), out float yaw));
        Assert.Equal(0f, yaw, Precision);
    }

    [Fact]
    public void MoveTowardAngle_takes_shortest_path_through_wrap()
    {
        // de 170° para -170°: o caminho curto (20°) passa por 180°, não por 0°
        float current = MathF.PI * 170f / 180f;
        float target = -MathF.PI * 170f / 180f;
        float step = MathF.PI * 10f / 180f;

        float result = AimCalculator.MoveTowardAngle(current, target, step);
        Assert.Equal(MathF.PI * 180f / 180f, MathF.Abs(result), Precision);
    }

    [Fact]
    public void MoveTowardAngle_snaps_when_within_step()
        => Assert.Equal(0.2f, AimCalculator.MoveTowardAngle(0.1f, 0.2f, 1f), Precision);

    [Fact]
    public void WrapAngle_maps_into_range()
    {
        Assert.Equal(0f, AimCalculator.WrapAngle(2f * MathF.PI), Precision);
        Assert.Equal(-MathF.PI / 2f, AimCalculator.WrapAngle(3f * MathF.PI / 2f), Precision);
    }
}
