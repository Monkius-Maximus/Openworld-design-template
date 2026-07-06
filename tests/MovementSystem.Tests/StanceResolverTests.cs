using MovementSystem.Core;
using Xunit;

namespace MovementSystem.Tests;

public class StanceResolverTests
{
    [Fact]
    public void Default_is_walk()
        => Assert.Equal(MovementStance.Walk, StanceResolver.Resolve(sneak: false, run: false, sprint: false, aiming: false));

    [Fact]
    public void Sprint_beats_run()
        => Assert.Equal(MovementStance.Sprint, StanceResolver.Resolve(sneak: false, run: true, sprint: true, aiming: false));

    [Fact]
    public void Sneak_beats_everything()
        => Assert.Equal(MovementStance.Sneak, StanceResolver.Resolve(sneak: true, run: true, sprint: true, aiming: true));

    [Fact]
    public void Aiming_caps_run_and_sprint_to_walk()
    {
        Assert.Equal(MovementStance.Walk, StanceResolver.Resolve(sneak: false, run: true, sprint: false, aiming: true));
        Assert.Equal(MovementStance.Walk, StanceResolver.Resolve(sneak: false, run: false, sprint: true, aiming: true));
    }

    [Fact]
    public void Aim_walk_is_slower_than_normal_walk()
    {
        float aimSpeed = StanceResolver.SpeedFor(MovementStance.Walk, aiming: true);
        float walkSpeed = StanceResolver.SpeedFor(MovementStance.Walk);
        Assert.True(aimSpeed < walkSpeed);
        Assert.Equal(MovementThresholds.AimWalkSpeed, aimSpeed);
    }

    [Fact]
    public void Speeds_grow_with_stance()
    {
        Assert.True(StanceResolver.SpeedFor(MovementStance.Sneak) < StanceResolver.SpeedFor(MovementStance.Walk));
        Assert.True(StanceResolver.SpeedFor(MovementStance.Walk) < StanceResolver.SpeedFor(MovementStance.Run));
        Assert.True(StanceResolver.SpeedFor(MovementStance.Run) < StanceResolver.SpeedFor(MovementStance.Sprint));
    }
}
