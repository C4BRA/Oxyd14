using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared._Oxyd.Economy;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._Oxyd.Economy;

public sealed class OxydAtmBoundUserInterface : BoundUserInterface
{
    private OxydAtmWindow? _window;

    public OxydAtmBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindowCenteredLeft<OxydAtmWindow>();
        _window.OnLogin += (acct, pin) => SendMessage(new OxydAtmLoginMessage { AccountNumber = acct, Pin = pin });
        _window.OnLogout += () => SendMessage(new OxydAtmLogoutMessage());
        _window.OnWithdraw += amount => SendMessage(new OxydAtmWithdrawMessage { Amount = amount });
        _window.OnTransfer += (target, amount) =>
            SendMessage(new OxydAtmTransferMessage { TargetAccount = target, Amount = amount });
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is OxydAtmUiState s)
            _window?.UpdateState(s);
    }
}

public sealed class OxydAtmWindow : FancyWindow
{
    public event Action<int, int>? OnLogin;
    public event Action? OnLogout;
    public event Action<int>? OnWithdraw;
    public event Action<int, int>? OnTransfer;

    private readonly Label _status = new();
    private readonly Label _error = new() { Visible = false };
    private readonly LineEdit _account = new() { PlaceHolder = "Account #", HorizontalExpand = true };
    private readonly LineEdit _pin = new() { PlaceHolder = "PIN", MinWidth = 70 };
    private readonly LineEdit _withdraw = new() { PlaceHolder = "Withdraw amount", HorizontalExpand = true };
    private readonly LineEdit _transferAmount = new() { PlaceHolder = "Amount", MinWidth = 90 };
    private readonly OptionButton _target = new() { HorizontalExpand = true };
    private readonly List<int> _targetAccounts = new();
    private readonly BoxContainer _bankRows;
    private readonly BoxContainer _root;

    public OxydAtmWindow()
    {
        Title = "ATM";
        SetSize = new Vector2(420, 320);

        _root = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Margin = new Thickness(8),
            SeparationOverride = 4,
            VerticalExpand = true,
            HorizontalExpand = true,
        };

        _root.AddChild(_status);
        _root.AddChild(_error);

        var loginRow = new BoxContainer { Orientation = LayoutOrientation.Horizontal, SeparationOverride = 4 };
        loginRow.AddChild(_account);
        loginRow.AddChild(_pin);
        var loginBtn = new Button { Text = "Login" };
        loginBtn.OnPressed += _ =>
        {
            int.TryParse(_account.Text, out var a);
            int.TryParse(_pin.Text, out var p);
            OnLogin?.Invoke(a, p);
        };
        var logoutBtn = new Button { Text = "Logout" };
        logoutBtn.OnPressed += _ => OnLogout?.Invoke();
        loginRow.AddChild(loginBtn);
        loginRow.AddChild(logoutBtn);
        _root.AddChild(loginRow);

        _bankRows = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 4,
            Visible = false,
        };

        var withdrawRow = new BoxContainer { Orientation = LayoutOrientation.Horizontal, SeparationOverride = 4 };
        withdrawRow.AddChild(_withdraw);
        var withdrawBtn = new Button { Text = "Withdraw" };
        withdrawBtn.OnPressed += _ =>
        {
            if (!int.TryParse(_withdraw.Text, out var amount) || amount <= 0)
                return;
            OnWithdraw?.Invoke(amount);
        };
        withdrawRow.AddChild(withdrawBtn);
        _bankRows.AddChild(withdrawRow);

        var transferRow = new BoxContainer { Orientation = LayoutOrientation.Horizontal, SeparationOverride = 4 };
        _target.AddItem("Transfer target", 0);
        _target.TrySelectId(0);
        transferRow.AddChild(_target);
        transferRow.AddChild(_transferAmount);
        var transferBtn = new Button { Text = "Transfer" };
        transferBtn.OnPressed += _ =>
        {
            var sel = _target.SelectedId;
            if (sel <= 0 || sel > _targetAccounts.Count)
                return;
            if (!int.TryParse(_transferAmount.Text, out var amount) || amount <= 0)
                return;
            OnTransfer?.Invoke(_targetAccounts[sel - 1], amount);
        };
        transferRow.AddChild(transferBtn);
        _bankRows.AddChild(transferRow);

        _bankRows.AddChild(new Label { Text = "Insert a Credit stack on the machine to deposit." });
        _root.AddChild(_bankRows);
        ContentsContainer.AddChild(_root);
    }

    public void UpdateState(OxydAtmUiState state)
    {
        _error.Visible = state.Error != null;
        _error.Text = state.Error ?? string.Empty;
        _status.Text = state.LoggedIn
            ? $"#{state.AccountNumber} {state.OwnerName} — {state.Balance}cr"
            : "Not logged in";
        _bankRows.Visible = state.LoggedIn;

        var selected = _target.SelectedId is { } id && id >= 1 && id <= _targetAccounts.Count
            ? _targetAccounts[id - 1]
            : (int?)null;
        _target.Clear();
        _targetAccounts.Clear();
        _target.AddItem("Transfer target", 0);
        var restore = 0;
        for (var i = 0; i < state.Accounts.Count; i++)
        {
            var acct = state.Accounts[i];
            if (acct.Number == state.AccountNumber)
                continue; // can't transfer to yourself anyway
            _targetAccounts.Add(acct.Number);
            _target.AddItem($"#{acct.Number} {acct.OwnerName}", _targetAccounts.Count);
            if (acct.Number == selected)
                restore = _targetAccounts.Count;
        }
        _target.TrySelectId(restore);
    }
}
