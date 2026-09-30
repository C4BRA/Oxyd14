# NeoTheology review inventory

Historical baseline inventory accompanying [the review report](neotheology-litany-review.md).
Subsequent fixes and validation are in [the resolution checklist](neotheology-litany-review-fixes.md).

## Scope and interpretation

- Published PR: `e055ca4267` against `97c050d91a`; 134 commits and 246 net changed paths.
- Local follow-ups reviewed at `7df3c31239`; these were unpublished at review time.
- The file ledger records scope and review method, not a claim of independent manual certification of every test line or visual asset.
- Every active source phrase was matched after whitespace normalization and DM placeholder unescaping. This checks completeness of names/phrases, not effect semantics.

## All 60 active litany mappings

All matched; no missing active source phrase. Source files below are relative to CEV-Eris; port locations refer to Oxyd14.

| Port ID | Source declaration | Port data location | Assessment |
| --- | --- | --- | --- |
| `AcceleratedGrowth` | `code/modules/core_implant/cruciform/rituals/agrolyte.dm:7` — `/datum/ritual/cruciform/agrolyte/accelerated_growth` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:2` | F05: requires other mobs; scan is not caster-visible plant set. |
| `HandOfMercy` | `code/modules/core_implant/cruciform/rituals/agrolyte.dm:47` — `/datum/ritual/cruciform/agrolyte/mercy` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:21` | Holy medicine adapter implemented; F03 and front/critical-patient parity need checks. |
| `AbsolutionOfWounds` | `code/modules/core_implant/cruciform/rituals/agrolyte.dm:70` — `/datum/ritual/cruciform/agrolyte/absolution` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:40` | Holy reagents implemented; F03 and native metabolism need checks. |
| `WordsOfPurging` | `code/modules/core_implant/cruciform/rituals/custodian.dm:7` — `/datum/ritual/cruciform/custodian/purging` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:58` | Local addiction recovery/pain adapter; biological rejection and source timing differ. |
| `Epiphany` | `code/modules/core_implant/cruciform/rituals/priest.dm:26` — `/datum/ritual/cruciform/priest/acolyte/epiphany` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:73` | Activation implemented; F01/F16 and objective/observation source hooks missing. |
| `Asacris` | `code/modules/core_implant/cruciform/rituals/priest.dm:68` — `/datum/ritual/cruciform/priest/acolyte/unupgrade` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:90` | F14: physical attachment removed instead of source core-upgrade category. |
| `GraceOfPerseverance` | `code/modules/core_implant/cruciform/rituals/priest.dm:162` — `/datum/ritual/cruciform/priest/acolyte/short_boost/wisdom` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:107` | Local ten-minute duration correction; F03, hearing/Deaf filter needs parity. |
| `UpholdHolyWord` | `code/modules/core_implant/cruciform/rituals/priest.dm:167` — `/datum/ritual/cruciform/priest/acolyte/short_boost/courage` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:130` | Local ten-minute duration correction; F03, hearing/Deaf filter needs parity. |
| `Atonement` | `code/modules/core_implant/cruciform/rituals/priest.dm:172` — `/datum/ritual/targeted/cruciform/priest/atonement` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:153` | Local named speech correction; F03 and local pain instead of halloss. |
| `BaptismalRecord` | `code/modules/core_implant/cruciform/rituals/priest.dm:212` — `/datum/ritual/cruciform/priest/acolyte/records` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:170` | Altar paper implemented; active global scan substitutes for source registry. |
| `DivineIntervention` | `code/modules/core_implant/cruciform/rituals/priest.dm:309` — `/datum/ritual/cruciform/priest/offering/divine_intervention` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:185` | Observation-only offering adaptation; no next-reward selection. |
| `HolyGuidance` | `code/modules/core_implant/cruciform/rituals/priest.dm:316` — `/datum/ritual/cruciform/priest/offering/holy_guidance` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:201` | Oddity requirement omitted; observation replaces next-reward/power behavior. |
| `DivineBlessing` | `code/modules/core_implant/cruciform/rituals/priest.dm:323` — `/datum/ritual/cruciform/priest/divine_blessing` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:217` | F15 book/oddity active-hand conflict; local permanent penalty stacking fixed. |
| `Confirmation` | `code/modules/core_implant/cruciform/rituals/priest.dm:361` — `/datum/ritual/cruciform/priest/confirmation` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:232` | F09 and source clergy-module preservation differ; speech defaults to Acolyte. |
| `Adoption` | `code/modules/core_implant/cruciform/rituals/priest.dm:395` — `/datum/ritual/cruciform/priest/adoption` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:251` | Published version replaced behavior; local clearance correction still needs F07. |
| `Ordination` | `code/modules/core_implant/cruciform/rituals/priest.dm:411` — `/datum/ritual/cruciform/priest/ordination` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:268` | Published rank mutation replaced locally with clearance; F07 grant initialization. |
| `Omission` | `code/modules/core_implant/cruciform/rituals/priest.dm:427` — `/datum/ritual/cruciform/priest/omission` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:283` | Published rank mutation replaced locally with clearance; F07 access-tag consistency. |
| `Excommunication` | `code/modules/core_implant/cruciform/rituals/priest.dm:451` — `/datum/ritual/targeted/cruciform/priest/excommunication` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:298` | Local named speech/clearance changes; F06 specialization grant remains. |
| `OrderArmaments` | `code/modules/core_implant/cruciform/rituals/priest.dm:492` — `/datum/ritual/cruciform/priest/acolyte/buy_item` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:317` | Separate printer adaptation; F12 reachability, no printer blueprint. |
| `DivineGuidance` | `code/modules/core_implant/cruciform/rituals/construction.dm:27` — `/datum/ritual/cruciform/priest/acolyte/blueprint_check` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:332` | Catalog UI implemented; speech requires book; recipe/catalog substitutions. |
| `Manifestation` | `code/modules/core_implant/cruciform/rituals/construction.dm:44` — `/datum/ritual/cruciform/priest/acolyte/construction` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:348` | F11/F12: queued item reuse, ignored BuildTime, parent coordinate mismatch. |
| `Uproot` | `code/modules/core_implant/cruciform/rituals/construction.dm:91` — `/datum/ritual/cruciform/priest/acolyte/deconstruction` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:365` | F11/F12: queued-refund duplication risk and parent coordinate mismatch. |
| `PoundingWhisper` | `code/modules/core_implant/cruciform/rituals/group.dm:52` — `/datum/ritual/group/cruciform/stat/mechanical` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:381` | Local permanent stacking fixed; F10 righteous/Channeling, completion membership/hooks remain. |
| `RevelationOfSecrets` | `code/modules/core_implant/cruciform/rituals/group.dm:75` — `/datum/ritual/group/cruciform/stat/cognition` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:412` | Local permanent stacking fixed; F10 righteous/Channeling, completion membership/hooks remain. |
| `LispOfVitae` | `code/modules/core_implant/cruciform/rituals/group.dm:96` — `/datum/ritual/group/cruciform/stat/biology` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:440` | Local permanent stacking fixed; F10 righteous/Channeling, completion membership/hooks remain. |
| `CantoOfCourage` | `code/modules/core_implant/cruciform/rituals/group.dm:116` — `/datum/ritual/group/cruciform/stat/robustness` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:468` | Local permanent stacking fixed; F10 righteous/Channeling, completion membership/hooks remain. |
| `ChantOfObservance` | `code/modules/core_implant/cruciform/rituals/group.dm:135` — `/datum/ritual/group/cruciform/stat/vigilance` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:496` | Local permanent stacking fixed; F10 righteous/Channeling, completion membership/hooks remain. |
| `ReclamationOfEndurance` | `code/modules/core_implant/cruciform/rituals/group.dm:155` — `/datum/ritual/group/cruciform/stat/toughness` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:524` | Local permanent stacking fixed; F10 righteous/Channeling, completion membership/hooks remain. |
| `Sanctify` | `code/modules/core_implant/cruciform/rituals/group.dm:175` — `/datum/ritual/group/cruciform/sanctify` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:553` | Obelisk forced activity only; no area/objective signal, righteous-life update. |
| `Crusade` | `code/modules/core_implant/cruciform/rituals/group.dm:205` — `/datum/ritual/group/cruciform/crusade` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml:579` | Rites granted; no faction-item crusade activation or ceremony-success world hooks. |
| `Relief` | `code/modules/core_implant/cruciform/rituals/base.dm:18` — `/datum/ritual/cruciform/base/relief` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_common.yml:2` | Native medicine adapter; metabolism/overdose timing needs live parity check. |
| `SoulHunger` | `code/modules/core_implant/cruciform/rituals/base.dm:31` — `/datum/ritual/cruciform/base/soul_hunger` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_common.yml:24` | Nutrition + Heat implemented; unlike Eris refuses an already-full patient. |
| `Entreaty` | `code/modules/core_implant/cruciform/rituals/base.dm:44` — `/datum/ritual/cruciform/base/entreaty` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_common.yml:45` | Same-station active roster instead of source global roster; isolated test passed. |
| `Rejection` | `code/modules/core_implant/cruciform/rituals/base.dm:64` — `/datum/ritual/cruciform/base/reject` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_common.yml:64` | Explicit purge implemented; source embedded objects/malfunction and automatic purity differ. |
| `RevealAdversaries` | `code/modules/core_implant/cruciform/rituals/base.dm:92` — `/datum/ritual/cruciform/base/reveal` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_common.yml:83` | F04: wrong scan origin/recipient; empty-area failure. |
| `CruciformSense` | `code/modules/core_implant/cruciform/rituals/base.dm:125` — `/datum/ritual/cruciform/base/sense_cruciform` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_common.yml:101` | Active-only followers, not every installed cruciform; visibility path implemented. |
| `Revelation` | `code/modules/core_implant/cruciform/rituals/base.dm:152` — `/datum/ritual/cruciform/base/revelation` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_common.yml:119` | F03: reach recheck; front-ray/hallucination/observation-reset/objective hooks differ. |
| `InstallUpgrade` | `code/modules/core_implant/cruciform/rituals/base.dm:177` — `/datum/ritual/cruciform/base/install_upgrade` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_common.yml:137` | Altar/body/clothing path implemented; F03/F14 and native acquisition need checks. |
| `UninstallUpgrade` | `code/modules/core_implant/cruciform/rituals/base.dm:228` — `/datum/ritual/cruciform/base/uninstall_upgrade` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_common.yml:152` | Physical attachment removal implemented; F03/F14. |
| `Reincarnation` | `code/modules/core_implant/cruciform/rituals/base.dm:258` — `/datum/ritual/cruciform/base/reincarnation` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_common.yml:167` | F01/F02: absent normal soul module; snapshot overwrite, not saved-mind transfer. |
| `Commitment` | `code/modules/core_implant/cruciform/rituals/base.dm:311` — `/datum/ritual/cruciform/base/install` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_common.yml:182` | Altar/empty implant/Human gate implemented; F03, grabbed-victim/species rules differ. |
| `Deprivation` | `code/modules/core_implant/cruciform/rituals/base.dm:377` — `/datum/ritual/cruciform/base/ejection` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_common.yml:197` | Same implant extraction implemented; F01/F03, live corpse recovery still unverified. |
| `Penance` | `code/modules/core_implant/cruciform/rituals/inquisitor.dm:32` — `/datum/ritual/targeted/cruciform/inquisitor/penance` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_inquisitor.yml:2` | Local named speech correction; F03, pain/follower recheck. |
| `Convalescence` | `code/modules/core_implant/cruciform/rituals/inquisitor.dm:112` — `/datum/ritual/cruciform/inquisitor/selfheal` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_inquisitor.yml:18` | Native heal/pain adapter implemented; live timing/parity unverified. |
| `Succour` | `code/modules/core_implant/cruciform/rituals/inquisitor.dm:138` — `/datum/ritual/cruciform/inquisitor/heal_other` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_inquisitor.yml:43` | F03: patient can leave reach during chant and still receive healing. |
| `Scrying` | `code/modules/core_implant/cruciform/rituals/inquisitor.dm:189` — `/datum/ritual/cruciform/inquisitor/scrying` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_inquisitor.yml:69` | F08: fixed marker, not following target; active same-station roster differs. |
| `Sending` | `code/modules/core_implant/cruciform/rituals/inquisitor.dm:233` — `/datum/ritual/cruciform/inquisitor/message` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_inquisitor.yml:86` | Book target/text implemented; speech prepared broadcast; F03 target recheck. |
| `Initiation` | `code/modules/core_implant/cruciform/rituals/inquisitor.dm:259` — `/datum/ritual/cruciform/inquisitor/initiation` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_inquisitor.yml:103` | F07/F09/F14: automatic ascension kit substitute, clearance/rank math. |
| `Knowledge` | `code/modules/core_implant/cruciform/rituals/inquisitor.dm:290` — `/datum/ritual/cruciform/inquisitor/check_telecrystals` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_inquisitor.yml:118` | Native hidden-store balance report; no-cost adaptation to source false return. |
| `Bounty` | `code/modules/core_implant/cruciform/rituals/inquisitor.dm:314` — `/datum/ritual/targeted/cruciform/inquisitor/spawn_item` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_inquisitor.yml:133` | Native hidden store; source NT products/prices/deployment remain adapted. |
| `EternalBrotherhood` | `code/modules/core_implant/cruciform/rituals/crusader.dm:8` — `/datum/ritual/cruciform/crusader/brotherhood` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_inquisitor.yml:148` | Status-icon HUD implemented; no source holiness drain/low-power removal. |
| `CallToBattle` | `code/modules/core_implant/cruciform/rituals/crusader.dm:23` — `/datum/ritual/cruciform/crusader/battle_call` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_inquisitor.yml:163` | Timed skill buff implemented; live recipient counts and changed recast strength need check. |
| `SearingRevelation` | `code/modules/core_implant/cruciform/rituals/crusader.dm:52` — `/datum/ritual/cruciform/crusader/flash` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_inquisitor.yml:182` | Local atheist marker + forced knockdown; source scan/victim/message differences. |
| `Resurrection` | `code/modules/core_implant/cruciform/rituals/machinery.dm:13` — `/datum/ritual/cruciform/machines/resurrection` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_machines.yml:2` | F01/F02 block normal soul workflow; rank-based clone damage exists locally. |
| `MakeCruciform` | `code/modules/core_implant/cruciform/rituals/machinery.dm:43` — `/datum/ritual/cruciform/machines/cruciformforge` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_machines.yml:17` | Forge material/job path implemented; local holdable output fixed, native acquisition needed. |
| `ActivateDoor` | `code/modules/core_implant/cruciform/rituals/machinery.dm:79` — `/datum/ritual/cruciform/machines/lock_door` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_machines.yml:32` | Local facing/broken/clearance behavior added; F03/F07; source powered rule differs. |
| `RepairDoor` | `code/modules/core_implant/cruciform/rituals/machinery.dm:100` — `/datum/ritual/cruciform/machines/repair_door` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_machines.yml:47` | Repair implemented; F03, WorldRotation offset in local coordinates needs rotated-grid check. |
| `PowerBiogenerator` | `code/modules/core_implant/cruciform/rituals/machinery.dm:151` — `/datum/ritual/cruciform/machines/power_biogen_awake` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_machines.yml:63` | F13 failed fuel debit; source near-console targeting becomes front machine. |
| `BioreactorSolution` | `code/modules/core_implant/cruciform/rituals/machinery.dm:200` — `/datum/ritual/cruciform/machines/bioreactor/solution` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_machines.yml:78` | Single-entity chamber/pump adapter; source range/plumbing/material processing differ. |
| `BioreactorChamber` | `code/modules/core_implant/cruciform/rituals/machinery.dm:219` — `/datum/ritual/cruciform/machines/bioreactor/chamber_doors` | `Resources/Prototypes/_Oxyd/NeoTheology/litanies_machines.yml:93` | Single-entity chamber state; source multipart seal/door behavior differs. |

## All 134 published PR commits

GitHub paginated commit list, in API/history order; merge commits retained. Review focuses on the final code, so superseded intermediate implementations are not separate outstanding defects.

| # | Commit | Subject |
| --- | --- | --- |
| 1 | `14acc56828` | checkpoint before checking out eris-litany-port |
| 2 | `c6b7241004` | feat(neotheology): add litany catalog validation |
| 3 | `2d56fe074f` | fix(neotheology): centralize holiness profile calculations |
| 4 | `ad89ad0bf1` | fix(skills): return refreshed unique buff data |
| 5 | `ca63c51446` | feat(neotheology): add private litany book UI |
| 6 | `b2f059e7ba` | docs(neotheology): record litany port progress |
| 7 | `0156dff0ef` | test(skills): cover unique buff refresh behavior |
| 8 | `ed345bba01` | fix(neotheology): use shared speech event contracts |
| 9 | `8681c4f2c1` | style(chat): remove empty declaration line |
| 10 | `01791d39d7` | docs(neotheology): align progress ledger with event contract |
| 11 | `e91c95b0ac` | fix(neotheology): make profiles and UI data-driven |
| 12 | `acc944a96a` | feat(neotheology): complete litany milestone 1 baseline contracts |
| 13 | `2b008ab721` | docs(neotheology): record milestone 1 tip SHA |
| 14 | `591fafd14f` | docs(neotheology): sync milestone 1 tip to branch HEAD |
| 15 | `6b9b3de457` | docs(neotheology): pin milestone 1 feature tip |
| 16 | `e42bc2e860` | test(neotheology): M2 cruciform lifecycle coverage + safe duplicate reject |
| 17 | `0f0e6b9fcd` | fix(neotheology): clear pending cast on cruciform death |
| 18 | `988219dcee` | feat(neotheology): M3 litany speech hook and cast transaction |
| 19 | `564b89533a` | docs(neotheology): pin milestone 3 tip SHA |
| 20 | `a52de6016e` | docs(neotheology): fix milestone 3 evidence tip |
| 21 | `ed22f9626f` | fix(neotheology): bind book cast to next speech sequence |
| 22 | `7cf78157be` | feat(neotheology): M4 Packet A Bible UI shell and privacy |
| 23 | `770fa4c512` | feat(neotheology): M4 Packet B Relief and SoulHunger handlers |
| 24 | `7796e4c17d` | feat(neotheology): M4 Packet C Entreaty and CruciformSense handlers |
| 25 | `4f0783acc0` | docs: reconcile litany implementation progress |
| 26 | `bc4ac362ad` | fix(neotheology): address PR #33 review round 2 (P1) |
| 27 | `c140fb63ad` | refactor(neotheology): drop analgesia, heal through damage specifiers (P1) |
| 28 | `4294425798` | fix(neotheology): resolve rules by Selected, not a hardcoded prototype id (P1) |
| 29 | `da4962683d` | refactor(neotheology): share cruciform bearer lookups in SharedCruciformSystem (P2) |
| 30 | `52ea2d8b3c` | refactor(neotheology): replace litany parameter blocks with LitanyEffect classes (P1) |
| 31 | `a890538b98` | data(neotheology): convert litany parameters to effect lists (P1) |
| 32 | `9274f2c6fe` | docs(neotheology): correct stale rules-id gap and link the bottom-up plan |
| 33 | `e73ce55a5c` | [P1] neotheology: add oxydCoreModule prototype type |
| 34 | `5d7493933b` | [P1] cruciform: track installed core modules |
| 35 | `f3bd2b7023` | [P1] neotheology: add core module lifecycle events |
| 36 | `06239847dd` | [P1] cruciform: add core module install/uninstall system (P1) |
| 37 | `0a1757b7c8` | [P1] cruciform: derive unlocked litany sets from profile and modules (P1) |
| 38 | `7902973858` | [P1] cruciform: fold module multipliers and righteous life into capacity/regen (P1) |
| 39 | `2ed97b9340` | [P1] cruciform: grant module access levels to bearers (P1) |
| 40 | `aeb7073bfa` | [P1] neotheology: define Eris core modules as prototype data (P1) |
| 41 | `3693958a44` | [P1] neotheology: split priest_convert off the cloning module (P1) |
| 42 | `c1cfd4b9b3` | [P1] cruciform: port Eris make_* rank conversions as profile+module swaps (P1) |
| 43 | `d296b3b1fd` | [P1] neotheology: align profile access privileges with Eris ranks (P1) |
| 44 | `7fb145417d` | [P1] cruciform: apply module activation profile on activation (P1) |
| 45 | `327ca884a2` | [P1] neotheology: add job to profile map on rules prototype |
| 46 | `d2f8f357fc` | [P1] neotheology: map the chaplain job to the preacher profile (P1) |
| 47 | `77333d65a8` | [P1] neotheology: grant cruciforms to jobs via rules mapping (P1) |
| 48 | `d4906629aa` | [P1] cruciform: add idempotent GrantCruciform entry point (P1) |
| 49 | `74d6b85a10` | [P1] cruciform: add ranged bearer enumeration helper |
| 50 | `0bd808b7d2` | [P1] cruciform: add installable upgrade component |
| 51 | `f2e2cabdae` | [P1] cruciform: install and uninstall upgrades on the cruciform (P1) |
| 52 | `834e6e4672` | [P1] neotheology: add cruciform upgrade entities and locale |
| 53 | `639690dc74` | [P2] neotheology: give the altar an item-search radius |
| 54 | `1619874cf1` | [P2] neotheology: add altar item lookup (P2) |
| 55 | `a4615ecc02` | [P2] neotheology: add biomatter material and stack (P2) |
| 56 | `8b17f2f373` | [P2] botany: biomass reclaimer outputs biomatter (P2) |
| 57 | `21bb255c7a` | [P2] neotheology: holy door repair consumes biomatter (P2) |
| 58 | `5b30743018` | [P2] neotheology: add cruciform forge with material accounting (P2) |
| 59 | `35835cfc44` | [P2] neotheology: add the cruciform forge machine (P2) |
| 60 | `91c8bfb411` | [P2] neotheology: flattened biogenerator power machine (P2) |
| 61 | `4574bcd91e` | [P2] neotheology: flattened bioreactor with chamber state (P2) |
| 62 | `fb2c5b2a13` | [P2] neotheology: bank machine materials in MaterialStorage (P2) |
| 63 | `23be684349` | [P2] neotheology: add biogenerator and bioreactor machines (P2) |
| 64 | `3a28031402` | [P2] neotheology: snapshot the wearer's soul on the cloning module (P2) |
| 65 | `b80ebfdfa2` | [P2] neotheology: cruciform reader, cloner and biomass container (P2) |
| 66 | `5baa06a018` | [P2] neotheology: obelisk aura, hostile damage and cap (P2) |
| 67 | `e6dd7e3a8c` | [P2] neotheology: add obelisk machine with deferred Eye hook (P2) |
| 68 | `c632b6321e` | [P2] neotheology: add oxydArmament prototype type |
| 69 | `eb2d9919f5` | [P2] neotheology: armaments printer with discount schedule (P2) |
| 70 | `5c0ceee284` | [P2] neotheology: port the Eris armament catalog (P2) |
| 71 | `bcfb8e2774` | [P3] neotheology: cheaper holy hand grenade for the armament catalog (P3) |
| 72 | `4f6ad37770` | [P3] eotp: add eye of the protector component |
| 73 | `79a6394488` | [P3] eotp: accrue observation from faithful in range (P3) |
| 74 | `44eeb6cefb` | [P3] eotp: obelisks feed the eye's observation (P3) |
| 75 | `1920834495` | [P3] eotp: bless the faithful in range (P3) |
| 76 | `8c6e7edfdf` | [P3] eotp: bank armament points on the eye (P3) |
| 77 | `0a2c618030` | [P3] eotp: periodic miracles funded by observation (P3) |
| 78 | `ed2e1fa47a` | [P3] eotp: eye of the protector machine and status UI (P3) |
| 79 | `222e9c0d38` | [P3] eotp: altar offerings feed the eye (P3) |
| 80 | `3e2daa5b5b` | [P3] neotheology: bounded scrying sessions (P3) |
| 81 | `f364910efc` | [P4] litany: implement target-mode resolution (P4) |
| 82 | `99ddd54afd` | [P4] litany: StationFollower tolerates zero recipients (P4) |
| 83 | `8acd4c405b` | [P4] litany: ActivateDoor handler (P4) |
| 84 | `32d4391405` | [P4] litany: medical handlers — HandOfMercy, Absolution, Convalescence, Succour (P4) |
| 85 | `d1bce41f82` | [P4] litany: skill buff handlers (P4) |
| 86 | `006a572de7` | [P4] litany: sanity handlers — Revelation, DivineBlessing, Epiphany (P4) |
| 87 | `57357ce040` | [P4] litany: Commitment and Deprivation handlers (P4) |
| 88 | `3b51f0c91a` | [P4] litany: conversion role handlers (P4) |
| 89 | `1574900abb` | [P4] litany: upgrade install/uninstall handlers (P4) |
| 90 | `33f31ee600` | [P4] litany: soul snapshot and resurrection handlers (P4) |
| 91 | `aee15c8da3` | [P4] litany: machine handlers (P4) |
| 92 | `f7cbfde49a` | [P4] litany: scrying handler (P4) |
| 93 | `f21c6ee00f` | [P4] litany: offering handlers (P4) |
| 94 | `120f21ce9d` | [P4] litany: armaments order handler (P4) |
| 95 | `d8c48177d8` | [P4] litany: initiation handler (P4) |
| 96 | `6564fbe61d` | [P4] litany: sending handler (P4) |
| 97 | `55974202b6` | [fix] neotheology: apply the independent review fixes (server) |
| 98 | `6d8b73e3f4` | [test] neotheology: add the review regression tests |
| 99 | `aabf418e1b` | [docs] neotheology: refresh the progress ledger and dependency map |
| 100 | `4b5970f3c4` | [fix] neotheology: close the remaining review blockers |
| 101 | `de2cc4b591` | [fix] neotheology: align the Eye economy with Eris |
| 102 | `96eaed2cfc` | [feat] neotheology: atomic cast transaction and UI result publication |
| 103 | `738ad6dfcf` | [feat] neotheology: enable BaptismalRecord from the altar |
| 104 | `0c3cdb1294` | [feat] neotheology: land the first dependency packet (7 litanies) |
| 105 | `0d87703433` | [docs] neotheology: record 44/60 progress and the packet wave |
| 106 | `89e4cc1784` | [feat] neotheology: server-owned book choice flow |
| 107 | `17b7908f45` | [feat] neotheology: cruciform upgrade behaviours |
| 108 | `65f3c23967` | [docs] neotheology: record the Stage 3 close-out |
| 109 | `45b24941f2` | [feat] neotheology: construction packet |
| 110 | `158939bbb3` | [docs] neotheology: record the construction packet (47/60) |
| 111 | `fa8a5996c7` | [feat] neotheology: NtUplink packet |
| 112 | `d03ac2b731` | [docs] neotheology: record the NtUplink packet (49/60) |
| 113 | `7e9c0afcb7` | [feat] neotheology: ceremony engine and the eleven ceremonies |
| 114 | `3454976826` | [docs] neotheology: record the ceremony packet (60/60) |
| 115 | `975c806085` | [fix] erisported: quote descriptions that break the YAML parse |
| 116 | `bc3d94fa51` | [fix] neotheology: audit corrections (Asacris target, reclaimer blueprint) |
| 117 | `611a061d21` | [chore] branch: drop the .freebuff preview files |
| 118 | `a59033316f` | [docs] neotheology: record the Stage 6 audit and release state |
| 119 | `5f82baaf84` | [fix] neotheology: close the six litany audit defects |
| 120 | `c123ad9e62` | [test] neotheology: add litany audit regression coverage |
| 121 | `292d1d2da8` | [docs] neotheology: record the audit fix and the final validation |
| 122 | `b75c08fd2e` | [fix] neotheology: answer the ECS review |
| 123 | `1bd8bddcde` | [fix] chat: preserve the original speech data |
| 124 | `3688ed0e55` | [fix] neotheology: sell single-use armament design disks |
| 125 | `97838e7d80` | [fix] neotheology: apply the ECS and visibility review requests |
| 126 | `a45e194384` | [docs] neotheology: record the review fixes and test results |
| 127 | `5da2a280c5` | chore: remove unrelated files from litany PR |
| 128 | `d2766be7ad` | docs(neotheology): align progress notes with PR scope |
| 129 | `b046afa5e1` | Complete litany medical effects and altar procedures |
| 130 | `1793468ca4` | Avoid sandbox-restricted list initialization in litany catalog |
| 131 | `3f81ba8a5d` | Merge AEV-Oxyd master into litany port |
| 132 | `b6c3d4ad01` | Document merged litany status and live gameplay gaps |
| 133 | `430180cbe2` | Record merged client and server smoke check |
| 134 | `e055ca4267` | Restore NeoTheology litany test ghost roles |

## All 246 published net changed paths

No pagination truncation: this list is from the local final Git diff, not `gh pr view`'s first 100 files.

| Path | Review method / validation scope |
| --- | --- |
| `Content.Client/_Oxyd/NeoTheology/NtDiscipleHudSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Client/_Oxyd/NeoTheology/UI/ArmamentsPrinterBoundUserInterface.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Client/_Oxyd/NeoTheology/UI/ArmamentsPrinterWindow.xaml` | UI wiring plus client compilation; live layout/accessibility not certified. |
| `Content.Client/_Oxyd/NeoTheology/UI/ArmamentsPrinterWindow.xaml.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Client/_Oxyd/NeoTheology/UI/EyeOfTheProtectorBoundUserInterface.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Client/_Oxyd/NeoTheology/UI/EyeOfTheProtectorWindow.xaml` | UI wiring plus client compilation; live layout/accessibility not certified. |
| `Content.Client/_Oxyd/NeoTheology/UI/EyeOfTheProtectorWindow.xaml.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Client/_Oxyd/NeoTheology/UI/LitanyBoundUserInterface.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Client/_Oxyd/NeoTheology/UI/LitanyWindow.xaml` | UI wiring plus client compilation; live layout/accessibility not certified. |
| `Content.Client/_Oxyd/NeoTheology/UI/LitanyWindow.xaml.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.IntegrationTests/Tests/Changeling/ChangelingSlimeTests.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/Storage/StorageInteractionTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/AltarOfferingTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/AltarTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/ArmamentsPrinterTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/BiogeneratorTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/BiomatterReclaimerTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/BioreactorTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/CoreModuleTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/CruciformForgeTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/CruciformLifecycleTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/CruciformReaderTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/CruciformSoulTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/CruciformUpgradeBehaviorTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/CruciformUpgradeTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/EyeOfTheProtectorTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyAuditRegressionTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyCastTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyCeremonyTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyChoiceTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyEffectsArmamentsTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyEffectsCommitmentTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyEffectsConstructionTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyEffectsConversionTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyEffectsFaithTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyEffectsMachinesTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyEffectsMedicalTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyEffectsOfferingsTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyEffectsRecordsTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyEffectsScryingTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyEffectsSendingTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyEffectsSkillTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyEffectsSocialTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyEffectsSoulTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyEffectsUpgradeTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyEffectsUplinkTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyMedicalDependenciesTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyPrototypeTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanySecurityTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyTargetTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/LitanyUiTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/NeoTheologyDoorTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/NeoTheologyJobTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/NeoTheologyTestMap.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/NeoTheologyTestRoleTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/ObeliskTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/ScryingTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.IntegrationTests/Tests/_Oxyd/NeoTheology/SocialNoticeGameTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.Server/Chat/Systems/ChatSystem.PrivateAPI.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Server/Lathe/LatheSystem.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Server/Medical/BiomassReclaimer/BiomassReclaimerComponent.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Server/Medical/BiomassReclaimer/BiomassReclaimerSystem.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Server/_Oxyd/Chat/ChatSystem.Language.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Server/_Oxyd/Framework/ViewCalc/ViewCalcSystem.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Server/_Oxyd/Framework/ViewCalc/ViewRelevantComponent.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Server/_Oxyd/Medical/AddictionSystem.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Server/_Oxyd/NeoTheology/CoreModuleBehaviorSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/CoreModuleSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/CruciformSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/CruciformUpgradeBehaviorSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/CruciformUpgradeSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/EyeOfTheProtectorSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/LitanyPrototypeValidationSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/LitanySystem.Casting.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/LitanySystem.Ceremony.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/LitanySystem.Speech.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/LitanySystem.UI.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/LitanySystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/Machines/AltarSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/Machines/ArmamentsPrinterSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/Machines/BiogeneratorSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/Machines/BioreactorSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/Machines/CruciformForgeSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/Machines/CruciformReaderSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/Machines/NeoTheologyDoorSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/Machines/NeoTheologyMachineSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/Machines/ObeliskSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/NeoTheologyConstructionSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/NeoTheologyFoundationSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/NeoTheologyJobSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/NtUplinkSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Server/_Oxyd/NeoTheology/ScryingSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/Botany/Components/PlantGrowthComponent.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Shared/Botany/Systems/PlantGrowthSystem.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Shared/Buckle/SharedBuckleSystem.Buckle.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Shared/Chat/MessageData.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Shared/Chat/SharedChatEvents.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Shared/EntityEffects/SharedEntityEffectsSystem.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Shared/Traits/Assorted/PainNumbnessSystem.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Shared/_Oxyd/Medical/AddictionComponent.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Shared/_Oxyd/Medical/MedicalEntityEffects.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Shared/_Oxyd/Medical/MedicalTraits.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Shared/_Oxyd/Medical/PainComponent.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Shared/_Oxyd/Medical/PainSystem.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Shared/_Oxyd/Medical/RoboticOrganSystem.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Shared/_Oxyd/NeoTheology/Components/ActiveCeremonyComponent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Components/ArmamentsPrinterComponent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Components/BiogeneratorComponent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Components/BioreactorComponent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Components/CruciformBearerComponent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Components/CruciformClonerComponent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Components/CruciformComponent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Components/CruciformForgeComponent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Components/CruciformReaderComponent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Components/CruciformSoulComponent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Components/CruciformUpgradeBehaviorComponents.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Components/CruciformUpgradeComponent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Components/EyeOfTheProtectorComponent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Components/LitanyBookComponent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Components/NeoTheologyAltarComponent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Components/NeoTheologyDoorComponent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Components/NeoTheologyTestRoleComponent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Components/NtDiscipleHudComponent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Components/NtUplinkComponent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Components/ObeliskComponent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Components/ScryingSessionComponent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/DamageableSystem.Oxyd.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyAcceleratedGrowthEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyActivateDoorEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyAdoptionEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyArmamentsEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyAsacrisEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyBaptismalRecordEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyBioreactorChamberEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyBioreactorSolutionEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyBountyEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyCallToBattleEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyCeremonyEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyCommitmentEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyConfirmationEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyCruciformSenseEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyCrusadeEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyDeprivationEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyDivineBlessingEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyDivineGuidanceEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyEffectSystem.Medical.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyEffectSystem.Procedures.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyEffectSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyEntreatyEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyEpiphanyEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyEternalBrotherhoodEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyExcommunicationEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyGroupStatEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyHealEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyInitiationEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyInjectReagentsEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyInstallUpgradeEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyKnowledgeEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyMakeCruciformEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyManifestationEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyOfferingEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyOmissionEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyOrdinationEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyPainEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyPowerBiogeneratorEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyReincarnationEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyRejectionEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyRepairDoorEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyResurrectionEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyRevealAdversariesEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyRevelationEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanySanctifyEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyScryingEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanySearingRevelationEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanySendingEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanySkillEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanySoulHungerEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyUninstallUpgradeEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyUprootEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyWordsOfPurgingEffect.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Events/CoreModuleEvents.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Events/LitanyDoAfterEvent.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Events/LitanyEffectBridgeEvents.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/LitanyCatalogValidator.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/LitanyHandlerCatalog.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/LitanyPhraseParser.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/NeoTheologyHoliness.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/NeoTheologySkills.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/NeoTheologyTypes.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Prototypes/ArmamentPrototype.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Prototypes/CoreModulePrototype.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Prototypes/LitanyPrototype.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Prototypes/LitanySetPrototype.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Prototypes/NeoTheologyBlueprintPrototype.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Prototypes/NeoTheologyProfilePrototype.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Prototypes/NeoTheologyRulesPrototype.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/Prototypes/OfferingPrototype.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/SharedCruciformSystem.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/UI/ArmamentsPrinterUi.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/UI/EyeOfTheProtectorUi.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/UI/LitanyUiKey.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/NeoTheology/UI/LitanyUiMessages.cs` | Runtime flow / schema / UI review; focused tests and source comparison. |
| `Content.Shared/_Oxyd/Skills/SharedSkillSystem.cs` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Content.Tests/Shared/_Oxyd/NeoTheology/LitanyCatalogPolicyTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.Tests/Shared/_Oxyd/NeoTheology/LitanyPhraseParserTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.Tests/Shared/_Oxyd/NeoTheology/LitanyRulesTest.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Content.Tests/Shared/_Oxyd/Skills/SharedSkillSystemTests.cs` | Coverage/fixture assessment and focused test scope; see test limits and F16. |
| `Resources/Locale/en-US/_Oxyd/neotheology/armaments.ftl` | Localization/catalog references and focused startup validation; prose not gameplay proof. |
| `Resources/Locale/en-US/_Oxyd/neotheology/cruciform.ftl` | Localization/catalog references and focused startup validation; prose not gameplay proof. |
| `Resources/Locale/en-US/_Oxyd/neotheology/litanies.ftl` | Localization/catalog references and focused startup validation; prose not gameplay proof. |
| `Resources/Locale/en-US/_Oxyd/neotheology/machines.ftl` | Localization/catalog references and focused startup validation; prose not gameplay proof. |
| `Resources/Locale/en-US/_Oxyd/neotheology/medical.ftl` | Localization/catalog references and focused startup validation; prose not gameplay proof. |
| `Resources/Locale/en-US/_Oxyd/neotheology/messages.ftl` | Localization/catalog references and focused startup validation; prose not gameplay proof. |
| `Resources/Locale/en-US/_Oxyd/neotheology/ui.ftl` | Localization/catalog references and focused startup validation; prose not gameplay proof. |
| `Resources/Locale/en-US/_Oxyd/neotheology/upgrades.ftl` | Localization/catalog references and focused startup validation; prose not gameplay proof. |
| `Resources/Locale/en-US/_Oxyd/neotheology/uplink.ftl` | Localization/catalog references and focused startup validation; prose not gameplay proof. |
| `Resources/Locale/en-US/stack/stacks.ftl` | Localization/catalog references and focused startup validation; prose not gameplay proof. |
| `Resources/Prototypes/Body/species_base.yml` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Resources/Prototypes/Entities/Mobs/Player/clone.yml` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Resources/Prototypes/Reagents/narcotics.yml` | Cross-cutting diff/caller inspection and build/startup coverage; full unrelated suites not run. |
| `Resources/Prototypes/_Oxyd/NeoTheology/access.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Prototypes/_Oxyd/NeoTheology/armaments.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Prototypes/_Oxyd/NeoTheology/blueprints.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Prototypes/_Oxyd/NeoTheology/entities.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Prototypes/_Oxyd/NeoTheology/litanies_clergy.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Prototypes/_Oxyd/NeoTheology/litanies_common.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Prototypes/_Oxyd/NeoTheology/litanies_inquisitor.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Prototypes/_Oxyd/NeoTheology/litanies_machines.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Prototypes/_Oxyd/NeoTheology/litany_sets.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Prototypes/_Oxyd/NeoTheology/machines.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Prototypes/_Oxyd/NeoTheology/materials.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Prototypes/_Oxyd/NeoTheology/medicine.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Prototypes/_Oxyd/NeoTheology/modules.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Prototypes/_Oxyd/NeoTheology/offerings.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Prototypes/_Oxyd/NeoTheology/profiles.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Prototypes/_Oxyd/NeoTheology/robotic_organs.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Prototypes/_Oxyd/NeoTheology/rules.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Prototypes/_Oxyd/NeoTheology/status_icons.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Prototypes/_Oxyd/NeoTheology/test_roles.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Prototypes/_Oxyd/NeoTheology/upgrades.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Prototypes/_Oxyd/NeoTheology/uplink.yml` | Source catalog/recipe/profile reachability review and focused prototype validation. |
| `Resources/Textures/Oxyd/NeoTheology/disciple_hud.rsi/disciple.png` | Asset inventory only; no visual certification. |
| `Resources/Textures/Oxyd/NeoTheology/disciple_hud.rsi/meta.json` | RSI metadata inventory / startup validation; no visual certification. |
| `docs/neotheology-litany-dependencies.md` | Read and compared against current code/test evidence; stale claims recorded. |
| `docs/neotheology-litany-progress.md` | Read and compared against current code/test evidence; stale claims recorded. |

## Five litany follow-ups retained (unpublished at review time)

```text
192c0b1747 Make the cruciform holdable and drop Deprivation beside the corpse
6cbf5cfd61 Aim named rites and machine litanies at the faced target
e746f1fc8c Keep cruciform clearance separate from church rank
5166cb2eef Stack litany skill changes and limit short blessings to ten minutes
7df3c31239 End a ceremony only when its leader dies or loses the cruciform
```

The unrelated original sixth commit, `8a04c267be` (ricochet), is archived outside the canonical branch.
