namespace EconomySystem.Core;

/// <summary>Movimentação de caixa (entrada ou saída) num domicílio.</summary>
public delegate void MoneyEventHandler(string householdId, MoneyTransaction tx);

/// <summary>Conta entregue: valor devido e se foi paga.</summary>
public delegate void BillEventHandler(string householdId, int amountDue, bool paid);

/// <summary>Venda concluída num negócio: comprador, item e lucro.</summary>
public delegate void SaleEventHandler(string ownerId, string customerId, int profit);

/// <summary>Promoção: o personagem alcançou um novo nível de carreira.</summary>
public delegate void PromotionEventHandler(string characterId, CareerLevel newLevel);
