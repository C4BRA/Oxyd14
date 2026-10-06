using System.Linq;
using Content.Shared.CartridgeLoader;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared._Oxyd.Medical;

/// <summary>MIRC — Moebius Internal Reagent Catalogue (Eris chem_catalog.dm):
/// a PDA program listing Moebius medicine reagents and their synthesis recipes.</summary>
[RegisterComponent]
public sealed partial class OxydMircCartridgeComponent : Component;

public sealed partial class OxydMircCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<OxydMircCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
    }

    private void OnUiReady(EntityUid uid, OxydMircCartridgeComponent comp, CartridgeUiReadyEvent args)
    {
        _cartridgeLoader.UpdateCartridgeUiState(args.Loader, new OxydMircUiState { Entries = BuildEntries() });
    }

    private List<OxydMircEntry> BuildEntries()
    {
        var byProduct = new Dictionary<string, List<ReactionPrototype>>();
        foreach (var reaction in _prototypes.EnumeratePrototypes<ReactionPrototype>())
        {
            foreach (var product in reaction.Products.Keys)
            {
                if (!byProduct.TryGetValue(product.Id, out var list))
                    byProduct[product.Id] = list = new List<ReactionPrototype>();
                list.Add(reaction);
            }
        }

        var entries = new List<OxydMircEntry>();
        foreach (var reagent in _prototypes.EnumeratePrototypes<ReagentPrototype>())
        {
            if (reagent.Abstract || !reagent.ID.StartsWith("OxydMed"))
                continue;

            var entry = new OxydMircEntry
            {
                Id = reagent.ID,
                Name = reagent.LocalizedName,
                Description = reagent.LocalizedDescription,
            };
            if (byProduct.TryGetValue(reagent.ID, out var reactions))
            {
                foreach (var reaction in reactions.OrderBy(r => r.ID))
                {
                    var reactants = reaction.Reactants
                        .Select(kv => $"{_prototypes.Index(kv.Key).LocalizedName} ({kv.Value.Amount})");
                    var products = reaction.Products
                        .Select(kv => $"{_prototypes.Index(kv.Key).LocalizedName} ({kv.Value})");
                    entry.Recipes.Add($"{string.Join(" + ", reactants)} = {string.Join(", ", products)}");
                }
            }
            entries.Add(entry);
        }
        return entries.OrderBy(e => e.Name).ToList();
    }
}
