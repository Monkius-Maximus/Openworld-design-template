using EconomySystem.Core;
using EconomySystem.Core.Digital;
using Xunit;

namespace EconomySystem.Tests;

public class DigitalIncomeTests
{
    [Fact]
    public void Library_is_indexed_case_insensitively()
    {
        Assert.True(DigitalIncomeLibrary.All.ContainsKey("appgig"));
        Assert.Same(DigitalIncomeLibrary.MarketplaceSale, DigitalIncomeLibrary.All["MarketplaceSale"]);
    }

    [Fact]
    public void AppGig_runs_through_the_same_income_resolver()
    {
        // Prova que a renda digital reusa o esquema v1 sem máquinas novas.
        var resolver = new IncomeResolver();
        var funds = new HouseholdFunds(0);

        resolver.Perform("casa1", funds, DigitalIncomeLibrary.AppGig, skillLevel: 0);

        // Sucesso ou falha, nunca lança e o saldo nunca fica negativo.
        Assert.True(funds.Balance >= 0);
    }

    [Fact]
    public void AppGig_is_available_to_anyone()
    {
        Assert.True(DigitalIncomeLibrary.AppGig.Available(new IncomeContext(0, 0.0)));
    }
}
