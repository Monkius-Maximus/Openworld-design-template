namespace EconomySystem.Core.Careers;

/// <summary>
/// Catálogo de chance cards prontos, indexados por Id. Espelha o
/// <c>InteractionLibrary</c>. Cada card oferece duas opções com desfechos
/// distintos (uma visão moderna sobre os cards do The Sims 2).
/// </summary>
public static class ChanceCardLibrary
{
    /// <summary>Negócios: um cliente importante propõe um acordo arriscado.</summary>
    public static readonly ChanceCard BusinessDeal = new()
    {
        Id = "BusinessDeal",
        CareerId = "Business",
        Prompt = "Um cliente importante quer fechar um grande acordo informalmente. Aceitar o risco?",
        OptionA = new ChanceCardOutcome("Fechar o acordo", 1_200, CareerEffect.Promote),
        OptionB = new ChanceCardOutcome("Recusar e jogar seguro", 0, CareerEffect.None),
        AtLevel = 3,
    };

    /// <summary>Culinária: um crítico gastronômico aparece de surpresa.</summary>
    public static readonly ChanceCard FoodCritic = new()
    {
        Id = "FoodCritic",
        CareerId = "Culinary",
        Prompt = "Um crítico gastronômico chega sem aviso. Improvisar um prato ousado?",
        OptionA = new ChanceCardOutcome("Arriscar o prato ousado", 800, CareerEffect.Promote),
        OptionB = new ChanceCardOutcome("Servir o de sempre", -200, CareerEffect.Demote),
        AtLevel = 2,
    };

    /// <summary>Gig: um app oferece um bônus por dirigir na madrugada.</summary>
    public static readonly ChanceCard SurgeBonus = new()
    {
        Id = "SurgeBonus",
        CareerId = "GigCourier",
        Prompt = "O app oferece tarifa dinâmica na madrugada. Encarar a jornada extra?",
        OptionA = new ChanceCardOutcome("Aceitar a madrugada", 300, CareerEffect.None),
        OptionB = new ChanceCardOutcome("Descansar", 0, CareerEffect.None),
    };

    /// <summary>Todos os cards registrados, indexados por Id.</summary>
    public static readonly IReadOnlyDictionary<string, ChanceCard> All =
        new[] { BusinessDeal, FoodCritic, SurgeBonus }
            .ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);
}
