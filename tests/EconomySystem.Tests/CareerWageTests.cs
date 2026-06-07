using EconomySystem.Core;
using EconomySystem.Core.Careers;
using Xunit;

namespace EconomySystem.Tests;

public class CareerWageTests
{
    [Fact]
    public void PayDailyWage_credits_level_wage()
    {
        var resolver = new CareerResolver();
        var funds = new HouseholdFunds(0);
        var state = new CareerState { CharacterId = "alice", Career = CareerLibrary.Business };

        resolver.PayDailyWage("casa1", funds, state);

        Assert.Equal(140, funds.Balance); // salário do nível 1 de Negócios
    }

    [Fact]
    public void DailyTick_pays_wages_of_employed_members()
    {
        var tick = new EconomyTickSystem(new BillsSystem());
        var h = new Household { Id = "casa1", Funds = new HouseholdFunds(0) };
        h.Careers["alice"] = new CareerState { CharacterId = "alice", Career = CareerLibrary.GigCourier };

        tick.DailyTick(h, DayOfWeek.Monday); // segunda: sem conta, só salário

        Assert.Equal(90, h.Funds.Balance); // salário do nível 1 de Entregas
    }

    [Fact]
    public void Wage_event_fires_through_resolver()
    {
        var resolver = new CareerResolver();
        var funds = new HouseholdFunds(0);
        MoneyTransaction? paid = null;
        resolver.WagePaid += (_, tx) => paid = tx;

        resolver.PayDailyWage("casa1", funds, new CareerState
        {
            CharacterId = "alice",
            Career = CareerLibrary.RemoteSoftware,
        });

        Assert.NotNull(paid);
        Assert.Equal(TransactionKind.Wage, paid!.Kind);
        Assert.Equal(220, paid.Amount);
    }
}
