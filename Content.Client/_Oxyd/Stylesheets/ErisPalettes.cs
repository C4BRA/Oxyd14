using Content.Client.Stylesheets.Palette;

namespace Content.Client._Oxyd.Stylesheets;

/// <summary>
///     CEV-Eris NanoUI colours (nano/css/shared.css and the ErisStyle HUD), used by the default stylesheet.
/// </summary>
public static class ErisPalettes
{
    // Eris NanoUI buttons and links.
    public static readonly ColorPalette Blue = ColorPalette.FromHexBase("#40628a", lightnessShift: 0.05f, chromaShift: 0.0045f);
    // Eris hull grey, used for panels and secondary controls.
    public static readonly ColorPalette Hull = ColorPalette.FromHexBase("#4d535e");
    // Eris NanoUI heading amber.
    public static readonly ColorPalette Amber = ColorPalette.FromHexBase("#c9a46b");
}
