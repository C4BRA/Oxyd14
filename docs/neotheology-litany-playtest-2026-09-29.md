# NeoTheology Litany manual playtest handoff — 2026-09-29

## Status

**Partial live-client validation.** The Disciple test role was entered, its book UI opened, one no-target failure was observed, and one book-initiated cast completed with a visible debit and chant. The other role profiles and Litany effects were not validated in gameplay. Repeated server tick-lag warnings and stalled ghost-role UI interactions ended the run; the user then requested server shutdown, which completed cleanly.

This report separates direct client observations from untested behavior and source-level notes. It is an evidence handoff, not a claim of Litany parity or completion.

## Environment and run boundary

- Checkout: Oxyd14, branch `eris-litany-port`; the worktree was clean before this report was added.
- Local development configuration: `Resources/ConfigPresets/Build/development.toml`; sandbox round on the Dev map, localhost admin elevation, local server on port 1212.
- Server and client builds had completed successfully earlier in the session (server: 0 errors, 496 warnings; client: 0 errors, 250 warnings). No automated tests were run as part of this manual session.
- Server reached Ready and the client entered the round. The server then repeatedly logged `MainLoop: Cannot keep up!` and occasional late entity messages. A second client UI also became unresponsive, so it could not serve as a target-player session.
- The local server was stopped at the user's request with Ctrl+C and exited with code 0. The final partial UI state was an open F5 Entity Spawn Panel filtered to “Preacher”; no live server remains running from this session.
- No gameplay source files were changed. The report and its link from the progress matrix are the only intended workspace changes.

## Confirmed client observations

| Feature | Steps and visible result | Assessment |
| --- | --- | --- |
| Disciple test marker and role entry | F5 Entity Spawn Panel search found `RandomHumanoidSpawnerOxydNtDisciple`, displayed as “NeoTheology disciple [Litany Test Ghost Role]”. After placing the marker, Ghost Roles showed “NeoTheology disciple (litany test)” and its test-role description. The confirmation dialog was accepted and the client entered the role. | **Observed pass** for marker discovery, role-list publication, confirmation, and one role entry. |
| Disciple starting kit and UI | The entered role had a cruciform implant and yellow-cross NeoTheology Bible/book. The book was in the active hand. Pressing Z opened the NeoTheology Litany UI. | **Observed pass** for this role’s basic kit and UI reachability only. Other profiles and actual implant effects were not checked. |
| Profile readout | The UI displayed holiness 50/50, regeneration .0167/s, rank Disciple, and clearance None. | **Observed display only.** Regeneration rate and rank-gated access were not independently tested over time. |
| No-target Cruciform Sense | Selected Cruciform Sense (cost 20, cooldown 1 minute, target VisibleFollower) with no follower available and pressed Begin. The UI reported “The litany fails: No valid target is available.” Holiness stayed at 50. | **Observed expected failure/refund path** for one no-target case. No valid-target sense result was tested. |
| Reveal Adversaries book cast | Selected Reveal Adversaries (description: “Sense classified hostile presences and traps.”; cost 35; cooldown 1 minute; target VisibleArea). Begin showed “Chanting: OxydLitanyRevealAdversaries” and 8.13 seconds remaining. On completion the UI showed “The litany takes hold.”; the complete phrase appeared in chat and holiness changed from 50 to 15. | **Observed pass** for this book-started cast, automatic chant, completion message, and 35-point debit. The intended world scan result was not independently checked against known hostile entities or traps. |
| Immediate repeat after Reveal Adversaries | The Begin control appeared dark/disabled immediately after the completed cast; holiness remained at 15. No enabled recast submission or server rejection was observed. | **Cooldown UI indication only**, not a verified cooldown enforcement result. Recheck after cooldown expiry and try a server submission if the UI allows it. |
| Spoken phrase | While in Disciple, the entered phrase `Et si medio umbrae.` appeared as ordinary speech in chat; no Litany cast result appeared. | **Unresolved / not a confirmed defect.** The progress documentation says Disciple has `canChannel: false`, and the phrase may also have been the wrong phrase. Repeat with a channel-capable profile and its exact configured phrase before filing a bug. |
| Preacher marker | F5 search for “Preacher” showed `RandomHumanoidSpawnerOxydNtPreacher` as “NeoTheology preacher [Litany Test Ghost Role]”. Placing another marker changed the ghost toolbar count from 1 to 2. | **Observed pass** for marker discovery/placement and count update. Preacher role entry and kit were not reached. |
| Ghost Roles panel after adding marker | Clicking the visible “Ghost Roles (2)” control did not open the panel in the recovered session. Pressing F5 did open the Entity Spawn Panel, so the client still accepted at least that input. Earlier in the session the Ghost Roles panel had opened for Disciple. | **Needs reproduction.** This could be a client/server lag or coordinate/input issue; it is not yet attributable to Litany role code. |

## Gameplay gaps for follow-up

Statuses below refer to this session’s direct in-client evidence. “Not tested” means no gameplay conclusion can be drawn, even if code or focused tests cover the behavior.

### Role, rank, inventory, and access matrix

| Profile / path | Manual status | Next check |
| --- | --- | --- |
| Disciple | Entered; cruciform, Bible/book, UI, displayed rank and holiness observed. | Check regeneration over a measured interval, all role entitlements, upgrade slots, and whether speech channeling is correctly unavailable. |
| Acolyte | Not tested. | Spawn its test marker, enter the role, record rank/clearance/holiness, check cruciform and Bible/book, inspect available rites, then attempt one permitted and one denied rite. |
| Agrolyte | Not tested. | Repeat role/kit/UI checks; test plant-growth rite on a known plant and record before/after growth state. |
| Custodian | Not tested. | Repeat role/kit/UI checks; test its door/building rites against valid, invalid, broken, and wrongly faced targets as applicable. |
| Preacher | Marker placed; role not entered. | Reopen Ghost Roles, enter Preacher, compare its kit, access, rank and UI; test spoken and book cast paths. Verify whether a normal Chaplain receives a Bible without test gear. |
| Inquisitor | Not tested in this run. | Enter role; check kit/access, store balance, target-dependent rites, and the group/capture flows. |
| Normal job integration | Not tested. | Check Chaplain mapping, Bible access, normal department access, and whether test-only marker behavior leaks into regular roles. |

### Cast transaction, targeting, and speech

Only the two outcomes in the confirmed observations above were exercised. For each rite below, verify visible effect, exact target, cost, cooldown, failure refund, and cleanup—not just the UI result.

- **Observed:** CruciformSense — one no-target failure/refund; valid target behavior remains untested.
- **Observed:** RevealAdversaries — completed book cast and 35 holiness debit; actual reveal against a known target remains untested.
- **Unverified cast IDs:** AbsolutionOfWounds, AcceleratedGrowth, ActivateDoor, Adoption, Asacris, Atonement, BaptismalRecord, BioreactorChamber, BioreactorSolution, Bounty, CallToBattle, CantoOfCourage, ChantOfObservance, Commitment, Confirmation, Convalescence, Crusade, Deprivation, DivineBlessing, DivineGuidance, DivineIntervention, Entreaty, Epiphany, EternalBrotherhood, Excommunication, GraceOfPerseverance, HandOfMercy, HolyGuidance, Initiation, InstallUpgrade, Knowledge, LispOfVitae, MakeCruciform, Manifestation, Omission, OrderArmaments, Ordination, Penance, PoundingWhisper, PowerBiogenerator, ReclamationOfEndurance, Reincarnation, Rejection, Relief, RepairDoor, Resurrection, Revelation, RevelationOfSecrets, Sanctify, Scrying, SearingRevelation, Sending, SoulHunger, Succour, UninstallUpgrade, UpholdHolyWord, Uproot, WordsOfPurging.
- **Speech and target matrix:** Test exact configured phrases on profiles permitted to channel; compare with book initiation. Test book choice dialogs, front/faced tile, adjacent target, visible follower, station follower, area targets, line of sight, distance, target identity after choice, and two-player privacy. The second client in this run never became usable for these checks.
- **Transaction edges:** Test insufficient holiness, wrong rank, invalid/deleted target, interrupted chant, moving out of range, swapping/removing the book or cruciform, repeated Begin/submit, and cooldown after success. Capture holiness/cooldown before and after each case. Confirm unsuccessful or interrupted casts have no world side effect and no debit/cooldown.
- **Cooldown note:** Progress documentation names Relief, SoulHunger, Entreaty, RevealAdversaries, CruciformSense, and Revelation as one-minute rites. Only RevealAdversaries’ configured value and an immediate dark button were seen live; do not treat the other cooldowns as manually verified.

### Medical, implants, corpses, and altar procedures

None of the following was exercised live:

- Healing, pain/stamina changes, local reagent metabolism, addiction recovery, and WordsOfPurging blood-reagent behavior.
- Rejection with a foreign implant, robotic organ, natural organ, and cruciform present; verify exactly what is retained or removed.
- Reincarnation and Resurrection with a known dead bearer, including stored profile, soul/clone state, body eligibility, and failure cleanup.
- Altar procedures with each relevant posture, restraint, clothing, distance, material, and target-state precondition; record both accepted and rejected attempts.
- Asacris on installed upgrades and rank modules; verify that only the intended upgrades are removed. Also check install/uninstall slots, resource debit, effect activation, and removal cleanup.

### Machines, construction, economy, and objectives

None of the following was exercised live:

- Eye of the Protector observations/rewards, powered state, and player feedback; progress documentation flags oddity rewards and faithless penalties as incomplete due to missing entities.
- Armaments printer, design disks, disk insertion/consumption, printed item, and repeat/invalid-input behavior.
- Blueprint choices, altar/forge, holy door, biogenerator, bioreactor, biomass container, reclaimer, reader, cloner, obelisk, and construction/refund flows. Spawn or build the actual entities and test powered/unpowered, valid/invalid materials, and placement constraints.
- Knowledge/Bounty store balance, debit/refund, purchase inventory, persistence, and insufficient balance. Compare to expected Eris store behavior; do not infer parity from a local hidden store UI.
- Sanctify church-objective signal, Crusade objectives/world state, area effect, atheist mutation, and related HUD feedback. The progress document describes some of these as absent or incomplete, but this playtest did not verify those source notes.

### Group rites and multiplayer

No group rite or two-player target flow was reached. Exercise nearby and distant participants, minimum participant count, starter interruption/death, timeout, duplicate ceremony start, member leaving/rejoining, target change/deletion, and effect delivery to every intended participant. Specifically cover EternalBrotherhood, CallToBattle, Sanctify, Crusade, and SearingRevelation; inspect church/crusade state and HUD effects. Confirm one starter’s cooldown/holiness debit and no unintended debit for participants.

## Environment issues and reproduction notes

1. The server emitted repeated `MainLoop: Cannot keep up!` warnings during the spawn/role/UI sequence and late entity-message warnings for the test clients. The game remained visible, but subsequent UI actions intermittently had no visible result.
2. A second client’s UI timed out and could not be used as a target. The recovered primary client accepted F5 and reopened the entity spawn panel, but the Ghost Roles control did not visibly open after the second marker.
3. Reproduce with one client first and confirm stable server tick behavior before adding the second client. Spawn one marker, confirm its role entry, complete one cast, and inspect the server log. Add the second client only after the UI and server remain responsive.
4. Startup warnings about `fake_moustache.yml` and `AttachmentOxydScope` were also seen earlier in this session; no evidence connected them to NeoTheology, so they are excluded from gameplay findings.

## Suggested next-run evidence format

For every attempt record: role/profile, exact Litany ID, initiation path (speech/book), target entity/state, distance/facing/LOS, holiness before and after, cooldown before and after, cast timer/interruption, exact client result, visible world change, server log, and whether the effect cleans up correctly. Label each result PASS, FAIL, EXPECTED REJECTION, or BLOCKED. A code path or passing focused test does not change a live result from NOT TESTED.

The existing [NeoTheology progress matrix](neotheology-litany-progress.md) supplies the broader feature list and source-documented behavior differences. This report adds only the observations from this 2026-09-29 client session and the remaining manual coverage handoff.
