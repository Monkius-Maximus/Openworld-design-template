using EconomySystem.Core.Businesses;
using RelationshipSystem.Core;

namespace EconomySystem.Core.Integration;

/// <summary>
/// Costura única entre a economia e o núcleo de relacionamentos. Só LÊ o
/// <see cref="RelationshipMatrix"/> e ESCREVE de volta pelas APIs públicas já
/// existentes (<c>AddModifier</c>, <c>Value</c>) — o núcleo de relacionamentos
/// permanece intocado e livre de dependências.
/// </summary>
public sealed class RelationshipEconomyBridge
{
    private static readonly Random Rng = Random.Shared;
    private readonly RelationshipMatrix _matrix;

    public RelationshipEconomyBridge(RelationshipMatrix matrix)
    {
        _matrix = matrix ?? throw new ArgumentNullException(nameof(matrix));
    }

    /// <summary>
    /// Conta quantos amigos (mútuos) um personagem tem, reusando a regra
    /// existente <see cref="RelationshipMatrix.AreFriends"/>. Será o gatilho de
    /// promoções gated por amigos quando carreiras entrarem (fase posterior).
    /// </summary>
    public int CountFriends(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            throw new ArgumentException("characterId required", nameof(characterId));

        return _matrix.All
            .Where(r => r.FromId == characterId)
            .Select(r => r.ToId)
            .Distinct()
            .Count(other => _matrix.AreFriends(characterId, other));
    }

    /// <summary>
    /// Probabilidade [0..1] de um cliente comprar: sobe com a simpatia
    /// (EffectiveDaily do cliente pelo dono) e cai com markup acima do padrão.
    /// Função pura — fácil de testar.
    /// </summary>
    public double PurchaseWillingness(string ownerId, string customerId, int markupPercent)
    {
        float affinity = _matrix.Get(customerId, ownerId).EffectiveDaily; // -100..100
        double fromAffinity = 0.5 + affinity / 200.0;                     // 0..1
        double markupPenalty = (markupPercent - BusinessRules.DefaultMarkupPercent) / 200.0;
        return Math.Clamp(fromAffinity - markupPenalty, 0.0, 1.0);
    }

    /// <summary>
    /// Vende ao cliente decidindo a compra a partir do relacionamento. Numa
    /// venda satisfatória, devolve um modificador "Bom atendimento" ao
    /// relacionamento cliente→dono (reusa o mecanismo de modificador do CK3).
    /// Retorna o lucro (0 se o cliente não comprou).
    /// </summary>
    public int SellViaRelationship(BusinessResolver resolver, Business business, HouseholdFunds funds, string customerId, string itemId)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(business);

        double willingness = PurchaseWillingness(business.OwnerId, customerId, business.MarkupPercent);
        bool buys = Rng.NextDouble() < willingness;

        int profit = resolver.SellTo(business, funds, customerId, itemId, buys);

        if (buys)
        {
            _matrix.Get(customerId, business.OwnerId).AddModifier(new RelationshipModifier
            {
                Name = "Bom atendimento",
                Value = BusinessRules.GoodServiceModifierValue,
                RemainingHours = BusinessRules.GoodServiceModifierHours,
            });
        }

        return profit;
    }

    /// <summary>
    /// Quando o repo-man é acionado, anexa uma "memória ruim" (modificador
    /// negativo) entre os moradores do domicílio — reusando o mesmo mecanismo
    /// de modificador temporário. Não cria primitiva nova.
    /// </summary>
    public void ApplyRepoManMemory(IReadOnlyList<string> memberIds)
    {
        ArgumentNullException.ThrowIfNull(memberIds);

        foreach (var a in memberIds)
        foreach (var b in memberIds)
        {
            if (a == b) continue;
            _matrix.Get(a, b).AddModifier(new RelationshipModifier
            {
                Name = "Repo-man levou as coisas",
                Value = -10f,
                RemainingHours = 48f,
            });
        }
    }
}
