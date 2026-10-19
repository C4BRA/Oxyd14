using Content.Client._Oxyd.Stylesheets;
using Content.Client.Stylesheets.Palette;

namespace Content.Client.Stylesheets.Stylesheets;

public partial class NanotrasenStylesheet
{
    public override ColorPalette PrimaryPalette => ErisPalettes.Blue; // Oxyd: CEV-Eris palette
    public override ColorPalette SecondaryPalette => ErisPalettes.Hull; // Oxyd: CEV-Eris palette
    public override ColorPalette PositivePalette => Palettes.Green;
    public override ColorPalette NegativePalette => Palettes.Red;
    public override ColorPalette HighlightPalette => ErisPalettes.Amber; // Oxyd: CEV-Eris palette
}
