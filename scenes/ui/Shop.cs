using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Between-wave shop. Static layout lives in Shop.tscn; item cards are built in code.
/// Mutates the RunState directly (gold, items) and signals Main to re-apply gear and stats.
/// Gear that replaces something refunds half the old item's price. Below the random offers is a
/// potion shelf: every consumable, always in stock, buyable until the player carries its MaxCarry.
/// Each offer rolls a rarity (<see cref="RaritySettings"/>, odds rising with the wave; rerolls roll again),
/// which scales its stats and price and colors its card. Potions have no rarity.
/// </summary>
public partial class Shop : CanvasLayer
{
    [Signal] public delegate void PurchasedEventHandler();
    [Signal] public delegate void ContinueRequestedEventHandler();

    /// <summary>Gold went up or down (purchase, refund, reroll), so the HUD behind the shop can follow.</summary>
    [Signal] public delegate void GoldChangedEventHandler(int gold);

    /// <summary>New offers are on display (shop opened or rerolled).</summary>
    [Signal] public delegate void OffersRolledEventHandler();

    [Export] public int OfferCount { get; set; } = 4;
    [Export] public int BaseRerollCost { get; set; } = 3;
    [Export] public int RerollCostIncrease { get; set; } = 2;
    [Export] public float PriceIncreasePerWave { get; set; } = 0.1f;

    /// <summary>Rarity tuning and odds. Set by Main before the first Open.</summary>
    public RaritySettings Rarities { get; set; }

    /// <summary>Debug (Autoplay --rarity): every offer gets this rarity instead of a roll.</summary>
    public Rarity? ForcedRarity { get; set; }

    public class Offer
    {
        public ItemInstance Item;
        public int Price;
        public bool Sold;

        /// <summary>One per slot the item can go into: two when both ring slots are full. Rebuilt on refresh.</summary>
        public List<Button> Buttons { get; } = new();
        public Button BuyButton => Buttons[0];

        public Label Replaces;
        public VBoxContainer ButtonBox;
    }

    /// <summary>A potion on the shelf. Not rerolled; the price rises with the wave like the offers.</summary>
    public class PotionOffer
    {
        public ConsumableData Item;
        public int Price;
        public Button BuyButton;
        public Label Carried;
    }

    private readonly List<Offer> _offers = new();
    private readonly List<PotionOffer> _potionOffers = new();

    /// <summary>Current offers; Autoplay reads these to decide what to press.</summary>
    public IReadOnlyList<Offer> Offers => _offers;
    public IReadOnlyList<PotionOffer> PotionOffers => _potionOffers;
    public Button ContinueButton => _continue;
    public Button RerollButton => _reroll;
    public int RerollCost => _rerollCost;
    private RunState _run;
    private IReadOnlyList<ItemData> _pool;
    private int _rerollCost;

    private Label _title;
    private Label _gold;
    private HBoxContainer _odds;
    private HBoxContainer _offerRow;
    private Label _stats;
    private VBoxContainer _equipment;
    private HBoxContainer _potionShelf;
    private Button _reroll;
    private Button _continue;

    public override void _Ready()
    {
        ScreenFit.Attach(GetNode<Control>("%Layout"));
        _title = GetNode<Label>("%Title");
        _gold = GetNode<Label>("%Gold");
        _odds = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _odds.AddThemeConstantOverride("separation", 18);
        _gold.AddSibling(_odds);
        _offerRow = GetNode<HBoxContainer>("%Offers");
        _stats = GetNode<Label>("%Stats");
        _equipment = GetNode<VBoxContainer>("%Equipment");
        _potionShelf = GetNode<HBoxContainer>("%PotionShelf");
        _reroll = GetNode<Button>("%RerollButton");
        _continue = GetNode<Button>("%ContinueButton");
        GetNode<PanelContainer>("%StatsPanel").AddThemeStyleboxOverride("panel", UiStyles.Panel());
        var damagePanel = GetNode<PanelContainer>("%DamagePanel");
        damagePanel.AddThemeStyleboxOverride("panel", UiStyles.Panel());
        damagePanel.AddChild(new CombatMeters { Tabbed = true });
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

        BuildPotionShelf();
        BuildOdds();
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
        float priceScale = PriceScale;
        foreach (var item in PickRandom(pool, OfferCount))
        {
            var tier = ForcedRarity is { } forced ? Rarities.Tier(forced) : Rarities.Roll(_run.Wave);
            var instance = new ItemInstance(item, tier);
            var offer = new Offer { Item = instance, Price = Mathf.RoundToInt(instance.Price * priceScale) };
            _offers.Add(offer);
            _offerRow.AddChild(BuildCard(offer));
        }

        Refresh();
        EmitSignal(SignalName.OffersRolled);
    }

    private float PriceScale => 1f + PriceIncreasePerWave * (_run.Wave - 1);

    // "Common 82%  Uncommon 15%  Rare 3%" in each tier's color, so the odds (and the colors) are learnable.
    private void BuildOdds()
    {
        foreach (var child in _odds.GetChildren())
        {
            _odds.RemoveChild(child);
            child.QueueFree();
        }

        var chances = Rarities.Chances(_run.Wave);
        for (int i = 0; i < chances.Length; i++)
        {
            if (chances[i] <= 0f && ForcedRarity == null)
                continue;
            var tier = Rarities.Tiers[i];
            string text = ForcedRarity is { } forced
                ? (forced == tier.Rarity ? $"{tier.Label} (forced)" : null)
                : $"{tier.Label} {chances[i]:0.#}%";
            if (text == null)
                continue;
            var label = new Label { Text = text };
            label.AddThemeFontSizeOverride("font_size", 15);
            label.AddThemeColorOverride("font_color", tier.Color);
            _odds.AddChild(label);
        }
    }

    private void BuildPotionShelf()
    {
        foreach (var child in _potionShelf.GetChildren())
        {
            _potionShelf.RemoveChild(child);
            child.QueueFree();
        }
        _potionOffers.Clear();

        foreach (var item in _run.Potions.Catalog)
        {
            var offer = new PotionOffer { Item = item, Price = Mathf.RoundToInt(item.Price * PriceScale) };
            _potionOffers.Add(offer);
            _potionShelf.AddChild(BuildPotionCard(offer));
        }
    }

    // Compact horizontal card: swatch, name and effect, carried count, buy button.
    private Control BuildPotionCard(PotionOffer offer)
    {
        var card = new PanelContainer { CustomMinimumSize = new Vector2(250, 0), TooltipText = offer.Item.Describe() };
        var style = UiStyles.Panel();
        style.SetContentMarginAll(8);
        card.AddThemeStyleboxOverride("panel", style);
        card.Theme = UiStyles.TooltipTheme();

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        card.AddChild(row);

        row.AddChild(new ColorRect
        {
            Color = offer.Item.IconColor,
            CustomMinimumSize = new Vector2(14, 0),
            MouseFilter = Control.MouseFilterEnum.Pass,
        });

        var text = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        text.AddThemeConstantOverride("separation", 0);
        row.AddChild(text);
        var name = new Label { Text = offer.Item.DisplayName };
        name.AddThemeFontSizeOverride("font_size", 15);
        text.AddChild(name);
        var effect = new Label { Text = ShortEffect(offer.Item) };
        effect.AddThemeFontSizeOverride("font_size", 13);
        effect.AddThemeColorOverride("font_color", UiStyles.Dim);
        text.AddChild(effect);
        offer.Carried = new Label();
        offer.Carried.AddThemeFontSizeOverride("font_size", 13);
        offer.Carried.AddThemeColorOverride("font_color", UiStyles.Dim);
        text.AddChild(offer.Carried);

        offer.BuyButton = new Button { CustomMinimumSize = new Vector2(76, 0) };
        offer.BuyButton.Pressed += () => BuyPotion(offer);
        row.AddChild(offer.BuyButton);
        return card;
    }

    private static string ShortEffect(ConsumableData item) => item.HealthPotion
        ? "+1 health potion (Space)"
        : $"{string.Join(", ", item.Modifiers.Select(m => m.Describe()))}, {item.Duration:0}s";

    private void BuyPotion(PotionOffer offer)
    {
        if (!_run.Potions.CanCarryMore(offer.Item) || !_run.TrySpend(offer.Price))
            return;

        _run.AddPotion(offer.Item);
        EmitSignal(SignalName.Purchased);
        Refresh();
    }

    private void RefreshPotionOffer(PotionOffer offer)
    {
        var potions = _run.Potions;
        int carried = potions.Count(offer.Item);
        bool full = !potions.CanCarryMore(offer.Item);
        offer.Carried.Text = $"Carrying {carried}/{offer.Item.MaxCarry}";
        offer.BuyButton.Text = full ? "Full" : $"Buy ({offer.Price})";
        offer.BuyButton.Disabled = full || _run.Gold < offer.Price;
    }

    private Control BuildCard(Offer offer)
    {
        var item = offer.Item;
        var card = new PanelContainer { CustomMinimumSize = new Vector2(196, 260) };
        // Common gets a faint border so the colored ones stand out.
        card.AddThemeStyleboxOverride("panel", item.Rarity == Rarity.Common
            ? UiStyles.BorderedPanel(new Color(item.Color, 0.25f), 1)
            : UiStyles.BorderedPanel(item.Color, 3));
        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 8);
        card.AddChild(content);

        content.AddChild(new ColorRect { Color = item.Item.IconColor, CustomMinimumSize = new Vector2(0, 28) });

        var name = UiStyles.WrappedLabel(item.DisplayName, 20);
        name.AddThemeColorOverride("font_color", item.Color);
        content.AddChild(name);

        var slot = UiStyles.WrappedLabel(item.SlotLabel, 14);
        slot.AddThemeColorOverride("font_color", item.Rarity == Rarity.Common ? UiStyles.Dim : item.Color);
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
        foreach (var offer in _potionOffers)
            RefreshPotionOffer(offer);

        // Only stats you have: listing every stat at +0 no longer fits the screen (14 stats).
        var owned = GearViews.StatLines(_run.Stats, all: false);
        _stats.Text = "Your stats\n\n" + (owned.Count > 0 ? string.Join("\n", owned) : "No bonuses yet");
        GearViews.FillEquipment(_equipment, _run);
        EmitSignal(SignalName.GoldChanged, _run.Gold);
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

        var targets = offer.Item.IsGear && !offer.Sold ? _run.Equipment.TargetsFor(offer.Item.Item) : new List<int> { -1 };
        offer.Replaces.Text = "";
        foreach (int position in targets)
        {
            var replaced = position >= 0 ? _run.Equipment.Displaced(offer.Item.Item, position) : new List<ItemInstance>();
            string text = offer.Sold ? "Sold" : $"Buy ({offer.Price})";
            if (targets.Count > 1)
                text += $"\nover {string.Join(", ", replaced.Select(i => i.FullName))}";
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

    private string DescribeReplacement(ItemInstance item, List<ItemInstance> replaced)
    {
        int refund = replaced.Sum(i => i.SellValue);
        string names = string.Join(" and ", replaced.Select(i => i.FullName));
        string text = refund > 0 ? $"Replaces {names} (+{refund} gold back)" : $"Replaces {names}";

        // An off-hand item pushing out a two-hander leaves the main hand to the starting weapon.
        var equipment = _run.Equipment;
        if (item.Slot != EquipSlot.MainHand && replaced.Contains(equipment.MainHand))
            text += $". {equipment.StartingWeapon.DisplayName} goes back in your main hand";
        return text;
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
