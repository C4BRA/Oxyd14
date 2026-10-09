
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
