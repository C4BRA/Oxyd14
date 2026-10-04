using Content.Shared._Oxyd.Medical;
using Robust.Client.UserInterface;

namespace Content.Client._Oxyd.Medical.UI;

/// <summary>Eris centrifuge / electrolyzer UI port.</summary>
public sealed class OxydChemProcessorBoundUserInterface : BoundUserInterface
{
    private OxydChemProcessorWindow? _window;

    public OxydChemProcessorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<OxydChemProcessorWindow>();
        _window.TargetSet += (id, target) =>
            SendMessage(new OxydChemProcessorSetTargetMessage { Reagent = id, TargetBeaker = target });
        _window.StartRequested += () => SendMessage(new OxydChemProcessorStartMessage());
        _window.EjectRequested += index => SendMessage(new OxydChemProcessorEjectMessage { BeakerIndex = index });
        _window.OnClose += Close;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is OxydChemProcessorState proc)
            _window?.UpdateState(proc);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _window?.Dispose();
            _window = null;
        }
        base.Dispose(disposing);
    }
}
