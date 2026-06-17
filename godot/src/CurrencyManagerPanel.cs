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

    // Campo de volatilidade do câmbio (v7) — injetado em código no formulário
    // (a cena não tem essa linha), mas persistente (não vive na lista que é
    // reconstruída a cada Refresh).
    private SpinBox _volatilityField = null!;

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

        InjectVolatilityField();
    }

    /// <summary>
    /// Cria a linha "Volatilidade do câmbio %/dia" (v7) e a insere no formulário,
    /// logo após a linha de inflação. Feito em código para não exigir edição da
    /// cena; persiste entre Refreshes por viver no Layout, não na CurrencyList.
    /// </summary>
    private void InjectVolatilityField()
    {
        var inflationRow = InflationField.GetParent();
        var layout = inflationRow.GetParent();

        var row = new HBoxContainer();
        row.AddChild(new Label
        {
            Text = "Volatilidade %/dia",
            CustomMinimumSize = new Vector2(140, 0),
        });
        _volatilityField = new SpinBox
        {
            MinValue = 0,
            MaxValue = (double)MarketRules.MaxExchangeRateVolatilityPercent,
            Step = 0.5,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _volatilityField.Connect(Godot.Range.SignalName.ValueChanged,
            Callable.From((double _) => UpdatePreview()));
        row.AddChild(_volatilityField);

        layout.AddChild(row);
        if (layout is Node layoutNode && inflationRow is Node inflationNode)
            layoutNode.MoveChild(row, inflationNode.GetIndex() + 1);
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

            string cambio = draft.ExchangeRateVolatilityPercent == 0m
                ? "câmbio fixo"
                : $"câmbio flutua ±{draft.ExchangeRateVolatilityPercent}%/dia";

            PreviewLabel.Text =
                $"Carro: {MarketRules.BaseCurrencySymbol}{emMoney.RoundedPrice} → {draft.Symbol}{convertido}\n" +
                $"Inflação projetada {draft.ProjectedAnnualInflationPercent}%/ano " +
                $"(ativa a partir do dia {draft.InflationActivationDay}; hoje é o dia {market.Calendar.CurrentDay}); " +
                $"{cambio}";
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

        BuildHistorySection(market);
        BuildEventsSection(market);

        foreach (var currency in market.Currencies.All)
        {
            var quote = market.Quote(MarketRules.SamplePreviewPriceMoney, currency.Id, productId: "carro");
            int custoReal = market.CostInMoney(MarketRules.SamplePreviewPriceMoney, currency.Id, productId: "carro");
            decimal taxaEfetiva = market.EffectiveRate(currency.Id);
            string cambio = currency.IsBase || currency.ExchangeRateVolatilityPercent == 0m
                ? $"taxa {currency.UnitsPerMoney}"
                : $"taxa {currency.UnitsPerMoney}→{taxaEfetiva:0.####} (±{currency.ExchangeRateVolatilityPercent}%/dia)";
            var row = new HBoxContainer();
            row.AddChild(new Label
            {
                Text = $"{currency.Name} ({currency.Symbol})  {cambio}  " +
                       $"inflação {currency.ProjectedAnnualInflationPercent}%/ano  carro: {quote}  " +
                       $"(custo real {MarketRules.BaseCurrencySymbol}{custoReal})",
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
    /// Renderiza a seção de histórico (v6): um gráfico do índice global ao longo
    /// do tempo e botões para avançar a simulação (que também dão ao gerador
    /// estocástico a chance de programar choques). Tudo em código.
    /// </summary>
    private void BuildHistorySection(CurrencyMarket market)
    {
        CurrencyList.AddChild(new Label { Text = "— Histórico de inflação —" });

        var chart = new MarketHistoryChart();
        CurrencyList.AddChild(chart);
        if (market.History is not null)
            chart.SetHistory(market.History.Samples);

        var controls = new HBoxContainer();
        AddAdvanceButton(controls, "Avançar 30 dias", 30);
        AddAdvanceButton(controls, "Avançar 1 ano", MarketRules.DaysPerYear);
        CurrencyList.AddChild(controls);
    }

    private void AddAdvanceButton(HBoxContainer parent, string label, int days)
    {
        var button = new Button { Text = label };
        button.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
        {
            Controller.AdvanceDays(days);
            RefreshList();
            UpdatePreview();
        }));
        parent.AddChild(button);
    }

    /// <summary>
    /// Renderiza a seção de eventos econômicos (v5) no topo da lista: botões de
    /// preset (recessão/boom/crise) que agendam um evento começando hoje, o
    /// fator de reajuste de renda vigente, e a lista de eventos ativos/futuros
    /// com botão de remover. Tudo construído em código — sem cena/[Export] novo.
    /// </summary>
    private void BuildEventsSection(CurrencyMarket market)
    {
        int today = market.Calendar.CurrentDay;

        CurrencyList.AddChild(new Label { Text = "— Eventos econômicos —" });

        var presets = new HBoxContainer();
        AddPresetButton(presets, "Recessão", () => EconomicEventLibrary.Recession(today));
        AddPresetButton(presets, "Boom", () => EconomicEventLibrary.Boom(today));
        AddPresetButton(presets, "Crise", () => EconomicEventLibrary.Crisis(today));
        CurrencyList.AddChild(presets);

        var fator = market.IncomeAdjustmentFactor();
        CurrencyList.AddChild(new Label
        {
            Text = $"Reajuste de renda hoje (dia {today}): ×{fator:0.000} " +
                   $"(índice global × eventos ativos)",
        });

        foreach (var ev in market.Events.All)
        {
            string estado = ev.IsActiveOn(today)
                ? $"ativo até o dia {ev.EndDayExclusive - 1}"
                : ev.HasEndedOn(today) ? "encerrado" : $"começa no dia {ev.StartDay}";

            var row = new HBoxContainer();
            row.AddChild(new Label
            {
                Text = $"{ev.Name}: inflação {ev.GlobalInflationDelta:+0.#;-0.#;0}%, " +
                       $"renda ×{ev.IncomeMultiplier} — {estado}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            });

            var remove = new Button { Text = "Remover" };
            var id = ev.Id;
            remove.Connect(BaseButton.SignalName.Pressed, Callable.From(() => RemoveEvent(id)));
            row.AddChild(remove);
            CurrencyList.AddChild(row);
        }

        CurrencyList.AddChild(new Label { Text = "— Moedas —" });
    }

    private void AddPresetButton(HBoxContainer parent, string label, Func<EconomicEvent> factory)
    {
        var button = new Button { Text = label };
        button.Connect(BaseButton.SignalName.Pressed, Callable.From(() => ScheduleEvent(factory)));
        parent.AddChild(button);
    }

    private void ScheduleEvent(Func<EconomicEvent> factory)
    {
        var market = Controller.Market;
        if (market is null)
            return;

        var ev = factory();
        // Reagendar o mesmo preset no mesmo dia colidiria de Id — substitui.
        market.Events.Remove(ev.Id);
        market.Events.Schedule(ev);
        Controller.Save();
        RefreshList();
        PreviewLabel.Text = $"Evento '{ev.Name}' agendado para o dia {ev.StartDay} " +
                            $"(dura {ev.DurationDays} dias).";
    }

    private void RemoveEvent(string id)
    {
        var market = Controller.Market;
        if (market is null)
            return;

        if (market.Events.Remove(id))
            Controller.Save();
        RefreshList();
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
        ExchangeRateVolatilityPercent = (decimal)(_volatilityField?.Value ?? 0.0),
        CreatedOnDay = market.Calendar.CurrentDay,
    };
}
