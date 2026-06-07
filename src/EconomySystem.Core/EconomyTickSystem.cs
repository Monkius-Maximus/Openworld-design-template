namespace EconomySystem.Core;

/// <summary>
/// Passagem de tempo da economia. Espelha o <c>RelationshipDecaySystem</c>:
/// um tick diário que percorre os domicílios. Mantém o loop fechado
/// (ganhar → guardar → gastar → o tempo passa).
/// </summary>
public sealed class EconomyTickSystem
{
    private readonly BillsSystem _bills;

    public EconomyTickSystem(BillsSystem bills)
    {
        _bills = bills ?? throw new ArgumentNullException(nameof(bills));
    }

    /// <summary>
    /// Chamado 1x por dia do jogo para um domicílio: deprecia os bens, entrega
    /// contas nos dias de cobrança (terça/quinta) e avança a tolerância do
    /// repo-man.
    /// </summary>
    public void DailyTick(Household household, DayOfWeek day, int? gameDay = null)
    {
        ArgumentNullException.ThrowIfNull(household);

        // 1. Bens depreciam.
        foreach (var obj in household.Inventory.All)
            obj.DepreciateOneDay(EconomyPhysics.DepreciationPercentPerDay);

        // 2. Avança tolerância de contas atrasadas (pode acionar o repo-man).
        _bills.AdvanceGrace(household);

        // 3. Em dia de cobrança, entrega e tenta debitar a conta.
        if (EconomyPhysics.BillDeliveryDays.Contains(day))
            _bills.DeliverAndCharge(household, gameDay);

        // TODO (v2): pagar salários de carreira e debitar assinaturas digitais.
        // TODO (v3): folha de pagamento dos funcionários do negócio.
    }

    /// <summary>Conveniência: roda o DailyTick para vários domicílios.</summary>
    public void DailyTick(IEnumerable<Household> households, DayOfWeek day, int? gameDay = null)
    {
        ArgumentNullException.ThrowIfNull(households);
        foreach (var h in households)
            DailyTick(h, day, gameDay);
    }
}
