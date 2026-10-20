using System.Globalization;
using Content.Shared.Dataset;
using Content.Shared.Materials;
using Content.Shared.Random.Helpers;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Oxyd.Economy;

/// <summary>
/// Eris artwork naming + story generation (code/__HELPERS/artwork_names.dm,
/// code/game/objects/items/artwork_statue.dm make_art_review). Consumes the
/// artist_strings datasets in Resources/Prototypes/_Oxyd/Economy/artwork_datasets.yml.
/// </summary>
public sealed partial class OxydArtistBenchSystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IPlayerManager _playerMan = default!;



    private static readonly ProtoId<LocalizedDatasetPrototype> WeaponFirst = "OxydArtWeaponFirstNames";
    private static readonly ProtoId<LocalizedDatasetPrototype> WeaponSecond = "OxydArtWeaponSecondNames";
    private static readonly ProtoId<LocalizedDatasetPrototype> CreaturesPlural = "OxydArtCreaturesPlural";
    private static readonly ProtoId<LocalizedDatasetPrototype> Locations = "OxydArtLocations";
    private static readonly ProtoId<LocalizedDatasetPrototype> LocationsDescriptors = "OxydArtLocationsDescriptors";
    private static readonly ProtoId<LocalizedDatasetPrototype> LocationsSpecifics = "OxydArtLocationsSpecifics";
    private static readonly ProtoId<LocalizedDatasetPrototype> SculptingMethod = "OxydArtSculptingMethod";
    private static readonly ProtoId<LocalizedDatasetPrototype> SculptingMethodDescriptors = "OxydArtSculptingMethodDescriptors";
    private static readonly ProtoId<LocalizedDatasetPrototype> ArtStyles = "OxydArtStyles";
    private static readonly ProtoId<LocalizedDatasetPrototype> ArtStyleDescriptors = "OxydArtStyleDescriptors";
    private static readonly ProtoId<LocalizedDatasetPrototype> ArtTypes = "OxydArtTypes";
    private static readonly ProtoId<LocalizedDatasetPrototype> ArtTypesStatue = "OxydArtTypesStatue";
    private static readonly ProtoId<LocalizedDatasetPrototype> TypeActionsStatue = "OxydArtTypeActionsStatue";
    private static readonly ProtoId<LocalizedDatasetPrototype> TypeOfsStatue = "OxydArtTypeOfsStatue";
    private static readonly ProtoId<LocalizedDatasetPrototype> ObjectSubjects = "OxydArtObjectSubjects";
    private static readonly ProtoId<LocalizedDatasetPrototype> SubjectActions = "OxydArtSubjectActions";
    private static readonly ProtoId<LocalizedDatasetPrototype> ObjectActions = "OxydArtObjectActions";
    private static readonly ProtoId<LocalizedDatasetPrototype> ObjectSubjectActions = "OxydArtObjectSubjectActions";
    private static readonly ProtoId<LocalizedDatasetPrototype> ObjectSubjectActionsPresent = "OxydArtObjectSubjectActionsPresent";
    private static readonly ProtoId<LocalizedDatasetPrototype> ObjectSubjectStates = "OxydArtObjectSubjectStates";
    private static readonly ProtoId<LocalizedDatasetPrototype> ArtThemes = "OxydArtThemes";

    private string Pick(ProtoId<LocalizedDatasetPrototype> id) => _random.Pick(_proto.Index(id));

    private string Pick(params string[] options) => options[_random.Next(options.Length)];

    private string CapFirst(string s) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s);

    /// <summary>get_weapon_name(capitalize=TRUE) — oddity/toolmod/gunpart/armorpart names.</summary>
    private string GetWeaponArtworkName() => $"{CapFirst(Pick(WeaponFirst))} {CapFirst(Pick(WeaponSecond))}";

    /// <summary>get_artwork_crew_name — a random player character's name (first, last, or full).
    /// Eris picks a living human player's name; the AttachedEntity of a session is that mob.</summary>
    private string GetCrewName()
    {
        var names = new List<string>();
        foreach (var session in _playerMan.Sessions)
        {
            if (session.AttachedEntity is { } ent)
                names.Add(MetaData(ent).EntityName);
        }
        if (names.Count == 0)
            return "Someone";
        var real = names[_random.Next(names.Count)].Trim();
        var parts = real.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return _random.Next(3) switch
        {
            0 when parts.Length > 1 => parts[0],
            1 when parts.Length > 1 => parts[^1],
            _ => real,
        };
    }

    /// <summary>get_artwork_name — statue artwork names.</summary>
    private string GetStatueArtworkName()
    {
        return _random.Next(3) switch
        {
            // "the {adjective} secret of {crew}"
            0 => $"the {Pick("peculiar", "gray", "blue", "great", "small", "odd", "astounding")} secret of {GetCrewName()}",
            // "the {crew}s {trip|journey} to the {location}"
            1 => $"the {GetCrewName()}'s {Pick("trip", "journey")} to the {Pick(Locations)}",
            // "the {noun} of {crew}"
            _ => $"the {Pick("heart", "soul", "honor", "beauty", "feet", "true face", "true form")} of {GetCrewName()}",
        };
    }

    /// <summary>BYOND \a — indefinite article ("a"/"an") for a word.</summary>
    private static string A(string word) =>
        word.Length > 0 && "aeiou".Contains(char.ToLowerInvariant(word[0])) ? $"an {word}" : $"a {word}";

    /// <summary>BYOND \The — capitalized "The".</summary>
    private static string The(string word) => $"The {word}";

    /// <summary>Eris get_artwork_description — the multi-sentence generated story.</summary>
    private string GetArtworkDescription()
    {
        // Eris pick(objects, crew name) — a depicted object can just be a crew member.
        string PickObject() => _random.Next(5) == 0 ? GetCrewName() : Pick(ObjectSubjects);
        var objectName = PickObject();
        var subjectName = PickObject();
        var subjectAction = Pick(SubjectActions);
        var objectAction = Pick(ObjectActions);
        var objectSubjectAction = Pick(ObjectSubjectActions);
        var objectSubjectActionPresent = Pick(ObjectSubjectActionsPresent);
        var objectSubjectState = Pick(ObjectSubjectStates);
        var location = Pick(Locations);
        var locationDescriptor = Pick(LocationsDescriptors);
        var locationSpecific = Pick(LocationsSpecifics);
        var styleDescriptor = Pick(ArtStyleDescriptors);
        var theme = Pick(ArtThemes);
        var type = Pick(ArtTypesStatue);

        string descriptionLocation = "";
        if (objectName != location && subjectName != location)
            descriptionLocation = $"{Pick("This", "The")} {type} {locationDescriptor} {locationSpecific} {A(location)}.";

        string descriptionEventSubjectToObject =
            subjectAction.Contains("[object]")
                ? $"{The(subjectName)} {subjectAction.Replace("[object]", $"a {objectName}")}."
                : $"{The(subjectName)} {subjectAction} {A(objectName)}.";

        var descriptionEventObjectToSubject = _random.Prob(0.5f)
            ? $"{The(objectName)} {objectAction} by {A(subjectName)}."
            : $"{The(objectName)} {objectAction} while {A(subjectName)} {objectSubjectActionPresent} {Pick("in the background", "next to them")}.";

        var descriptionEventObjectSubject =
            $"{The(subjectName)} {objectSubjectAction} {Pick("on top of", "with")} {A(objectName)}.";

        var descriptionEventObject = $"{The(objectName)} {objectAction}.";

        var descriptionEvent = _random.Next(5) switch
        {
            0 => descriptionEventSubjectToObject,
            1 => descriptionEventObjectToSubject,
            2 => descriptionEventObjectSubject,
            3 => descriptionEventObject,
            _ => $"{The(subjectName)}.{The(objectName)}.",
        };

        if (!descriptionEvent.Contains(subjectName))
        {
            if (_random.Prob(0.5f))
                descriptionEvent += $" The {objectName} is {objectSubjectState}.";
        }
        else if (!descriptionEvent.Contains(objectName))
        {
            if (_random.Prob(0.5f))
                descriptionEvent += $" The {subjectName} is {objectSubjectState}.";
        }
        else if (_random.Prob(0.5f))
        {
            descriptionEvent += $" {The(_random.Prob(0.5f) ? subjectName : objectName)} is {objectSubjectState}.";
        }

        var descriptionStyle =
            $"{Pick("If you squint your eyes,", "Looking closely enough,")} the {styleDescriptor} " +
            $"{Pick("composition", "layout")} of {Pick("this", "the")} {type} " +
            $"{Pick("reveals", "seems to portray", "seems to hide")} the " +
            $"{Pick("outline of", "shape of", "parts of")} {A(objectName)}.";

        var descriptionTheme =
            $"{Pick("This", "The")} {type} {Pick("seems to", "appears to")} " +
            $"{Pick("portray", "suggest", "be entirely devoid of", "be utterly absent of")} {theme}.";

        return $"{descriptionEvent} {descriptionLocation} {Pick("", descriptionStyle)} {Pick("", descriptionTheme)}";
    }

    /// <summary>Eris get_artwork_review / object get_sanity_desc — quality verdict roll.</summary>
    private string GetQualityReview()
    {
        var value = Math.Clamp(
            (_random.Next(1, 5) - _random.Next(1, 5)) - (_random.Next(1, 5) - _random.Next(1, 5)) + _random.Next(0, 2),
            -6, 6);
        return value switch
        {
            >= 5 => " How awful. It's like a car crash — hard to look away.",
            >= 1 => " This artwork is quite unpleasant to look at.",
            0 => " A very mediocre piece of art.",
            >= -4 => " A beautiful piece of artwork.",
            _ => " A stunning artwork. Looking at it fills you with awe.",
        };
    }

    /// <summary>description_artwork_statue — the statue's opening line (verb or stationary form).</summary>
    private string GetStatueReviewLine()
    {
        var type = Pick(ArtTypesStatue);
        var of = Pick(TypeOfsStatue);
        return _random.Prob(0.5f)
            ? $"{Pick("This", "The")} {type} {Pick(TypeActionsStatue)} {(_random.Prob(0.5f) ? "" : of)}".TrimEnd()
            : $"{Pick(SculptingMethodDescriptors)} {Pick("this", "the")} {type} is {of}";
    }

    /// <summary>artwork_statue.make_art_review — full generated story + quality line.</summary>
    private string MakeStatueReview() =>
        $"{GetStatueReviewLine()} {GetArtworkDescription()}{GetQualityReview()}";

    /// <summary>Generic make_art_review for non-statue kinds:
    /// "A {style} {type} {method} {material}. {Inspires|Infuses} {emotion} in those who look at it."</summary>
    private string MakeGenericReview(ProtoId<MaterialPrototype> materialId)
    {
        var material = _proto.TryIndex(materialId, out MaterialPrototype? m) ? m.Name : "unknown material";
        var emotion = Pick("fear", "joy", "laughter", "sadness", "respect", "terror", "vigor", "encouragement");
        return $"A {Pick(ArtStyles)} {Pick(ArtTypes)} {Pick(SculptingMethod)} {material}. " +
               $"{Pick("Inspires", "Infuses")} {emotion} in those who look at it.";
    }
}
