using Content.Client.CharacterInfo;
using Content.Shared._Oxyd.Economy;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Analyzers;
using Robust.Shared.Utility;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._Oxyd.Economy;

/// <summary>Adds a "Bank" panel to the stock character-info window (C action) showing
/// the mob's personal account number and balance, fed by the networked
/// <see cref="OxydPersonalAccountComponent"/> fields.</summary>
public sealed partial class ClientOxydBankInfoSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnGetCharacterInfoControls(ref CharacterInfoSystem.GetCharacterInfoControlsEvent ev)
    {
        if (!TryComp<OxydPersonalAccountComponent>(ev.Entity, out var personal))
            return;

        var box = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 4,
        };

        var accountPanel = new PanelContainer
        {
            HorizontalExpand = true,
            MinWidth = 100,
            PanelOverride = new StyleBoxFlat(backgroundColor: new Color(36, 37, 53)),
        };
        var accountRow = new BoxContainer { Orientation = LayoutOrientation.Horizontal, SeparationOverride = 6 };
        accountRow.AddChild(new Label { Text = "Account", MinWidth = 125 });
        accountRow.AddChild(new Label { Text = $"#{personal.AccountNumber}" });
        accountPanel.AddChild(accountRow);
        box.AddChild(accountPanel);

        var balancePanel = new PanelContainer
        {
            HorizontalExpand = true,
            MinWidth = 100,
            PanelOverride = new StyleBoxFlat(backgroundColor: new Color(36, 37, 53)),
        };
        var balanceRow = new BoxContainer { Orientation = LayoutOrientation.Horizontal, SeparationOverride = 6 };
        balanceRow.AddChild(new Label { Text = "Balance", MinWidth = 125 });
        balanceRow.AddChild(new Label { Text = $"{personal.Balance}cr" });
        balancePanel.AddChild(balanceRow);
        box.AddChild(balancePanel);

        ev.PanelControls.Add("Bank", box);
    }
}
