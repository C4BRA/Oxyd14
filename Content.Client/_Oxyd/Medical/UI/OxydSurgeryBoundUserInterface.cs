using Content.Shared._Oxyd.Medical;
using Robust.Client.UserInterface;

namespace Content.Client._Oxyd.Medical.UI;

/// <summary>Surgery UI host on a nullspace proxy (Eris surgery_organ nano UI port).</summary>
public sealed class OxydSurgeryBoundUserInterface : BoundUserInterface
{
    private OxydSurgeryWindow? _window;

    public OxydSurgeryBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<OxydSurgeryWindow>();
        _window.StepSelected += OnStepSelected;
        _window.OnClose += Close;
    }

    private void OnStepSelected(NetEntity organ, OxydSurgeryStep step)
    {
        SendMessage(new OxydSurgerySelectStepMessage(organ, step));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is OxydSurgeryState surgery)
            _window?.UpdateState(surgery);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _window is not null)
        {
            _window.StepSelected -= OnStepSelected;
            _window.OnClose -= Close;
            _window.Dispose();
            _window = null;
        }
        base.Dispose(disposing);
    }
}
