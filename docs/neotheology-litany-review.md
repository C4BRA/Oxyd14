# NeoTheology / Litany review — 2026-09-30

> Historical review of the revisions below. Subsequent fixes and their
> validation are tracked in [the resolution checklist](neotheology-litany-review-fixes.md).
> Findings here are retained as baseline evidence, not a description of every current worktree behavior.

## Verdict

**Do not describe PR #33 as a complete NeoTheology port or merge it on the strength of “60/60 enabled”.** All 60 active Eris litany phrases have corresponding port entries, but several end-to-end flows are broken or intentionally replaced. The dependency inventory measures catalog coverage, not feature parity.

This is a source-level correctness/parity review of the final PR diff and the local follow-up changes, with fresh focused tests. It is not a live multiplayer certification, a visual asset review, or an assertion that every possible defect has been found. No gameplay fixes were applied during this review.

### Exact scope

| Item | Revision / scope |
| --- | --- |
| Source | `CEV-Eris` `5f7847f26` |
| Open PR | [AEV-Oxyd/Oxyd14#33](https://github.com/AEV-Oxyd/Oxyd14/pull/33), `C4BRA:eris-litany-port` → `AEV-Oxyd:master` |
| Published PR head | `e055ca42677003fc4785d947c60e3934dda69cbf` |
| Upstream base | `97c050d91ac4144a9bd23198bb41cfa4d53fcfc6` |
| Published scope | **134 commits, 246 changed files, +28,191 / −131 lines** |
| Reviewed local follow-up head | `7df3c31239` — five unpublished litany commits on top of the PR |
| RobustToolbox used for tests | `edf061e7450a4074f173e3000bf1552b6f54082f` — the litany branch's recorded revision |

The [inventory appendix](neotheology-litany-review-inventory.md) lists every published commit and changed path, plus all 60 source-to-port phrase mappings. Commit history was inspected for superseded work and branch integration; findings below concern final behavior, not bugs already fixed in historical commits. Test/helper files were assessed as coverage, not treated as gameplay evidence. Some findings specifically concern the unpublished local follow-ups and are labelled accordingly.

## Findings, highest priority first

Locations below refer to the local reviewed head unless otherwise stated. **P1** = core flow broken / exploitable; **P2** = significant correctness or parity defect; **P3** = presentation/documentation. “Reproduction” is a recommended regression check unless explicitly marked executed.

### F01 — P1: normal cruciforms never acquire a soul backup

- **Both published PR and local branch.** `Content.Server/_Oxyd/NeoTheology/CruciformSystem.cs:420–444,567–575`; `CoreModuleBehaviorSystem.cs:35–50,75–128`.
- `GrantCruciform` installs the rank table's modules. None of its six rows includes `OxydNtModuleCloning`. `Activate` also never adds the cloning module or writes the soul. The snapshot handler only runs for explicit cloning-module installation/removal or the snapshot litany bridge.
- Consequently a normal Chaplain, test role, or freshly baptized disciple has no soul snapshot. Reincarnation refuses its missing module; a recovered implant cannot be read for Resurrection. The soul tests manually install the module, hiding the missing gameplay wiring.
- Eris `cruciform.dm:activate/update_data` automatically adds the cloning module and saves the bearer.
- **Smallest correction:** install/refresh cloning at the authoritative activation/grant lifecycle boundary; capture the latest bearer data at appropriate death/extraction boundaries without replacing another person's stored soul.
- **Check:** grant each profile through the real job/ghost-role path, give it a mind, die, extract, insert into the reader, and resurrect without a test-only module install.

### F02 — P1: Reincarnation replaces the soul instead of restoring it

- **Both.** `Content.Shared/_Oxyd/NeoTheology/Effects/LitanyReincarnationEffect.cs:19–50`; `CoreModuleBehaviorSystem.cs:75–128`.
- The effect writes the current wearer's name, appearance and mind over the existing snapshot. It never transfers the stored mind into the prepared body, checks DNA compatibility, or requires a once-activated but inactive implant. It can run on an already-active bearer.
- Eris `rituals/base.dm:reincarnation` and `cruciform.dm:transfer_soul` reunite the saved soul with its matching new body. Eris Resurrection grows an unoccupied vessel; the subsequent commitment/reincarnation workflow restores the soul. The port transfers the mind during Resurrection instead and leaves Reincarnation as a different operation.
- This is not just missing flavor: the operation can destroy the prior saved identity. The existing test explicitly asserts this replacement behavior.
- **Correction:** implement the intended saved-soul transfer and guards, or explicitly retire/rename this operation as a non-parity adaptation. Do not silently advertise it as the Eris rite.

### F03 — P1: commit does not revalidate target geometry or follower identity

- **Both.** `LitanySystem.Casting.cs:168–270,550–797`; `Effects/LitanyPainEffect.cs`, `LitanyHealEffect.cs`, `LitanyAdoptionEffect.cs`, `LitanySendingEffect.cs`.
- Begin records entity targets; commit checks caster entitlement and effect-specific properties but never checks that the recorded targets still meet their original distance, facing, visibility, map/station or active-follower requirements.
- A nearby patient can leave reach during Succour and still be healed. A visible named punishment target can leave view or lose the cruciform; `LitanyPainEffect.CanApply` only checks a nonempty list and optional Godblood. Machine markers can likewise move away while component-only handlers remain valid.
- Eris Succour explicitly checks reach after its delay. The port's comment that targets are “revalidated” overstates the actual checks.
- **Smallest correction:** central validation of the recorded targets against the original target mode at commit; preserve the chosen identity, rather than selecting a replacement. Include deleted/queued entities and required cruciform linkage.
- **Check:** move the selected patient off-map / out of reach; remove the follower implant; move a machine during a chant. No effect, debit or cooldown should remain after rejection.

### F04 — P1: Reveal Adversaries scans for the bystanders, not the caster

- **Both.** `Resources/Prototypes/_Oxyd/NeoTheology/litanies_common.yml:86–104`; `Effects/LitanyRevealAdversariesEffect.cs`; `NeoTheologyFoundationSystem.cs:90–124`; `LitanySystem.Casting.cs:740–755`.
- `VisibleArea` resolves nearby mobs **excluding the actor**. The effect raises the scan on each mob; the server scans around that mob and shows its popup to that mob.
- Alone with a trap, the caster fails target resolution. With a bystander, that bystander receives the information and the caster pays. Several bystanders also mean several separate randomized scans. The manual playtest confirmed a debit, not the correct recipient or scan result.
- **Smallest correction:** caster-anchored effect/target mode. Independently preserve the source's separate hostile/trap ranges, live-hostile filtering and trap chance; the current scan includes dead faction members and does not implement the separate 80% trap roll.
- **Check:** lone caster plus a known mine; caster plus bystander. Only the caster receives the result, exactly once.

### F05 — P1: Accelerated Growth needs another mob and grows around that mob

- **Both.** `litanies_clergy.yml:1–19`; `Effects/LitanyAcceleratedGrowthEffect.cs`; `NeoTheologyFoundationSystem.cs:181–197`.
- The same `VisibleArea` mistake feeds nearby mobs to a handler which scans plants around its recipient. A lone Agrolyte surrounded by crops cannot begin. Nearby people shift the affected plant area and can cause repeated application.
- The scan is spatial only, not the Eris `view(user)` plant selection, so crops behind walls can be boosted.
- **Smallest correction:** one caster-origin plant scan with the desired visibility rule and a no-plants failure before payment.

### F06 — P1: Excommunication's local module removal does not revoke specialization litanies

- **Local follow-up `e746f1fc8c`.** `CruciformSystem.cs:102–115,599–616`; `profiles.yml`; `Effects/LitanyExcommunicationEffect.cs`.
- `OnLitanyRemoveSpecialization` removes Acolyte/Agrolyte/Custodian modules but leaves `Profile` unchanged. `RecomputeProfile` immediately adds that profile's litany sets back into `UnlockedSets`.
- An excommunicated Agrolyte therefore still has the Agrolyte rites. The local change correctly tries to distinguish clearance from rank but retains two sources of entitlement that contradict the removal.
- **Smallest correction:** make specialization entitlement derive from one authoritative state. Preserve priest/inquisitor modules according to source semantics; revoke the actual specialization, not just one duplicate grant.
- **Check:** excommunicate each specialization and assert the actual unlocked sets / cast denials, not only clearance or module membership.

### F07 — P2: local clearance initialization blocks normal clergy; public doors are not public

- **Local follow-up `e746f1fc8c`.** `CruciformSystem.cs:93–98,420–444,513–547`; `Machines/NeoTheologyDoorSystem.cs:45–61`; `entities.yml:87–117`.
- `GrantCruciform` and Initiation call `MakeRank`, which does not initialize clearance. Only the separate `MakePriest`/`MakeInquisitor` wrappers set Clergy, and those are not the grant/conversion paths. New clergy consequently remain at `None` and fail the new holy-door check.
- `OxydNtHolyDoorPublic` has minimum None, but `OnBeforeOpened` still demands an active cruciform or held Tau cross. A faithless user is cancelled even on that public prototype.
- `OnGetAccessTags` independently grants profile/module access without consulting clearance. Clearance-based holy doors and access-tag-based doors can therefore disagree after Omission; decide explicitly which mechanism each door uses.
- **Check:** normal granted Preacher/Inquisitor, freshly promoted Preacher, unimplanted public-door user, and a bearer before/after Omission. Test opening the actual entity, not just the clearance field.

### F08 — P2: Scrying freezes the view at the target's starting position

- **Both.** `ScryingSystem.cs:104–117`.
- The marker is spawned at `Transform(target).Coordinates`, which normally parents it to the target's grid/map, not the target entity. Neither the session component nor Update tracks the target or moves the marker.
- The thirty-second view remains at the cast location while the disciple walks away. Eris's god eye follows its target.
- **Smallest correction:** follow/parent the marker to the selected target and clean up if that target is deleted. Decide whether dead or inactive bearers remain eligible: the current active-only enumeration excludes them before the scrying handler runs.

### F09 — P2: rank modules and profile arithmetic do not match Eris

- **Both.** `CruciformSystem.cs:567–575,599–667`; `profiles.yml`; `modules.yml`.
- Source Preacher gets Red Light; source Inquisitor removes it. The port rank table does the reverse: Preacher lacks Red Light, Inquisitor gets it.
- Inquisitor capacity is also encoded twice: profile capacity 100, then module multiplier 2, then Red Light multiplier 1.6 → **320**, not source 100. Its regen deltas likewise compound the profile's already-modified baseline. Tests largely use `TrySetProfile`, bypassing the module table used by real grants.
- Confirmation uses the same whole-rank swap. Confirming a Preacher/Inquisitor as a specialization removes their clergy/inquisitor modules; Eris `make_acolyte/make_agrolyte/make_custodian` only swaps specialization modules and preserves clergy rank. Rank, specialization and clearance must remain independent where the source makes them independent.
- **Smallest correction:** express source rank modifiers once, and test the exact real-grant numeric results. Preserve data-driven profiles, but do not duplicate the same rank effect in both profile and module data.

### F10 — P2: regeneration's source inputs are disconnected

- **Both.** `rules.yml:4`; `NeoTheologyHoliness.cs`; `CruciformSystem.cs:599–690`; `Components/CruciformComponent.cs:61–64`.
- The configured baseline is 1/minute versus Eris's declared `20/(1 MINUTES)`. This is an explicit local balance choice, not source parity.
- `RighteousLife` and `Channeling` are read, but the gameplay systems do not write them. Group completion never adds the source +25 righteous life; alcohol/drugs do not subtract it; the Preacher job's Channeling perk is not connected.
- `canChannel` is used in regeneration calculation, **not** to authorize speech casts. The prior playtest's suggestion that Disciple speech failed because `canChannel: false` is not supported by the code.
- Cog/follower-dependent regen is cached at profile recomputation, not refreshed when those inputs change.

### F11 — P2: Uproot can refund an entity already queued for deletion

- **Both; source-confirmed path, simultaneous-player reproduction still needed.** `NeoTheologyConstructionSystem.cs:114–138,263–283,316–330`; Robust `EntityManager.QueueDeleteEntity`.
- Uproot identifies constructs by prototype, refunds them and calls `QueueDel`. Neither its lookup nor its blueprint match rejects queued entities. Queueing is separate from actual deletion.
- Two successful applications in the same update can both refund the same construct before deletion is processed. Non-stack recipe items queued by Manifestation have the same reuse exposure.
- **Smallest correction:** reject deleted/terminating/queued inputs and remove/reserve the construct before making its refund available. Add one same-tick two-caster regression.

### F12 — P2: construction data is partly decorative; some machines cannot be constructed

- **Both.** `NeoTheologyConstructionSystem.cs`; `litanies_clergy.yml:348–379`; `blueprints.yml`; `Prototypes/NeoTheologyBlueprintPrototype.cs`.
- `BuildTime` is never used by the cast/construction path; all blueprint choices inherit the litany's fixed delay rather than the individual source build times.
- Construction compares each candidate's local coordinates directly to the actor's tile without converting coordinate spaces. Items with a different parent can fail or match the wrong tile. The local targeting fix uses `WithEntityId` elsewhere, but construction still does not.
- The armaments printer is a required separate machine in this adaptation but has **no blueprint**, job kit or map placement. Building the Eye alone does not provide Order Armaments.
- Public/clergy holy-door prototypes exist locally but only the common door has a blueprint. Source biomatter canisters, solidifier and bioprinter are not represented by equivalent functional flows; the forge takes the bioprinter recipe but is not a bioprinter.

### F13 — P2: biogenerator can keep producing after its fuel debit fails

- **Both.** `Machines/BiogeneratorSystem.cs:54–69`.
- With a positive balance smaller than accumulated `owed`, `TryChangeMaterialAmount` fails. The accumulator is not reduced and output is still enabled. On later updates the larger owed amount continues to fail while that small positive balance remains, yielding ongoing unpaid output.
- **Check:** set a low positive fuel balance and advance with a frame time that owes more fuel than is available. Output must stop or be proportionally fuel-limited, never run indefinitely.

### F14 — P2: two distinct Eris upgrade mechanisms were conflated

- **Both.** `Effects/LitanyAsacrisEffect.cs`; `NeoTheologyFoundationSystem.cs:153–173`; `CruciformUpgradeSystem.cs`; source `upgrades.dm`, `rituals/priest.dm:unupgrade`.
- Eris has `coreimplant_upgrade` items (ascension/activatable modules) **and** a separate `cruciform_upgrade` physical attachment slot. Asacris strips the former list. The port loops the single physical attachment uninstall instead; ascension is a free automatic module install inside Initiation, with no ascension-kit item.
- Removing rank modules would also be wrong. Preserve the source distinction, or explicitly document that this adaptation replaces both systems. The current class summary says physical upgrades remain while the handler actually removes them.
- Cleansing Presence also substitutes puddle/weed cleanup for source maint-shroom/vine effects, and Martyr's damageable-entity query is wider than source living victims: verify terrain/items are not burned by its burst.

### F15 — P2: Divine Blessing cannot use the ordinary book flow

- **Both.** `LitanySystem.cs:180–187`; `LitanySystem.Casting.cs:209–219`; `LitanyEffectSystem.cs:560–574`; `LitanyDivineBlessingEffect.cs:24–67`; `entities.yml:39–43`.
- Book initiation and commit require the Bible in the active hand. Blessing requires an oddity in that same active hand. With the ordinary Bible prototype, both conditions cannot be true.
- The existing test uses manual speech and attaches Oddity to a Crowbar; it does not test the book path or acquisition of real NT oddities.
- **Correction:** define an explicit supported item-target/book workflow or present this as speech-only rather than offering a guaranteed-failing book cast.

### F16 — P2: the green focused-test summary hides a failing fixture

- **Executed on the local head.** Full focused run: **234 passed, 2 skipped, 0 failed**, 236 total. Both skips said `Test was dirty-disposed`.
- Running the skipped tests separately gave **1 passed, 1 failed**. `Epiphany_ActivatesInstalledCruciform_RejectsNoCruciformActiveAndDead` failed at `LitanyEffectsFaithTest.cs:158` with `oxyd-litany-denied-entitlement`.
- Its fixture calls `TrySetProfile(Preacher)` rather than `MakeRank`/`GrantCruciform`. Preacher's profile only lists `OxydLitanyPriest`; Epiphany comes from the Acolyte module supplied by a real grant. This exposes fixture/path inconsistency, not proof that a normally granted Preacher cannot baptize.
- **Correction:** exercise real profile grants in end-to-end tests. Keep any low-level profile-only tests explicitly separate. Do not count dirty-disposed skips as passing functionality.

### F17 — P3: UI snapshots and documentation are stale between casts

- **Both.** `LitanySystem.UI.cs:267–372`; `Content.Client/_Oxyd/NeoTheology/UI/LitanyWindow.xaml.cs:572–595`; progress/dependency documents.
- Viewer entries expose entitlement/availability but not current cooldown. Holiness/regen snapshots are sent on UI open and cast-related refreshes, not periodically. The client enables Begin from its last stored holiness, so after resource regeneration it can remain disabled until a refresh/reopen; cooldown expiry is not presented as a live server state.
- The dependency document says there are no test spawners, contradicting the published `test_roles.yml`. Progress claims missing atheist mutation, fixed-duration ceremony skill effects, five-minute timeout, missing clone-rank damage and unfaced doors; several have implementations or were changed by the local follow-ups.
- There are **8 multi-phrase ceremonies**, plus **3 ordinary Crusader rites** — not eleven multi-phrase ceremonies. The old “eleven ceremonies” label is misleading.

## Full-port gaps beyond named litanies

These are missing or materially adapted features, not automatically reasons to recreate every DM subsystem verbatim. Decide explicitly which belong in the intended port; otherwise “full” has no stable acceptance target.

| Domain | Source behavior / outstanding gap | Port evidence / smallest next step |
| --- | --- | --- |
| Church jobs and department | Source Preacher, Acolyte, Agrolyte and Custodian jobs; Church supervision, slots, outfits, wages/account permissions, stats, perks, Latin and accesses. Inquisitor has its own deployment path. | Only existing Chaplain maps to Preacher. Six admin-spawned test ghost roles are not station jobs. Add the real round-start/deployment paths and Bible kit before claiming normal playability. |
| Church map/bootstrap | Altar, reader, pod, biomass supply, forge, Eye, obelisks, holy doors and equipment need an obtainable starting loop. | No NeoTheology prototype placement was found in the checked maps. Construction cannot bootstrap every adapted machine. Supply a playable mapped/loadout path, not more test-only markers. |
| Soul continuity | Latest name/appearance, DNA/fingerprints, languages, stats/perks, mutations, mind identity; body growth followed by soul transfer; safe resurrection restrictions. | F01/F02 block the normal loop. Saved profile preserves some appearance but not the complete source identity/stat/language state. Local clone damage exemption exists; do not repeat the stale claim that it is wholly absent. |
| Cruciform lifecycle | Automatic cyber/mutation cleansing, implant resistance exceptions, hard-ejection injury, sanity perks, visible implant overlays/name/examination and faithful/lost registry semantics. | Explicit Rejection exists, but automatic purity and mutation cleanse, source sanity perks, hard-eject consequences and full presentation are absent/adapted. Extract/reimplant currently auto-activates any previously activated implant on a living bearer rather than waiting for the stored-soul rite. |
| Holiness | Source regeneration, righteous life, Channeling, rank modifiers and dynamic stat inputs. | F09/F10. These are gameplay economy inputs, not just numeric helpers. |
| Targeting and speech | Grabbed-victim preference; front-human rays; named/global follower targeting; prompts on spoken litanies; source cooldown semantics. | Named speech/facing improved in unpublished commits. Grab preference is absent, Revelation's 4 m ray becomes own/front-tile selection, global followers become same-station active followers. Manual Manifestation/Divine Guidance require the book; Confirmation silently defaults Acolyte; Sending uses a prepared broadcast rather than a target/text prompt. `cooldown = FALSE` in Eris also makes several apparently listed source cooldowns ineffective; the port deliberately enforces them. |
| Medical | Holy medicine physiology, hallucination/pain/addiction timing, biological rejection, foreign object/cyber removal. | Native chemistry/pain/addiction adapters are reasonable, but exact metabolism/timing and implants' source permanent malfunction/embedded-object handling are not equivalent. Test real critical patients and natural/robotic organ combinations; do not generalize from injected fixtures. |
| Eye economy / offerings | Faithful/global power, observation changes from conversion/deactivation/purging, faithless classification, offering-selected next reward, six meaningful reward types. | Offerings only bank 1000/500 observation instead of adding 5 power and selecting the next reward family. Holy Guidance omits the oddity. Oddity reward is empty; ALERT loses antagonist-location behavior; INSPIRATION is local insight; ENERGY fills current holiness rather than increasing regeneration. Rewards/power count nearby followers rather than the source global roster. Status-only Eye Blessing has no mechanical benefit. |
| Obelisk | Faithful sanity perk, direct power restore, shortened personal cooldowns, hostile kills awarding observation, mutant/carrion/artifact/burrow/maint-shroom interactions, active power load. | Local regen multiplier/sanity delta, fauna damage and weed suppression exist. Source cooldown acceleration, kill rewards, mutant/carrion and faction-artifact/burrow behavior are absent. Visibility work is useful but remains a fork-wide regression/performance surface. |
| Biomatter loop | Multipart reactor/generator, reagent biomatter canisters/tanks/pumps, corpse/animal processing, toxic biomass, solidifier and bioprinter products. | Single-entity reactor processes Produce, generator burns material stacks, tank is separate MaterialStorage without the source reagent plumbing. F12/F13. Flattening is an acceptable intentional choice only if the whole supply/use loop remains functional. |
| Armory / equipment | Eris NT disk recipes, single-use licensing, discount schedule, faction weapons/armor and first-purchase bank growth. | Design-disk adaptation exists, but equipment substitutes and machine reachability still need verification. Printer UI is directly activatable; Order Armaments is not the only opening path. Local litany routes from Eye to a separate nearest printer whose own UI/range must still be usable. |
| Upgrade acquisition | Ascension kit/core upgrades versus six physical attachments, fabrication/material costs and install/uninstall/death cleanup. | F14. Test native acquisition, not admin-spawn-only installs. Source cleansers and burst victims differ; time-dependent effects must remain tied to live implant ownership. |
| Inquisitor uplink | Hidden 15-TC store, source NT products and prices, category authority, kit promotion and balance persistence. | Uses a nullspace native store and substitutes; sound reuse of the existing store, not source equipment parity. Test UI access revocation on death/deactivation/mind transfer, not only extraction/module uninstall. |
| Ceremonies / objectives | Success signals for all participants, +25 righteous life, church conversion/revelation/sanctification objectives, scores, Crusade faction-item activation. | Eight phrase rounds exist. Sanctify only forces obelisks; it does not signal a sanctified area. Crusade grants rites but does not activate faction items. Church objectives/scores lack consumers. Completed targets are filtered only for alive status, not final range/active implant; local expiration no longer times out and tests still need corpse/control/implant replacement cases. |
| Crusader HUD | Source powered module drains holiness and removes itself when depleted. | Body status-icon HUD costs nothing and is removed on extraction; check death/revival and remote PVS/implant state in a real client. |
| Special items / presentation | Last Shelter, Sword of Truth destruction/objective/crusade behavior, holy weapon behaviors, NT sprites/audio/overlays, lore/manuals. | Recipes explicitly omit Last Shelter, machine sprites are borrowed, and source faction-objective/world-item mechanics are not part of this PR. Sprite existence alone is not a behavioral port. Art/audio parity was not visually certified in this review. |

Source anchors: `code/game/jobs/job/church.dm`, `code/datums/objective/individual_objective/church.dm`, `code/datums/uplink/neotheology.dm`, `code/modules/core_implant/{ritual,group_ritual}.dm`, `code/modules/core_implant/cruciform/{cruciform,modules,upgrades}.dm`, its `rituals/` and `machinery/` files, and `code/modules/biomatter_manipulation/`.

**Not a missing active litany:** Obey is commented out in the source ritual file. Its upgrade/module support exists elsewhere in Eris, so it is an optional associated-feature decision, not a sixty-first active phrase that this port forgot.

## Validation and limits

Commands run from the canonical Oxyd14 checkout:

```sh
dotnet test Content.Tests/Content.Tests.csproj --no-restore \
  --filter 'FullyQualifiedName~NeoTheology|FullyQualifiedName~SharedSkillSystemTests'
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore \
  --filter 'FullyQualifiedName~NeoTheology'
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-build --no-restore \
  --filter 'FullyQualifiedName~Epiphany_ActivatesInstalledCruciform_RejectsNoCruciformActiveAndDead|FullyQualifiedName~Entreaty_PreacherInquisitorAlways_Others50pct_EscapedNameLocation'
dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj --no-restore
git diff --check upstream/master...origin/eris-litany-port
```

- Unit checks: **21 passed**, no failures/skips. One test explicitly preserves `SetUniqueBuff`'s old amount/expiry when a different amount is requested. This conflicts with the shared litany helper's general “apply or refresh” description. All its callers were checked: fixed-strength short blessings and Eye buffs normally request the same amount, while Call to Battle has variable strength but a cooldown as long as its buff. Thus the different-strength case is a helper-contract/edge-case concern, not an established normal-play recast exploit. Clarify the contract before relying on overlapping variable-strength recasts.
- Integration checks: **234 passed, 2 skipped**; isolated recheck **1 passed, 1 failed**, as detailed in F16.
- Test builds compiled the local shared/server/client projects successfully, with warnings. They validate the five unpublished litany commits too, **not an untouched build of the published PR SHA**. Whole-repo unrelated test suites were not run.
- YAML linter failed in the existing `erisPorted/mask/fake_moustache.yml` plain-scalar parse. Test startup also logged existing Vapour mask, AttachmentOxydScope and several RSI errors. These files are outside the published litany diff; do not attribute them to this PR without a separate baseline comparison.
- Published diff whitespace check found an extra EOF blank line in `NeoTheologyProfilePrototype.cs`. Working-document whitespace check was clean before adding this report.
- GitHub reports several failed checks, but the available failed build log inspected is historical (`a45e194384`): checkout/submodule setup failed on the accidentally tracked `Oxyd14-port-eris-ironhammer` gitlink. That gitlink is absent at the current PR/local heads. Historical red checks are not evidence that the current litany C# fails compilation, nor are they a substitute for rerunning CI.
- Existing tests manually grant components, bypass player checks with `TestingTreatAsActor`, install soul modules, and/or call profile-only APIs. Those tests are useful unit/integration coverage but cannot establish real job → item → target → effect reachability.
- The prior manual playtest remains partial. No live two-client round was run during this review.

## Branch/worktree consolidation performed

Canonical working branch: **`eris-litany-port` at `7df3c31239`**, tracking `origin/eris-litany-port`, ahead by **five**. Only one registered worktree remains. `master` was fast-forwarded locally to `97c050d91a` and now tracks `upstream/master`.

Backups: **`/Users/russellrozario/Desktop/SS13-14/oxyd14-review-backup-20260930-141555/`**.

- `oxyd14-all-refs.bundle`: all original content refs/history, verified as a complete bundle.
- `robust-all-refs.bundle`: original RobustToolbox refs/history.
- Original main document edits saved as a binary patch; untracked playtest report copied separately. Both original files remain in the canonical checkout, unchanged by this review.
- Dirty floor worktrees **moved intact**, not deleted, into `retired-worktrees/`; per-worktree status/patch files also saved. Their only reported tracked edits were deleted Mac app bundle files. Their registered records were pruned after archiving.
- Local-only `8a04c267be` ricochet work removed from the litany tip with `reset --keep` after backing up. Its content and engine commit remain recoverable from the bundles. RobustToolbox restored to the litany-recorded commit, detached; no engine changes discarded.
- Deleted contained local branch names: `neotheology/pr33-completion`, `neotheology/pr33-upstream`, `pr33-litany-cleanup`, `port-eris-ironhammer`, `eris-flooring`, `port-eris-carpets`, `port-eris-floors-all`, `port-eris-floors-mapper`, `port-eris-floors-retargets`, `port-eris-lattice`, `visual-eris-floors`.
- Archived/deleted divergent obsolete names: `eris-litany-port-pr` (closed PR #34; ten equivalent patches and an older superseded foundation patch), `neotheology/fix-pass` (upstream merge plus obsolete worktree-gitlink removal). No historical scaffold was blindly merged over the newer implementation.
- GitHub confirms IronHammer PR #32 and combined floors PR #31 are merged. All retained floor tips were ancestors of the canonical litany head; no unique floor commits needed importing.
- **No push, force-push, remote branch deletion, PR edit, or new commit was made.** Remote branches are untouched. The published PR still ends at `e055ca4267`; the five local litany commits are not yet reviewed/merged on GitHub just because they exist locally.

Recovery example (choose the branch you actually need):

```sh
git fetch /Users/russellrozario/Desktop/SS13-14/oxyd14-review-backup-20260930-141555/oxyd14-all-refs.bundle \
  refs/heads/neotheology/fix-pass:refs/heads/recovered-fix-pass
# Recover the unrelated local commit without putting it back on the litany PR:
git branch recovered-ricochet 8a04c267be
```

## Recommended completion order

1. **Repair the normal gameplay loop:** real profile grant → cloning module/soul → conversion → death/extraction → vessel growth → reincarnation. Test through actual jobs/roles and obtainable machines.
2. **Fix central cast correctness:** target revalidation, caster-anchored scans, clearance/specialization authority, construction single-consumption, followed scrying view, paid generator output.
3. **Reconcile source economy once:** rank modules/capacity, regeneration inputs, Eye offering/reward semantics, upgrade categories. Record intentional SS14 differences instead of calling them complete parity.
4. **Wire normal-world availability:** Church jobs/loadouts/maps, construction access to the separate printer and needed machines, native NT equipment/store acquisition.
5. **Finish world hooks:** Church objectives/signals, Sanctify area state, Crusade faction effects, faithful lifecycle and special items within the agreed full-port scope.
6. **Correct fixtures/docs and validate:** no hidden dirty-disposed skips; fresh current-head CI; two-client playtests for targeting/privacy, cleanup, soul continuity and group rites. Only then publish the reviewed five local follow-ups and the fixes to the single PR branch.
