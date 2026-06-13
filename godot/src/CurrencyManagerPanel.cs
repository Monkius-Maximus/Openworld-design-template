using System;
using EconomySystem.Core.Market;
using Godot;

namespace BuildSystem;

/// <summary>
/// Painel de gerenciamento de moedas (v4). Permite criar moedas derivadas do
/// $Money (nome, símbolo, taxa de conversão, inflação anual projetada) com
/// preview em tempo real usando o produto-exemplo "carro"
/// (<see cref="MarketRules.SamplePreviewPriceMoney"/>), listar as moedas
/// existentes com cotação ao vivo e removê-las (exceto a base).
/// </summary>
public partial class CurrencyManagerPanel : PanelContainer
{
    [Export] public MarketController Controller { get; set; } = null!;
    [Export] public LineEdit NameField { get; set; } = null!;
    [Export] public LineEdit SymbolField { get; set; } = null!;
    [Export] public SpinBox RateField { get; set; } = null!;
    [Export] public SpinBox InflationField { get; set; } = null!;
    [Export] public Label PreviewLabel { get; set; } = null!;
    [Export] public Button AddButton { get; set; } = null!;
    [Export] public VBoxContainer CurrencyList { get; set; } = null!;

    public override void _Ready()
    {
        if (Controller is null) throw new InvalidOperationException($"{nameof(CurrencyManagerPanel)} requires {nameof(Controller)}.");
        if (NameField is null) throw new InvalidOperationException($"{nameof(CurrencyManagerPanel)} requires {nameof(NameField)}.");
        if (SymbolField is null) throw new InvalidOperationException($"{nameof(CurrencyManagerPanel)} requires {nameof(SymbolField)}.");
        if (RateField is null) throw new InvalidOperationException($"{nameof(CurrencyManagerPanel)} requires {nameof(RateField)}.");
        if (InflationField is null) throw new InvalidOperationException($"{nameof(CurrencyManagerPanel)} requires {nameof(InflationField)}.");
        if (PreviewLabel is null) throw new InvalidOperationException($"{nameof(CurrencyManagerPanel)} requires {nameof(PreviewLabel)}.");
        if (AddButton is null) throw new InvalidOperationException($"{nameof(CurrencyManagerPanel)} requires {nameof(AddButton)}.");
        if (CurrencyList is null) throw new InvalidOperationException($"{nameof(CurrencyManagerPanel)} requires {nameof(CurrencyList)}.");

        NameField.Connect(LineEdit.SignalName.TextChanged, Callable.From((string _) => UpdatePreview()));
        SymbolField.Connect(LineEdit.SignalName.TextChanged, Callable.From((string _) => UpdatePreview()));
        RateField.Connect(Godot.Range.SignalName.ValueChanged, Callable.From((double _) => UpdatePreview()));
        InflationField.Connect(Godot.Range.SignalName.ValueChanged, Callable.From((double _) => UpdatePreview()));
        AddButton.Connect(BaseButton.SignalName.Pressed, Callable.From(AddCurrency));
    }

    /// <summary>Reconstrói preview e lista. Chamado pelo controller após o load.</summary>
    public void Refresh()
    {
        UpdatePreview();
        RefreshList();
    }

    private void UpdatePreview()
    {
        var market = Controller.Market;
        if (market is null)
            return;

        try
        {
            var draft = BuildDraftCurrency(market);

            // O carro em $Money já com a deriva global/do produto de hoje;
            // a moeda nova ainda não tem índice próprio (inflação dormente).
            var emMoney = market.Quote(MarketRules.SamplePreviewPriceMoney,
                MarketRules.BaseCurrencyId, productId: "carro");
            var convertido = (int)Math.Round(
                emMoney.EffectiveMoneyPrice * draft.UnitsPerMoney,
                MidpointRounding.AwayFromZero);

            PreviewLabel.Text =
                $"Carro: {MarketRules.BaseCurrencySymbol}{emMoney.RoundedPrice} → {draft.Symbol}{convertido}\n" +
                $"Inflação projetada {draft.ProjectedAnnualInflationPercent}%/ano " +
                $"(ativa a partir do dia {draft.InflationActivationDay}; hoje é o dia {market.Calendar.CurrentDay})";
        }
        catch (ArgumentException e)
        {
            PreviewLabel.Text = $"Inválido: {e.Message}";
        }
    }

    private void AddCurrency()
    {
        var market = Controller.Market;
        if (market is null)
            return;

        try
        {
            var currency = BuildDraftCurrency(market);
            market.Currencies.Add(currency);
            Controller.Save();
            RefreshList();
            PreviewLabel.Text = $"{currency.Name} criada no dia {currency.CreatedOnDay} " +
                                $"(inflação ativa no dia {currency.InflationActivationDay}).";
        }
        catch (ArgumentException e)
        {
            PreviewLabel.Text = $"Inválido: {e.Message}";
        }
    }

    private void RemoveCurrency(string id)
    {
        var market = Controller.Market;
        if (market is null)
            return;

        try
        {
            if (market.RemoveCurrency(id))
                Controller.Save();
        }
        catch (InvalidOperationException e)
        {
            PreviewLabel.Text = e.Message;
        }
        RefreshList();
    }

    private void RefreshList()
    {
        var market = Controller.Market;
        if (market is null)
            return;

        foreach (var child in CurrencyList.GetChildren())
            child.QueueFree();

        foreach (var currency in market.Currencies.All)
        {
            var quote = market.Quote(MarketRules.SamplePreviewPriceMoney, currency.Id, productId: "carro");
            var row = new HBoxContainer();
            row.AddChild(new Label
            {
                Text = $"{currency.Name} ({currency.Symbol})  taxa {currency.UnitsPerMoney}  " +
                       $"inflação {currency.ProjectedAnnualInflationPercent}%/ano  carro: {quote}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            });

            var remove = new Button { Text = "Remover", Disabled = currency.IsBase };
            var id = currency.Id;
            remove.Connect(BaseButton.SignalName.Pressed, Callable.From(() => RemoveCurrency(id)));
            row.AddChild(remove);

            CurrencyList.AddChild(row);
        }
    }

    /// <summary>
    /// Monta uma Currency a partir dos campos atuais. O core valida fail-fast
    /// (nome/símbolo vazios, taxa/inflação fora dos limites) — o chamador
    /// captura ArgumentException e mostra a mensagem no preview.
    /// </summary>
    private Currency BuildDraftCurrency(CurrencyMarket market) => new()
    {
        Id = NameField.Text.Trim().TrimStart('$').Replace(" ", "-"),
        Name = NameField.Text.Trim(),
        Symbol = SymbolField.Text.Trim(),
        UnitsPerMoney = (decimal)RateField.Value,
        ProjectedAnnualInflationPercent = (decimal)InflationField.Value,
        CreatedOnDay = market.Calendar.CurrentDay,
    };
}
