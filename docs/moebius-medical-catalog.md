# Moebius medical port catalog

Inventory of CEV-Eris medical (Moebius department) gameplay, items, machines, and
UI, mapped to this fork. Source of truth: `C4BRA/CEV-Eris` (BYOND `.dm`). Paths
below are Eris paths unless prefixed `Oxyd14:`.

Status legend: **ported** / **native** (already in the SS14 base, thin Eris skin
only) / **planned** (in this branch) / **deferred** (ledgered, not in this PR).

## Department and roles — `code/game/jobs/job/medical.dm`, `code/datums/outfits/jobs/medical.dm`

| Eris role | Port |
| --- | --- |
| Moebius Biolab Officer (head) | planned |
| Moebius Doctor | planned |
| Moebius Chemist | planned |
| Moebius Paramedic | planned |
| Moebius Bio-Engineer | planned |
| Department `DEPARTMENT_MEDICAL`, accesses (`access_moebius`, `access_medical_equip`, `access_morgue`, `access_surgery`, `access_chemistry`, `access_paramedic`, `access_virology`, `access_genetics`, `access_cmo`, `access_change_medbay`, `access_psychiatrist`) | planned — mapped to native access levels where equivalent; Moebius-specific levels added under `_Oxyd` |
| Perk `selfmedicated`, stat modifiers (BIO etc.) | planned via existing `_Oxyd` skills/perks if present; ledgered otherwise |
| Chrysalis Pod / genetics research (bio-engineer) | deferred |
| Organ fabricator / visceral research (bio-engineer) | deferred |

## Health model — `code/modules/organs/`

| Eris feature | Port |
| --- | --- |
| External organs with wound tiers (cut/bruise/burn), open wounds, bleeding | planned — `OxydWounds` on body parts, bleeding to bloodstream |
| Broken bones (`status & ORGAN_BROKEN`), splints, bone surgery | planned |
| Internal organs with damage thresholds, failure, decay | partially — organ damage deferred; Eris robotic organs exist under NeoTheology |
| `pain.dm` | native — `_Oxyd/Medical/PainSystem` already ported |
| `blood.dm` (types, volume, IV transfer) | partially — native bloodstream; Eris IV stand planned |
| `body_modifications.dm` (cosmetic mods) | deferred |
| Germ/infection system | deferred — Eris keeps most infection in wounds; low value |

## Surgery — `code/modules/surgery/`

Step tree (`surgery.dm`, `organic.dm`, `organic_damage.dm`, `internal.dm`,
`robotic.dm`, `robotic_damage.dm`, `mods.dm`): `cut_open` (+laser),
`retract_skin`, `cauterize`, `fix_bleeding`, `close_wounds`, `mend_bone`,
`break_bone`, `replace_bone`, `remove_shrapnel`, `remove_item` (cavity implant),
`attach_organ`, `detach_organ`, `amputate`, robotic `open`/`fix_brute`/
`fix_burn`/`close`, hardsuit removal, assisted steps, `autodoc`.
UI: `surgery_ui.dm` (per-limb operation picker) — planned as bound UI.

| Feature | Port |
| --- | --- |
| Interactive per-body-part surgery, tool qualities, incision states | planned — `OxydSurgery` system + `OxydSurgeryWindow` |
| Organic steps (incision → retract → operate → close) | planned |
| Bone steps (mend/break/replace), organ attach/detach, cavity items, shrapnel | planned |
| Amputation / limb reattach | planned |
| Robotic steps | planned |
| Autodoc machine (auto-surgery) | planned |
| Slime surgery, MMI steps | deferred |

## Reagents — `code/modules/reagents/`

- `medicine.dm`: 112 definitions — Eris list incl. unique chems
  (ossisine, quickclot, kyphotorin, noexcutite, polystem, purger, addictol,
  aminazine, haloperidol, vomitol, suppressital, methylphenidate, citalopram,
  paroxetine, rezadone, ryetalyn, kognim, peridaxon, alkysine, imidazoline…).
  Port as `OxydMed*` reagents where no native equivalent exists; map to native
  IDs where identical (inaprovaline, bicaridine, kelotane, dermaline, dylovene,
  dexalin(+p), tricordrazine, cryoxadone, clonexadone, spaceacillin,
  synaptizine, hyronalin, arithrazine, leporazine, ethylredoxrazine, tramadol,
  oxycodone, paracetamol→native? none → port).
- NSA (`nerve_system_accumulations`) overdose limit — planned component/system.
- `stims.dm` (57), `nanites.dm` (49), `drugs.dm` (30) — planned; stims hook the
  existing `_Oxyd/Medical` AddictionSystem.
- `metabolism.dm` liver/kidney/kidney NSA handling — mapped to native
  `Bloodstream` metabolism + NSA system.
- Recipes (`recipes.dm` medical section) — planned for ported chems.

## Machines — `code/game/machinery/`, `code/modules/reagents/machinery/`

| Eris machine | Port |
| --- | --- |
| `adv_med.dm` full body scanner (limb/organ report) | planned — Eris-style scanner + report UI (native `MedicalScanner` exists, no organ report) |
| `Sleeper.dm` (chem injection, dialysis) | planned |
| `Operating.dm` operating table | native exists; Eris surgery hooks into it |
| `autodoc.dm` (+excelsior variant) | planned |
| `telemonitor.dm` crew monitor | native crew monitoring exists; Eris console skin planned if cheap |
| `cryo.dm`/`cryopod.dm` | native exists |
| `chem_dispenser.dm` | native `ChemDispenser` exists; Eris variant skin only if cheap |
| `chem_master.dm` | native exists |
| `chem_heater.dm` | native `SolutionHeater` exists |
| `grinder.dm` | native exists |
| `centrifuge.dm` (separates reagents / blood) | planned |
| `electrolyzer.dm` | planned |
| `atomic_distillery.dm` | planned |
| `reagent_dispenser.dm` wall dispensers | native exists |
| Medical stand / IV drip (`structures/medical_stand.dm`) | planned |
| `morgue.dm` | native exists |
| `autolathe_disk_cloner.dm` | deferred — generic autolathe feature, not medical |
| `medbot.dm` | deferred |

## Items

| Eris item group | Port |
| --- | --- |
| Medical stacks `stacks/medical.dm`: gauze, ointment, splints, non-sterile bandage, advanced trauma/burn kits, NT packs | planned (native Brutepack/Ointment/Gauze exist — Eris versions additive) |
| Autoinjectors `hypospray.dm` (14 flavors) + Eris hypospray | planned |
| Medkits `storage/firstaid.dm` (7 kits + NT medkit + surgery kit + 15 pill bottles + portable/organ freezer) | planned |
| Surgical tools `tools/*.dm`: scalpel, retractor, hemostat, cautery, bonesetter, surgical drill, circular saw, FixOVein, laser scalpel, bone gel | planned |
| Body bag / stasis (cryo) bag `bodybag.dm` | planned (stasis slows damage — check bodybag/expanded) |
| Health analyzer `devices/scanners/health.dm` (limb/organ/wound readout) | planned — Eris-style analyzer or native reskin |
| Syringe gun `projectiles/guns/launcher/syringe_gun.dm` | planned |
| Medical belt `storage/belt.dm`, pill bottles, dropper | planned (dropper/pills native) |
| Syringes, hypospray base | native exists — reuse |
| Medical HUD / Health Scanner HUD | native — `erisPorted/eye/Health_Scanner_HUD.yml` exists |
| Clothing: uniforms, labcoats (cmo/chemist/bioengineer/scientist/virologist), scrubs, surgical cap+apron, paramedic armor+helmet, medical cap, latex/nitrile gloves, sterile mask, medical voidsuit, chemistry backpack | native — already in `erisPorted` |
| Medical kiosk program, medical records, suit sensors PDA program (`modular_computers/…/medical`) | deferred — no modular-computer analog |
| Organ freezer decay behavior | deferred — no organ decay |
| Moebius PDA cartridges/IDs | planned as ID cards w/ Moebius accesses |
| `boubou` (monkey), virology gear | deferred — no virology |

## UI/UX

- Surgery limb/step picker (planned, `surgery_ui.dm`)
- Body scanner report (planned, `scanner.tmpl`)
- Sleeper UI (planned, `sleeper.tmpl`)
- Autodoc UI (planned)
- Chem dispenser/master (native UIs suffice; Eris `chem_dispenser.tmpl`, `ChemMaster.js` differ in data only)
- Health analyzer wound readout (planned; extends native)
- Crew monitor (`telemonitor`) — native UI exists

## Divergences (seeded; final register lives in moebius-medical-progress.md)

- SS14 damage model (Brute/Burn/Toxin/Airloss) replaces Eris per-organ wound
  pools; wounds layer on top for bleeding/fracture/incision state.
- SS14 reagent metabolism replaces `metabolism.dm`; NSA layered as a component.
- No virology, no genetics (chrysalis), no organ decay, no MMI/slime surgery.
- Eris cloning belongs to NeoTheology (already ported in PR #33).

## Port divergences & engine-convention fixes (final pass)

### Moebius protos
- `OxydErisMedkitAdvanced` drops `OxydErisHealthScanner` from its fill — native `Medkit` storage (`maxItemSize: Small`, 2x4 grid) cannot hold it. The scanner still exists as a standalone item (`OxydErisHealthScanner`).
- `OxydErisSurgeryKit` carries its own `Storage` override (`maxItemSize: Normal`, 8x3 grid) so the Normal-sized surgical saw fits beside the Small tools.
- Stack protos (`OxydErisBrutepack`, `OxydErisOintment`, `OxydErisTraumakit`, `OxydErisBurnkit`, `OxydErisSplint`, `OxydErisNanopaste`) gained `spawn:` pointing at new count-1 `<id>Single` entity protos, matching the native `StackPrototype.spawn` convention.
- Entity names/descriptions moved off `name:`/`description:` LocIds onto the auto-localization `ent-<id>`/`ent-<id>.desc` convention (required by `TestNoManualEntityLocStrings`). Access levels, stacks, and reagents keep their `oxyd-*` LocIds — those prototype kinds are not auto-localized.
- `eris_autoinjectors.rsi` gained `autoinjector1`/`hypospray1` fill-level states for `SolutionContainerVisuals` (`MaxFillLevels = 1`); autoinjectors use explicit `SolutionContainerLayers.Base`/`Fill` layer maps.
- `OxydMedicalOperatingTable` / `OxydMedicalMorgueTray` declare their own `Icon` component (`sprite`+`state`) because inherited parent `Icon` components (`TableBase`, morgue) ship no sprite.

### Pre-existing port content fixed in passing (litany-eris ported protos)
- `mk58_ironhammer.yml`: sprite pointed at nonexistent `mk58_sec.rsi` → `mk58.rsi` (+ `mag-0` state added to the rsi for the native gun-mag layer).
- `flat_cap.rsi`/`collectable_flat_cap.rsi`: `equipped-HELMET` was declared `directions: 4` with a 32px-wide PNG → atlas crash + no-sprite test failure; corrected to `directions: 1`.
- 53 helmet/hood RSIs (incl. `void_helmet.rsi`) were missing the `icon-unshaded`/`light-overlay` states required by `ClothingHeadSuitWithLightBase`; added transparent placeholder states (no emissive overlays were ever drawn for these sprites).
- `OxydNtBiomatterCanister`: jug sprite path updated to current upstream location (`Objects/Specific/Chemistry/jug.rsi`, state `icon_empty`).
- Debug/base items `BaseBundle`, `UnBroken`, `FoodDonutBuffer` marked `abstract` (they have no sprites by design).
- `Wintermule` sprite layer order fixed — a stateless `MagUnder` layer no longer masks the icon.
- `BundleSystem` default bundle proto `BaseBundle`→`BundleGroup`; `OxydGunComponent.casingEntity` made nullable.
- `EntityTableContainerFill` selectors rewritten in `AllSelector { children: [...] }` form (the `table:` wrapper overflows the stack on load).
- Duplicate `GetVerbsEvent<AlternativeVerb>` subscriptions merged — the same component+event may only be subscribed once.
- Missing-reagent substitutions: `Spaceacillin`→`OxydMedSpaceacillin`, `Hyperzine`→`OxydDrugHyperzine`; `reagent-physical-desc-clear`→`reagent-physical-desc-translucent` (no `clear` desc exists).
- `Vapour_mask.yml`/`fake_moustache.yml` descriptions quoted (unquoted `:` broke parsing); duplicate-id collisions resolved (`CentCom._hat`→`erisport_CentCom_hat`, `classic_chefs_apron.`→`erisport_classic_chefs_apron`); `ChemistPDA`→`ChemistryPDA`; `ChemistryBottleEthylredoxrazine` id fix in the medical belt fill; `Heat: 250` damage dict entries for `newGuns.yml`; `auto.yml` firemode `provider`/`validDiff` fields removed (not in schema); `wintermule.yml` `OxydMagazineChamber` converted to `providers` dict; `martin.yml` `visualState` replaced with `GenericVisualizer` firemode-state mapping (`martin_sec.rsi` added); `WeaponPistolMk58Ironhammer` pocket slot added to the Ironhammer job loadout.
