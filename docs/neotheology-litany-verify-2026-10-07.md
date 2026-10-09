
## Addendum — two-client pass (2026-10-07, commit fbe07ac61f)

Two live clients (PriestLeader + FollowerTwo) used to close remaining matrix rows.

### Newly verified
- **Commitment `success=True`** — full altar posture: `litany:face` + `litany:machine OxydNtAltar` + loose `OxydNtCruciform` on the altar + `litany:buckle` an undressed NPC → cast committed and installed the cruciform.
- **Choice window interactive flow** — `litany:cast OxydLitanyConfirmation` (AdjacentFollower designation) `success=True`; LitanyWindow opened in "Choosing: Confirmation" state with live countdown (~15s) and readable Choices pane inside the ScrollContainer. No crash — the RegisterControl fix holds.
- **Reincarnation target resolution on corpses** — dead adjacent NPC now reaches the effect and correctly reports `oxyd-litany-soul-lost` (no stored soul) instead of `no-target`.

### Port bugs fixed this pass (commit fbe07ac61f)
- **Buckled mobs invisible to litany targeting** — `SharedBuckleSystem` drops buckled mobs from the physics lookup; `ResolveAdjacentMobs` now also enumerates `StrapComponent.BuckledEntities`. Without this, Commitment/Uproot could never resolve an altar-bound candidate.
- **Reincarnation could never fire** — `LitanyReincarnationEffect` lacked `AllowsDeadTarget`; dead targets were filtered before the soul check ran.
- **`litany:kill` crash** — `entities named` also matches organ entities (same name, no MobStateComponent); mob gate added.
- **YAML boot errors** — unquoted `Warning:` descriptions in `fake_moustache.yml` / `Vapour_mask.yml` broke prototype parsing.

### Tooling notes (debug-command quirks, not port bugs)
- `litany:buckle`/`setup` teleports a mob to any altar seat within 5m; prefer `litany:machine OxydNtAltar` on the priest's front tile for deterministic posture.
- TryBuckle fails against a strap spawned at the mob's exact coordinates — always use the front-tile machine altar.
- `litany:buckleprobe` TryBuckles any strap (chairs/beds) — diagnostic only.

### Still open
- `AttachmentOxydScope` proto `NullNotAllowedException` at server boot (YAML needs fixing).
- Interactive choice-submit clicks (Choices populate after Begin within the countdown window) — partially verified; auto-expiry closes the window cleanly.
- Resurrection positive path (soulreader → cloner → biomass → mind transfer) on this build — denied-gate path verified only.
- DivineIntervention offering (200 OxydNtBiomatter near Eye) untested.

## Pass 4 — remaining-open items (10-08)

- **AttachmentOxydScope boot null** — RESOLVED. Zero proto errors at boot since the `newGuns.yml` Heat-indent fix (`417b55c166`); confirmed across two fresh boots.
- **Resurrection positive path — VERIFIED end-to-end.** Soul snapshotted to cruciform (soulreader), corpse deleted, clone grown in pod (material 200→129), ejected to diagonal tile, `litany:implant` installed soul cruciform, Reincarnation (AdjacentLiving, diagonal front tile works) → `success=True` → "Session localhost@FollowerTwo transferred to entity 5349" → client log "Attaching local player to Tanner Mull (MobHumanOxyd)". Gotchas: resurrection reader must be within 1.5m of the cloner AND an *empty* reader on the front tile shadows the loaded one (targets list takes precedence).
- **DivineIntervention 200-biomatter offering — VERIFIED.** Cast `success=True` vs eye; ~200 biomatter consumed from loose stacks within 7f (42→22 stacks).
- **Click-through choice-submit — PARTIALLY FIXED.** Root cause #1 (server): `SendChoiceSnapshot`/`SendProgressToActor` only addressed `FindActorBook` and early-returned with no book held — speech casts never got the snapshot. Fixed: route to `cast.Prompt` first (commit `087c9d0f7a`). Verified live via probe: snapshot now sent to prompt BUI with 18 blueprint options. Root cause #2 (client, suspected): message still not applied — relaxed the stale-revision drop to same-request-only. **Still open**: snapshot appears dropped in client BUI routing/dispatch before reaching `UpdateChoiceSnapshot`; needs a client-side probe pass.
- New debug tooling: `litany:implant` (eject cruciform from nearest reader ≤10m → ForceImplant).

## Pass 5 — 2026-10-07 (server-side verification, autopause root cause)

**Root cause for the "stuck chant" reports:** `game.auto_pause_empty` (default True) freezes `GameTiming.CurTime` with 0 connected players. doAfters and `LitanySystem.Update` never ticked — every cast parked at `stage=Chanting elapsed=0.00`. Not a port bug. Disabled via `cvar game.auto_pause_empty false`; the pending Relief cast immediately completed and debited holiness.

**Verified live this pass (all server-driven via `litany:` commands, no client):**
- **Initiation positive path** — Inquisitor-module caster → AdjacentFollower auto-resolve → chant → 5417 promoted `OxydNtDisciple`→`OxydNtPreacher`, clearance→Clergy, cap 50→80, gained Priest/Acolyte/RedLight modules + sets. Caster debited 100.
- **Crusade ceremony quorum** — Preacher leader + 6 bearer followers, full 9-phrase ritual driven by `litany:step` → all participants granted `OxydLitanyCrusader` (verified via `litany:status` `granted=`/`sets=`).
- **Sanctify ceremony** — completed; marks a 7-tile-radius `NeoTheologySanctifiedAreaComponent` on the grid and pushes `ForceActiveUntil` on all obelisks; Sanctify-kind objectives inside the area flip Completed (no live objective entity existed to observe the flip — mechanism verified).
- **HolyGuidance offering** — Eye on front tile + 1 oddity + 40 real produce items in range → `success=True`, −15 holiness, items consumed. (Produce isn't StackComponent — `litany:stack x40` leaves count 1.)
- **Set grant** — confirmed via Crusade grant + `litany:grant`.
- **Choice-submit client drop — FIXED**: `SendViewerSnapshot` now re-sends progress + choice snapshot alongside every viewer update (the prompt BUI replicates a tick after the open message; the first send dropped silently). Committed `61314929a1`.

**Environment findings (not port bugs):** autopause freeze above; `litany:place` at a disconnected session entity's coords puts mobs in a no-atmos spot → suffocation after sim resumes (anchor with `tpto <fixture> <mob>`); dead bearers present as `cannot-speak`/`no-implant`.

**Still open:** Scrying caster-privacy (needs a connected client to observe), obelisk aura regen vs. baseline delta measurement, EOTP purchase flow end-to-end.

## Pass 6 — 2026-10-09 (two-client verification: scrying privacy, obelisk delta, EOTP purchase)

All three remaining open items from Pass 5 verified live. Two clients on one server: A = Dante Pavlov (5141, inquisitor cruciform) and B = Maximilian Smail (5197).

- **Scrying remote view — VERIFIED.** `litany:cast OxydLitanyScrying "Human(Oxyd)"` committed `success=True`; caster's eye retargeted ~20 tiles to the target's room (paired screenshots: A's view rendered the same dark NPC room B occupies; B is visible inside it). Session expired ~30s; eye restored to caster.
- **Scrying privacy — VERIFIED.** Client B standing inside the scried room saw no marker entity, no HUD indicator, and no chat notification while A was remotely viewing the same room. The scrying marker is an invisible null-proto entity used only as an eye anchor — nothing is replicated to bystanders. No gap.
- **Obelisk aura regen delta — VERIFIED.** `litany:status` baseline `regen=0.417/s` → `litany:machine OxydNtObelisk` on the front tile → `regen=0.833/s` (2× `RegenMultiplier`, applied while in view) → obelisk deleted → back to `0.417/s`. Both application and removal proven.
- **EOTP purchase E2E — VERIFIED after fixing one real port bug.** Path: `litany:machine OxydNtEyeOfTheProtector` + `litany:machine OxydNtArmamentsPrinter` → `litany:armpoints 150` → `litany:cruciform "OxydNtInquisitor"` → `litany:openui <printer>` → select "ritual book design disk — 100" → Print.
  - **Bug found + fixed:** the printer's `ArmamentsPrinterState` pushed once on `AfterActivatableUIOpenEvent` and was dropped before the client window existed — the window rendered permanently empty on every open (deterministic, two opens both empty). Same race the other NT windows already had; fixed with the same 1s `IsUiOpen` re-push loop in `ArmamentsPrinterSystem.Update`. Window now populates: "Armament points: 150 / 150", cost-sorted list.
  - **Purchase verified:** click-select (description row appears) → Print → points 150→50 (cost 100 spent), first-purchase `MaxArmamentsPoints` 150→175 (+25 `MaxPointsIncrease`), per-armament discount repriced cost 100→75, "Not enough armament points" affordability feedback, `OxydNtRitualBookDesignDisk` (5285) spawned at the printer's coords (31.5,18.5).
- **New debug tool:** `litany:armpoints [amount]` — adds armament points to the first Eye (clamped to max); needed because points accrue only via released miracles otherwise.

**Environment findings (not port bugs):**
- Full Release build is red tree-wide on ~180 analyzer escalations (`RA0051` readonly `[Dependency]` fields, `RA0049` non-partial dep class, `RA0030`, `CS0414`) — pre-existing across `_Oxyd`; earlier "green" builds were incremental. Local testing builds pass with `-p:TreatWarningsAsErrors=false`. Worth a dedicated cleanup pass.
- Client launch needs `ALSOFT_DRIVERS=null` (set in the desktop .bat files); without it OpenAL collides audio-source key 0 and crashes on startup.
- `litany:cruciform` requires the profile quoted (`"OxydNtInquisitor"`); bare proto ids fail toolshed parsing as "Failed to execute toolshed command".
- Building while the server runs fails on dll file locks — `taskkill` the server first, relaunch after.

**Remaining gaps:** none for these three items. Broader port coverage per the session evaluation (~90%): EOTP's Eris disk-purchase list is intentionally not ported (gear routes via printer/uplink); art licensing unresolved.

## Pass 8 — Phase 2: Holiness HUD indicator (commit d99a7af564)

- IC stat category now carries a `Holiness` spec row + displayBar next to Focus
  Insight / Emote Menu / Character Info buttons. Verified live: bar reads
  `80 / 80`; `litany:holiness 40` flipped it to `40 / 80` and regen walked it
  back (46/80 observed mid-tick, then full). Row only exists while the local
  player has a linked cruciform; hides when the link is gone.
- Framework lesson: stat-panel content must be re-pushed via
  `StatusPanelSystem.refreshContent(key)` after `AddToPanel`/`tryAdd` — the
  helpers never refresh, and `StatContent` is only repopulated on tab-select.
  Also, components arriving during initial entity sync fire `ComponentStartup`
  BEFORE `LocalPlayerAttachedEvent`, so a lazy Update-time ensure is required
  for join-with-cruciform.

## Pass 9 — Phase 3: VFX/audio polish (2026-10-09, live)

Effects are RT-native spawnable protos (`effects.yml`): `OxydNtCastGlow`
(PointLight r2.5/e1.6 #e9c183, 4s, BibleHeal chime) spawned at the actor on cast
commit and per-participant on ceremony completion; `OxydNtEpiphanyFlash`
(PointLight r5/e3 #fff2c8, 6s, ChurchBell) spawned on `CruciformSystem.Activate`
first activation only (`!EverActivated` — debug `litany:cruciform` grants set it
directly and correctly do NOT flash); `OxydNtObelisk` gained PointLight r3.5/e0.9
#d9b26a + `LitOnPowered` idle aura.

**Verified live:**
- Commitment → Epiphany flow end-to-end (twice, two fresh rounds): mob buckled
  to altar + undressed → `cast OxydLitanyCommitment` success → dormant implant
  (`active=False everActivated=False`) → `cast OxydLitanyEpiphany` success →
  cruciform `active=True everActivated=True`, holiness 50/50, base+cloning
  modules, 3 litany sets. First-activation flash path fires.
- Epiphany flash renders: warm gold light visible around the altar-bound mob.
- Obelisk aura renders: warm halo around spawned obelisk.
- Cast glow: same proto pattern + verified spawn path; fires at commit.
- Sounds attach via EmitSoundOnSpawn (server accepted; audio not verifiable
  headless — ALSOFT null driver).

**AdjacentLiving target rules (confirmed by probing):** target must be on the
actor's own floored tile or the cardinal tile the actor faces (`IsOnTile` —
no diagonal); buckled mobs are found via `StrapComponent.BuckledEntities`
(separate from physics lookup); `litany:place` spawns at the NEAREST altar in
5m, not the piped mob — use it only when placement doesn't matter.

**Environment lessons:** `litany:machine`/`MakeOperational` REMOVES
ApcPowerReceiver — `LitOnPowered` then never fires but light stays at its
default `Enabled=true` (spawned obelisk glows). `tpto`/`tp`/`spawn` toolshed
commands do not exist here — they silently no-op. `buckleprobe`'s TryBuckle
per seat BUCKLES unbuckled mobs to the first free seat (mutating probe).
Client occasionally wedges in an NRE loop in `EntAddComponent` during join
entity-sync after a server restart — relaunching the client fixes it.

## Pass 10 — Phase 5 sweep (srvin11/12, 2026-10-08)

**Sanctify objective flip — VERIFIED E2E.** Granted `OxydNtSanctifyObjective` via `litany:objective "sanctify"` (pinned grid=2 tile=(31,18)). Ran full 7-phrase ceremony: leader + 1 follower repeating each round (`correct=[5197]`→`participants=[5197]`). Final phrase → `CompleteCeremony` → tiles marked → `objective 5196 kind=Sanctify completed=True`.

**Ceremony follower gate — mechanism documented (not a bug).** `OnSpeechAccepted` requires `IsPlayerActor(speaker)` — ActorComponent (real session) or `LitanyTestingActorComponent`. Mindless MobHuman speech is ignored → followers MUST be `litany:actor`-marked for headless tests. Eris parity (real bearers only). Earlier "ceremony vanished" = chant doAfter: `ActiveCeremonyComponent` is created at chant completion (~2s), not at cast commit.

**Obelisk — all five branches verified:**
- Hostile-damage: `MobAngryBee` (SimpleHostile) destroyed in <8s in aura with no other damage source. NOTE: `MobGorilla` is NOT hostile (SimpleNeutral only) — wrong test proto.
- Weed-removal: tray `weeds=8/10` → `0/10` in aura.
- Regen: `regen 0.333→0.667/s` (2× RegenMultiplier) while in view.
- Sanity: `insight 0→20` (SanityPerSecond accrual; sanity capped at 100).
- Cooldown-reduction: `cds=[OxydLitanyRelief]` drains ~2× faster in aura vs baseline (obelisk deleted control): -5s sim/~15s wall vs ~-1-2s baseline.

**Tool notes:** `litany:cooldowns` CLEARS cooldowns (mutating, not a read). `litany:probe` throws on server console. `litany:status` now prints vitals (state/dmg/sanity/insight), `cds=[key=remain]`, tray weeds, eye observation/armament. `litany:weed`/`hostile` piped variants; `hydroponicsTray` spawns pre-anchored (skip re-anchor). Server sim-time runs ~0.4× wall-clock on this VM — all time-based assertions must compare against sim rates, not wall time.

**Still open:** adjacent-living reagent litanies (WordsOfPurging/HandOfMercy), upgrades x7, 13 modules, edge cases (death mid-cast, disconnect mid-chant, dual-caster, scrying-during-death).

## Pass 11 — Phase 5 sweep part 2 (srvin14/server3, 2026-10-09)

**Modules** — all 11 core module protos install via `litany:module` (7 newly installed, Cloning correctly idempotent-false on repeat). Set-grant verified: Agrolyte/Custodian modules grant `OxydLitanyAgrolyte`/`OxydLitanyCustodian` sets (visible in `litany:status` sets list).

**Upgrades — all 7 verified E2E** (`litany:upgrade` now branches on component type): `CruciformUpgrade` items (NaturesBlessing, FaithsShield, CleansingPresence, MartyrGift, WrathOfGod, SpeedOfTheChosen) each install into the single upgrade slot — second install correctly refused while occupied (`installed=False`), `litany:upgrade "clear"` uninstalls, swap succeeds. `CruciformCoreUpgrade` (PreacherAscensionKit) takes the core-upgrade path → `coreInstalled=True` → registers `OxydNtModulePriestConvert` in modules. Status shows `upgrades=N upgrade=<uid>`.

**Reagent litanies — all 3 verified** (caster 5266 Agrolyte+Custodian, target 5341 adjacent): HandOfMercy → `OxydNtDeusBlessing=~15u`; AbsolutionOfWounds → `OxydNtHolyInaprovaline=~10 + OxydNtHolyDexalin=~10`; WordsOfPurging → success=True (beneficial reagents untouched — purges toxins only). `litany:reagents` dumps bloodstream+metabolites.

**Edge cases:**
- Death mid-cast: caster damaged to Dead mid-doAfter → cruciform `active=False`, holiness cost debited, no exceptions.
- Entity deletion mid-chant (`delete` during doAfter): no exception, pending cast dies with entity — clean.
- Dual-caster: two cruciform bearers cast ~1s apart → both `success=True` (rate limit is per-mob).
- Scrying during death: dead caster denied `oxyd-litany-denied-no-implant` (deactivated cruciform gates it).

**New deny-reasons catalogued:** `denied-npc` (mindless mob — needs `litany:actor`), `denied-rate-limit` (per-mob cast window — failed casts still consume it), `no-target` (AdjacentLiving requires target on own or FRONT tile — diagonal adjacency fails), `denied-no-implant` (deactivated/absent cruciform).

**Tool lessons:** `litany:upgrade` takes proto ids for BOTH upgrade kinds; bare `clear` unquoted fails parse — quote it. `litany:hostile` spawns at FRONT tile which can land diagonal — `litany:face` the caster first for cardinal adjacency. `ent <anchor> | litany:hostile` needs a positioned entity (map-root EntId=0 entities throw invalid-coords); station airlocks work. `ent X | delete` exists and is silent.
