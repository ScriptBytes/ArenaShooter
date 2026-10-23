using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Between-wave shop. Static layout lives in Shop.tscn; item cards are built in code.
/// Mutates the RunState directly (gold, items) and signals Main to re-apply gear and stats.
/// Gear that replaces something refunds half the old item's price.
/// </summary>
public partial class Shop : CanvasLayer
{
    [Signal] public delegate void PurchasedEventHandler();
    [Signal] public delegate void ContinueRequestedEventHandler();

    [Export] public int OfferCount { get; set; } = 4;
    [Export] public int BaseRerollCost { get; set; } = 3;
    [Export] public int RerollCostIncrease { get; set; } = 2;
    [Export] public float PriceIncreasePerWave { get; set; } = 0.1f;

    public class Offer
    {
        public ItemData Item;
        public int Price;
        public bool Sold;

        /// <summary>One per slot the item can go into: two when both ring slots are full. Rebuilt on refresh.</summary>
        public List<Button> Buttons { get; } = new();
        public Button BuyButton => Buttons[0];

        public Label Replaces;
        public VBoxContainer ButtonBox;
    }

    private readonly List<Offer> _offers = new();

    /// <summary>Current offers; Autoplay reads these to decide what to press.</summary>
    public IReadOnlyList<Offer> Offers => _offers;
    public Button ContinueButton => _continue;
    private RunState _run;
    private IReadOnlyList<ItemData> _pool;
    private int _rerollCost;

    private Label _title;
    private Label _gold;
    private HBoxContainer _offerRow;
    private Label _stats;
    private VBoxContainer _equipment;
    private Button _reroll;
    private Button _continue;

    public override void _Ready()
    {
        _title = GetNode<Label>("%Title");
        _gold = GetNode<Label>("%Gold");
        _offerRow = GetNode<HBoxContainer>("%Offers");
        _stats = GetNode<Label>("%Stats");
        _equipment = GetNode<VBoxContainer>("%Equipment");
        _reroll = GetNode<Button>("%RerollButton");
        _continue = GetNode<Button>("%ContinueButton");
        GetNode<PanelContainer>("%StatsPanel").AddThemeStyleboxOverride("panel", UiStyles.Panel());
        var equipmentPanel = GetNode<PanelContainer>("%EquipmentPanel");
        equipmentPanel.AddThemeStyleboxOverride("panel", UiStyles.Panel());
        // Tooltips take their look from the theme of the control they're hovering.
        equipmentPanel.Theme = UiStyles.TooltipTheme();

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

        // Gear you're already wearing would only replace itself. Rings are the exception: you can wear two.
        var pool = _pool.Where(i => i.Slot == EquipSlot.Ring || !_run.Equipment.IsWorn(i)).ToList();
        float priceScale = 1f + PriceIncreasePerWave * (_run.Wave - 1);
        foreach (var item in PickRandom(pool, OfferCount))
        {
            var offer = new Offer { Item = item, Price = Mathf.RoundToInt(item.Price * priceScale) };
            _offers.Add(offer);
            _offerRow.AddChild(BuildCard(offer));
        }

        Refresh();
    }

    private Control BuildCard(Offer offer)
    {
        var card = new PanelContainer { CustomMinimumSize = new Vector2(170, 280) };
        card.AddThemeStyleboxOverride("panel", UiStyles.Panel());
        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 8);
        card.AddChild(content);

        content.AddChild(new ColorRect { Color = offer.Item.IconColor, CustomMinimumSize = new Vector2(0, 48) });

        content.AddChild(UiStyles.WrappedLabel(offer.Item.DisplayName, 20));

        var slot = UiStyles.WrappedLabel(offer.Item.SlotLabel, 14);
        slot.AddThemeColorOverride("font_color", UiStyles.Dim);
        content.AddChild(slot);

        var description = UiStyles.WrappedLabel(offer.Item.Describe());
        description.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        content.AddChild(description);

        offer.Replaces = UiStyles.WrappedLabel("", 14);
        offer.Replaces.AddThemeColorOverride("font_color", UiStyles.Dim);
        content.AddChild(offer.Replaces);

        offer.ButtonBox = new VBoxContainer();
        content.AddChild(offer.ButtonBox);

        return card;
    }

    private void Buy(Offer offer, int position)
    {
        if (offer.Sold || !_run.TrySpend(offer.Price))
            return;

        offer.Sold = true;
        var replaced = _run.AddItem(offer.Item, position);
        _run.Gold += replaced.Sum(i => i.SellValue);
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
            RefreshOffer(offer);

        _stats.Text = "Your stats\n\n" + string.Join("\n",
            System.Enum.GetValues<StatType>().Select(s => StatInfo.Format(s, _run.Stats.Get(s))));
        BuildEquipment();
    }

    // Buying one item can change what another replaces (e.g. the first ring fills the empty slot),
    // so the buttons are rebuilt every refresh.
    private void RefreshOffer(Offer offer)
    {
        foreach (var button in offer.Buttons)
        {
            offer.ButtonBox.RemoveChild(button);
            button.QueueFree();
        }
        offer.Buttons.Clear();

        var targets = offer.Item.IsGear && !offer.Sold ? _run.Equipment.TargetsFor(offer.Item) : new List<int> { -1 };
        offer.Replaces.Text = "";
        foreach (int position in targets)
        {
            var replaced = position >= 0 ? _run.Equipment.Displaced(offer.Item, position) : new List<ItemData>();
            string text = offer.Sold ? "Sold" : $"Buy ({offer.Price})";
            if (targets.Count > 1)
                text += $"\nover {string.Join(", ", replaced.Select(i => i.DisplayName))}";
            else if (replaced.Count > 0)
                offer.Replaces.Text = DescribeReplacement(offer.Item, replaced);

            var button = new Button
            {
                Text = text,
                CustomMinimumSize = new Vector2(0, 40),
                Disabled = offer.Sold || _run.Gold < offer.Price,
            };
            button.Pressed += () => Buy(offer, position);
            offer.ButtonBox.AddChild(button);
            offer.Buttons.Add(button);
        }

        if (targets.Count > 1)
            offer.Replaces.Text = $"Both {EquipSlotInfo.Label(offer.Item.Slot).ToLower()} slots full: pick one to replace (half its price back)";
    }

    private string DescribeReplacement(ItemData item, List<ItemData> replaced)
    {
        int refund = replaced.Sum(i => i.SellValue);
        string names = string.Join(" and ", replaced.Select(i => i.DisplayName));
        string text = refund > 0 ? $"Replaces {names} (+{refund} gold back)" : $"Replaces {names}";

        // An off-hand item pushing out a two-hander leaves the main hand to the starting weapon.
        var equipment = _run.Equipment;
        if (item.Slot != EquipSlot.MainHand && replaced.Contains(equipment.MainHand))
            text += $". {equipment.StartingWeapon.DisplayName} goes back in your main hand";
        return text;
    }

    // One row per slot and per trinket, so each can show the item's stats on hover.
    private void BuildEquipment()
    {
        foreach (var child in _equipment.GetChildren())
        {
            _equipment.RemoveChild(child);
            child.QueueFree();
        }

        var equipment = _run.Equipment;
        AddEquipmentRow("Equipment\n", null);
        for (int i = 0; i < Equipment.Layout.Length; i++)
        {
            var slot = Equipment.Layout[i];
            string name = equipment[i]?.DisplayName
                ?? (slot == EquipSlot.OffHand && equipment.IsTwoHanding ? "(two-handed)" : "-");
            AddEquipmentRow($"{EquipSlotInfo.Label(slot)}: {name}", equipment[i]);
        }

        AddEquipmentRow("\nTrinkets", null);
        if (_run.Trinkets.Count == 0)
            AddEquipmentRow("-", null);
        foreach (var group in _run.Trinkets.GroupBy(t => t))
            AddEquipmentRow(group.Count() > 1 ? $"{group.Key.DisplayName} x{group.Count()}" : group.Key.DisplayName, group.Key);
    }

    private void AddEquipmentRow(string text, ItemData item)
    {
        var row = new Label { Text = text };
        row.AddThemeFontSizeOverride("font_size", 15);
        if (item != null)
        {
            row.TooltipText = $"{item.DisplayName}\n{item.Describe()}";
            // Labels ignore the mouse by default, which would also stop the tooltip.
            row.MouseFilter = Control.MouseFilterEnum.Pass;
        }
        _equipment.AddChild(row);
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
