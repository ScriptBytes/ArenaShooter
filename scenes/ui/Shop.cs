using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Between-wave shop. Static layout lives in Shop.tscn; item cards are built in code.
/// Mutates the RunState directly (gold, items) and signals Main to re-apply stats.
/// </summary>
public partial class Shop : CanvasLayer
{
    [Signal] public delegate void PurchasedEventHandler();
    [Signal] public delegate void ContinueRequestedEventHandler();

    [Export] public int OfferCount { get; set; } = 4;
    [Export] public int BaseRerollCost { get; set; } = 3;
    [Export] public int RerollCostIncrease { get; set; } = 2;
    [Export] public float PriceIncreasePerWave { get; set; } = 0.1f;

    private class Offer
    {
        public ItemData Item;
        public int Price;
        public Button BuyButton;
        public bool Sold;
    }

    private readonly List<Offer> _offers = new();
    private RunState _run;
    private IReadOnlyList<ItemData> _pool;
    private int _rerollCost;

    private Label _title;
    private Label _gold;
    private HBoxContainer _offerRow;
    private Label _stats;
    private Button _reroll;
    private Button _continue;

    public override void _Ready()
    {
        _title = GetNode<Label>("%Title");
        _gold = GetNode<Label>("%Gold");
        _offerRow = GetNode<HBoxContainer>("%Offers");
        _stats = GetNode<Label>("%Stats");
        _reroll = GetNode<Button>("%RerollButton");
        _continue = GetNode<Button>("%ContinueButton");
        GetNode<PanelContainer>("%StatsPanel").AddThemeStyleboxOverride("panel", UiStyles.Panel());

        _reroll.Pressed += Reroll;
        _continue.Pressed += () =>
        {
            Hide();
            EmitSignal(SignalName.ContinueRequested);
        };
        Hide();
    }

    public void Open(RunState run, IEnumerable<ItemData> pool)
    {
        _run = run;
        _pool = pool.ToList();
        _rerollCost = BaseRerollCost;

        _title.Text = $"Wave {run.Wave} cleared";
        _continue.Text = $"Start Wave {run.Wave + 1}";

        RollOffers();
        Show();
        _continue.GrabFocus();
    }

    private void RollOffers()
    {
        foreach (var child in _offerRow.GetChildren())
        {
            _offerRow.RemoveChild(child);
            child.QueueFree();
        }
        _offers.Clear();

        float priceScale = 1f + PriceIncreasePerWave * (_run.Wave - 1);
        foreach (var item in PickRandom(_pool, OfferCount))
        {
            var offer = new Offer { Item = item, Price = Mathf.RoundToInt(item.Price * priceScale) };
            _offers.Add(offer);
            _offerRow.AddChild(BuildCard(offer));
        }

        Refresh();
    }

    private Control BuildCard(Offer offer)
    {
        var card = new PanelContainer { CustomMinimumSize = new Vector2(190, 240) };
        card.AddThemeStyleboxOverride("panel", UiStyles.Panel());
        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 8);
        card.AddChild(content);

        content.AddChild(new ColorRect { Color = offer.Item.IconColor, CustomMinimumSize = new Vector2(0, 48) });

        content.AddChild(UiStyles.WrappedLabel(offer.Item.DisplayName, 20));

        var description = UiStyles.WrappedLabel(offer.Item.Describe());
        description.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        content.AddChild(description);

        offer.BuyButton = new Button { CustomMinimumSize = new Vector2(0, 40) };
        offer.BuyButton.Pressed += () => Buy(offer);
        content.AddChild(offer.BuyButton);

        return card;
    }

    private void Buy(Offer offer)
    {
        if (offer.Sold || !_run.TrySpend(offer.Price))
            return;

        offer.Sold = true;
        _run.AddItem(offer.Item);
        EmitSignal(SignalName.Purchased);
        Refresh();
    }

    private void Reroll()
    {
        if (!_run.TrySpend(_rerollCost))
            return;

        _rerollCost += RerollCostIncrease;
        RollOffers();
    }

    private void Refresh()
    {
        _gold.Text = _run.Bag > 0
            ? $"Gold: {_run.Gold}   ·   Bag: {_run.Bag} (doubles pickups next wave)"
            : $"Gold: {_run.Gold}";
        _reroll.Text = $"Reroll ({_rerollCost})";
        _reroll.Disabled = _run.Gold < _rerollCost;

        foreach (var offer in _offers)
        {
            offer.BuyButton.Text = offer.Sold ? "Sold" : $"Buy ({offer.Price})";
            offer.BuyButton.Disabled = offer.Sold || _run.Gold < offer.Price;
        }

        _stats.Text = "Your stats\n\n" + string.Join("\n",
            System.Enum.GetValues<StatType>().Select(s => StatInfo.Format(s, _run.Stats.Get(s))));
    }

    private static IEnumerable<ItemData> PickRandom(IReadOnlyList<ItemData> pool, int count)
    {
        // Partial Fisher–Yates shuffle: distinct items, no repeats within one roll.
        var items = pool.ToList();
        for (int i = 0; i < Mathf.Min(count, items.Count); i++)
        {
            int j = GD.RandRange(i, items.Count - 1);
            (items[i], items[j]) = (items[j], items[i]);
            yield return items[i];
        }
    }
}
