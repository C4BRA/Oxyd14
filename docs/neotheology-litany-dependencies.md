# NeoTheology litany dependencies

Status on PR #33 after the merge of AEV-Oxyd `master` (`97c050d91a`): all 60
catalog entries are enabled. The code implements the 19 dependency categories
below. This is **catalog coverage, not proof of gameplay parity**. See
[neotheology-litany-progress.md](neotheology-litany-progress.md) for the
behavior differences, test results, and live-round checks.

## Casting foundation

`CruciformSystem` owns bearer profiles, modules, and holiness. `LitanySystem`
handles speech, cast validation, cooldowns, ceremonies, and book requests.
`LitanyEffectSystem` and its server handlers apply effects. The book shows
server-authored choices. These systems require an installed cruciform and the
correct rank; an enabled catalog entry alone does not grant a player access.

The six profiles are in `Resources/Prototypes/_Oxyd/NeoTheology/profiles.yml`.
`rules.yml` maps only the existing Chaplain job to `OxydNtPreacher`.
Six administrator-spawned development ghost-role markers in `test_roles.yml`
provide the matching cruciform and Bible when their mind takes the role.
The Chaplain does not automatically receive `OxydNtBible`. These markers and
six profiles do not add round-start Church jobs, maps, or job slots.
Profiles declare starting modules; installed modules, not profile labels,
authorize rites. See [the review-fix checklist](neotheology-litany-review-fixes.md)
for current validation rather than treating historical counts as a fresh result.

## Dependency packets

| Dependency category | Litany or capability | Current implementation and limit |
| --- | --- | --- |
| `Purity` | `Rejection` | Local implant and robotic-organ scan; natural organs and the cruciform remain. |
| `ThreatClassification` | `RevealAdversaries` | Caster-centered live hostile-fauna scan at 14 m; visible landmines at 7 m. Source hidden 20% failure and separate 80% trap roll retained. |
| `Attachments` | `InstallUpgrade`, `UninstallUpgrade` | Installed cruciform upgrades have live effects; the procedures check the altar and target. |
| `SoulCloning` | `Reincarnation`, `Resurrection` | Normal grants install cloning and capture identity. Resurrection grows an unoccupied matching-DNA vessel; Commitment and Reincarnation restore the saved mind. Local rank-based clone damage exemption exists. |
| `PlantGrowth` | `AcceleratedGrowth` | Local plant growth multiplier. |
| `Addiction` | `WordsOfPurging` | Local dependence and recovery; Eris reagent removal does not occur. |
| `CoreModules` | `Asacris`, `Initiation` | A separate removable ascension-kit item supplies conversion; Initiation cannot conjure it. Asacris removes core-upgrade items while preserving the physical attachment. |
| `Pain` | `Atonement`, `Penance` | Local pain/stamina behavior replaces Eris hallucination loss. |
| `EyeEconomy` | `DivineIntervention`, `HolyGuidance` | Local Eye power and observation; oddity and faithless behavior remain incomplete. |
| `Armaments` | `OrderArmaments` | Local armaments printer and design-disk flow. |
| `ConstructionCatalog` | `DivineGuidance` | Local blueprint catalog and material choices. |
| `Construction` | `Manifestation`, `Uproot` | Parent-independent front-tile material scan; blueprint build times; single-consumption/refund guards. Native canisters, printers, solidifier, and holy-door variants are constructible; Eris multipart/liquid machinery is explicitly adapted. |
| `ForgeMaterials` | `MakeCruciform` | Local cruciform forge and material accounting. |
| `BiomatterMaterials` | `RepairDoor` | Local biomatter and door repair. |
| `Biogenerator` | `PowerBiogenerator` | Local powered NeoTheology machine. |
| `Bioreactor` | `BioreactorSolution`, `BioreactorChamber` | Local bioreactor solution and chamber checks. |
| `Ceremonies` | Eight multi-phrase ceremonies, three ordinary Crusader rites | Successful ceremonies feed righteous life. Leaders persist until death/implant loss; church-objective signaling and full source world effects remain incomplete. |
| `RemoteView` | `Scrying` | Bounded session whose marker follows the target; target deletion ends the session. Book target choice and speech fallback remain. |
| `NtUplink` | `Knowledge`, `Bounty` | Cruciform store with local banked balance; Eris equipment is not present. |

Every litany with no listed packet still needs its effect handler, profile access,
and valid targets. The enabled catalog and handler coverage checks are in the
NeoTheology unit and integration tests. Follow the live-round checks in the
progress document before claiming that a litany matches Eris gameplay.
