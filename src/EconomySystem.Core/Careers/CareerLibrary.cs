namespace EconomySystem.Core.Careers;

/// <summary>
/// Catálogo de carreiras prontas, indexadas por Id (case-insensitive). Espelha
/// o <c>InteractionLibrary</c>. Inclui pistas tradicionais (estilo The Sims 2) e
/// modernas (remoto/gig) — a modernização da v2. Salários e requisitos crescem
/// a cada nível; o gate de amigos integra-se ao <c>RelationshipMatrix</c>.
/// </summary>
public static class CareerLibrary
{
    private static IReadOnlyDictionary<string, int> Skills(params (string Name, int Level)[] reqs) =>
        reqs.ToDictionary(r => r.Name, r => r.Level, StringComparer.OrdinalIgnoreCase);

    /// <summary>Carreira de Negócios tradicional (escada 9-às-17).</summary>
    public static readonly CareerDefinition Business = new()
    {
        Id = "Business",
        DisplayName = "Negócios",
        Track = CareerTrack.Traditional,
        Levels = new[]
        {
            new CareerLevel(1, "Estagiário",        140, 0, Skills(), 0),
            new CareerLevel(2, "Executivo Júnior",  200, 1, Skills(("Charisma", 1)), 30),
            new CareerLevel(3, "Gerente",           320, 3, Skills(("Charisma", 3), ("Logic", 2)), 40),
            new CareerLevel(4, "Vice-Presidente",   520, 6, Skills(("Charisma", 5), ("Logic", 4)), 50),
            new CareerLevel(5, "CEO",               850, 10, Skills(("Charisma", 8), ("Logic", 6)), 60),
        },
    };

    /// <summary>Carreira Culinária tradicional.</summary>
    public static readonly CareerDefinition Culinary = new()
    {
        Id = "Culinary",
        DisplayName = "Culinária",
        Track = CareerTrack.Traditional,
        Levels = new[]
        {
            new CareerLevel(1, "Lavador de Pratos", 120, 0, Skills(), 0),
            new CareerLevel(2, "Cozinheiro",        190, 1, Skills(("Cooking", 2)), 30),
            new CareerLevel(3, "Chef de Partie",    300, 2, Skills(("Cooking", 4), ("Cleaning", 2)), 40),
            new CareerLevel(4, "Sous Chef",         470, 4, Skills(("Cooking", 6), ("Charisma", 3)), 50),
            new CareerLevel(5, "Chef Executivo",    760, 7, Skills(("Cooking", 9), ("Charisma", 5)), 60),
        },
    };

    /// <summary>Desenvolvimento de software REMOTO (moderno: sem deslocamento).</summary>
    public static readonly CareerDefinition RemoteSoftware = new()
    {
        Id = "RemoteSoftware",
        DisplayName = "Desenvolvimento Remoto",
        Track = CareerTrack.Remote,
        Levels = new[]
        {
            new CareerLevel(1, "Dev Júnior",        220, 0, Skills(("Logic", 1)), 0),
            new CareerLevel(2, "Dev Pleno",         360, 1, Skills(("Logic", 3)), 30),
            new CareerLevel(3, "Dev Sênior",        560, 2, Skills(("Logic", 5), ("Creativity", 2)), 40),
            new CareerLevel(4, "Tech Lead",         820, 4, Skills(("Logic", 7), ("Charisma", 4)), 50),
            new CareerLevel(5, "Arquiteto",        1180, 6, Skills(("Logic", 9), ("Charisma", 5)), 55),
        },
    };

    /// <summary>Entregador por app (gig economy moderna: escada curta, baixa barreira).</summary>
    public static readonly CareerDefinition GigCourier = new()
    {
        Id = "GigCourier",
        DisplayName = "Entregas por App",
        Track = CareerTrack.Gig,
        Levels = new[]
        {
            new CareerLevel(1, "Entregador",        90,  0, Skills(), 0),
            new CareerLevel(2, "Entregador Pro",    150, 0, Skills(("Body", 2)), 20),
            new CareerLevel(3, "Entregador Elite",  220, 1, Skills(("Body", 4)), 30),
        },
    };

    /// <summary>Todas as carreiras registradas, indexadas por Id.</summary>
    public static readonly IReadOnlyDictionary<string, CareerDefinition> All =
        new[] { Business, Culinary, RemoteSoftware, GigCourier }
            .ToDictionary(d => d.Id, StringComparer.OrdinalIgnoreCase);
}
