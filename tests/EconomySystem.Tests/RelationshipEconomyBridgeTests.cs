using EconomySystem.Core;
using EconomySystem.Core.Businesses;
using EconomySystem.Core.Integration;
using RelationshipSystem.Core;
using Xunit;

namespace EconomySystem.Tests;

public class RelationshipEconomyBridgeTests
{
    // Cria amizade mútua A<->B (ambos os lados daily >= limiar de amizade).
    private static void MakeFriends(RelationshipMatrix m, string a, string b)
    {
        m.Get(a, b).Value.ApplyDaily(60f);
        m.Get(b, a).Value.ApplyDaily(60f);
    }

    [Fact]
    public void CountFriends_matches_mutual_friendships()
    {
        var matrix = new RelationshipMatrix();
        MakeFriends(matrix, "alice", "bob");
        MakeFriends(matrix, "alice", "carol");
        // Dave gosta de Alice, mas não é mútuo.
        matrix.Get("dave", "alice").Value.ApplyDaily(60f);

        var bridge = new RelationshipEconomyBridge(matrix);

        Assert.Equal(2, bridge.CountFriends("alice"));
    }

    [Fact]
    public void Higher_affinity_raises_purchase_willingness()
    {
        var matrix = new RelationshipMatrix();
        matrix.Get("customer", "owner").Value.ApplyDaily(80f);
        var bridge = new RelationshipEconomyBridge(matrix);

        double willing = bridge.PurchaseWillingness("owner", "customer", BusinessRules.DefaultMarkupPercent);
        double stranger = bridge.PurchaseWillingness("owner", "nobody", BusinessRules.DefaultMarkupPercent);

        Assert.True(willing > stranger);
    }

    [Fact]
    public void Satisfying_sale_writes_good_service_modifier_back_to_relationship()
    {
        var matrix = new RelationshipMatrix();
        matrix.Get("customer", "owner").Value.ApplyDaily(100f); // willingness = 1.0 -> compra certa
        var bridge = new RelationshipEconomyBridge(matrix);
        var resolver = new BusinessResolver();
        var funds = new HouseholdFunds(0);

        var business = new Business { Id = "loja", OwnerId = "owner" };
        business.Stock["camisa"] = new StockItem { Id = "camisa", UnitCost = 50, Quantity = 1 };

        int profit = bridge.SellViaRelationship(resolver, business, funds, "customer", "camisa");

        Assert.True(profit > 0);
        var rel = matrix.Get("customer", "owner");
        Assert.Contains(rel.Modifiers, m => m.Name == "Bom atendimento");
    }

    [Fact]
    public void Null_matrix_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new RelationshipEconomyBridge(null!));
    }
}
