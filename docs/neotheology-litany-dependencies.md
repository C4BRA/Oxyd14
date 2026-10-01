# NeoTheology litany dependencies

Status on PR #33 after the merge of AEV-Oxyd `master` (`97c050d91a`): all 60
catalog entries are enabled. The code implements the 19 dependency categories
below. This is **catalog coverage, not proof of gameplay parity**. See
[neotheology-litany-progress.md](neotheology-litany-progress.md) for the
behavior differences, test results, and live-round checks. The newer
[feature-gap pass](neotheology-feature-gap-pass.md) records committed changes
and remaining limits separately from this historical PR baseline.

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
| `Purity` | `Rejection` | Shared explicit/periodic foreign-implant, robotic-organ and embedded-projectile removal; resistance/Godblood exceptions; rejected implants cannot be reinserted. Natural organs and cruciform remain. |
| `ThreatClassification` | `RevealAdversaries` | Caster-centered live hostile-fauna scan at 14 m; visible landmines at 7 m. Source hidden 20% failure and separate 80% trap roll retained. |
| `Attachments` | `InstallUpgrade`, `UninstallUpgrade` | Installed cruciform upgrades have live effects; the procedures check the altar and target. |
| `SoulCloning` | `Reincarnation`, `Resurrection` | Normal grants install cloning and capture identity. Resurrection grows an unoccupied matching-DNA vessel; Commitment and Reincarnation restore the saved mind. Local rank-based clone damage exemption exists. |
| `PlantGrowth` | `AcceleratedGrowth` | Local plant growth multiplier. |
| `Addiction` | `WordsOfPurging` | Local addiction treatment and pain suppression; source Eris does not purge blood reagents in this rite. |
| `CoreModules` | `Asacris`, `Initiation` | A separate removable ascension-kit item supplies conversion; Initiation cannot conjure it. Asacris removes core-upgrade items while preserving the physical attachment. |
| `Pain` | `Atonement`, `Penance` | Local pain/stamina behavior replaces Eris hallucination loss. |
| `EyeEconomy` | `DivineIntervention`, `HolyGuidance` | Five-power offerings choose reward families; Guidance requires oddity/produce. Seal, global faithful rewards and regeneration Energy exist; threat/world/perk parity remains incomplete. |
| `Armaments` | `OrderArmaments` | Native finished products and design disks; first sale of each product increases capacity. Full source armor/disk catalog remains incomplete. |
| `ConstructionCatalog` | `DivineGuidance` | Local blueprint catalog and material choices. |
| `Construction` | `Manifestation`, `Uproot` | Parent-independent front-tile material scan; blueprint build times; single-consumption/refund guards. Native canisters, printers, solidifier, and holy-door variants are constructible; Eris multipart/liquid machinery is explicitly adapted. |
| `ForgeMaterials` | `MakeCruciform` | Local cruciform forge and material accounting. |
| `BiomatterMaterials` | `RepairDoor` | Local biomatter and door repair. |
| `Biogenerator` | `PowerBiogenerator` | Local powered NeoTheology machine. |
| `Bioreactor` | `BioreactorSolution`, `BioreactorChamber` | Native chamber checks plus single-processing dead-body conversion, retaining equipment and implants. Multipart/liquid biomass remains adapted. |
| `Ceremonies` | Eight multi-phrase ceremonies, three ordinary Crusader rites | Righteous life, native objective consumers, bounded-tile Sanctify and marked faction-item Crusade activation exist. Full source areas/department/world products remain incomplete. |
| `RemoteView` | `Scrying` | Followed marker, target-deletion cleanup, global follower targeting, and book/spoken private choices. Multiplayer eye/privacy checks remain. |
| `NtUplink` | `Knowledge`, `Bounty` | Source-price 13-product NT catalog on native weapons, active linked bearer/listing/account checks, local banked balance and revocation. Exact source weapons/economy remain adapted. |

Every litany with no listed packet still needs its effect handler, profile access,
and valid targets. The enabled catalog and handler coverage checks are in the
NeoTheology unit and integration tests. Follow the live-round checks in the
progress document before claiming that a litany matches Eris gameplay.
