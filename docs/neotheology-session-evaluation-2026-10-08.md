# NeoTheology port — session evaluation (2026-10-08)

Branch `eris-litany-port` (PR AEV-Oxyd/Oxyd14#33). This evaluation covers three questions:
port coverage, functional/test status, and the debug tooling created for live QA.

## 1. Port coverage — what exists vs. what is missing

### Litanies / rites

**60/60 catalog entries** across four proto files (`litanies_common` 12, `litanies_clergy` 30,
`litanies_inquisitor` 11, `litanies_machines` 7). Every active Eris phrase was matched
(`neotheology-litany-review-inventory.md`), each has a `LitanyEffect` implementation
(51 effect classes in `Content.Shared/_Oxyd/NeoTheology/Effects/`), and grants flow through
profile→module→set resolution. All 60 are `enabled: true` — none are stubbed.

### Items / entities

| File | Content |
| --- | --- |
| `entities.yml` (10) | Cruciform implant, Bible + clergy Bible, altar, 3 holy doors, tau cross, holy hand grenade, Eye-blessing status |
| `machines.yml` (10) | Biomatter reclaimer, cruciform forge, biogenerator, bioreactor, cruciform reader, cloner, biomass container, obelisk, armaments printer, Eye of the Protector |
| `equipment.yml` (13) | Robes and NT clothing/armor |
| `armaments.yml` + `armory_expansion.yml` (18) | NT weapons incl. seven named energy guns, ritual blade, Sword of Truth |
| `upgrades.yml` (7) + `modules.yml` (13) | Cruciform physical upgrades and core/rank modules (incl. optional Obey kit) |
| `blueprints.yml` (18) | Divine Guidance constructibles (canisters, bioprinter, solidifier, printer, door variants) |
| `offerings.yml`, `medicine.yml`, `materials.yml` | Eye offerings, 6 NT reagents (AngelsBalm/DeusBlessing/HolyInaprovaline/…), biomatter |
| `robotic_organs.yml` (9), `access.yml` (3) | Church cyber-organs, clergy/public/holy access levels |
| `uplink.yml` (15) | All 13 source NT uplink listings at source TC prices + ascension kit |
| `world_objectives.yml`, `rules.yml`, `profiles.yml`, `test_roles.yml` | Native objective types, 6 NT profiles, Chaplain→Preacher mapping, dev ghost roles |

### UI / UX

- **LitanyWindow** (`Content.Client/_Oxyd/NeoTheology/UI/`): the Bible's BUI and the nullspace
  prompt proxy for spoken casts — catalogue, choices (blueprint/target/designation/text), live
  countdown, scrollable details. Reachable Submit/Cancel.
- **ArmamentsPrinterWindow** and **EyeOfTheProtectorWindow** BUIs.
- **NT uplink store** — hidden Inquisitor store with per-listing bearer/account authority.
- **Speech casting** — chat chants commit casts exactly like Eris; choice rites open the private
  prompt window.
- **Disciple HUD icon** (`OxydNtDiscipleIcon` security-icon marker) via `NtDiscipleHudSystem` —
  the Eris "who is faithful" indicator, granted by EternalBrotherhood.
- **8 multi-phrase ceremonies** persist until leader death/implant loss.

### Deliberate non-ports / open gaps

- **No HUD action button** — there is no scream-style pray button; casting is speech + Bible UI
  (Eris parity; a button would be new UX, not a port gap).
- **No persistent HUD holiness indicator** (no thirst/hunger-style widget); holiness is shown in
  the LitanyWindow only. Eris also surfaces holiness only through its litany panel — parity, but
  a QoL gap if SS14-side indicators are wanted.
- **Machinery is flattened**: single native entities driven by front-tile litanies and default
  interactions — no NanoUI-style machine windows for bioreactor/cloner/reader/forge/obelisk/altar.
  The forge is now a headless lathe (no LatheUI exposed) per maintainer review.
- **Jobs/maps**: no round-start Church jobs or map supply; six dev-only
  `RandomHumanoidSpawnerOxydNt*` ghost markers exist instead (Chaplain is the only real-job
  mapping, and does not auto-receive a Bible).
- **Powered Crusader HUD** (Eris power-cost disciple HUD) absent; local disciple HUD is unpowered.
- **Art/audio**: extracted Eris sprites for Eye/reader/forge/obelisk/upgrades + Seal/Sword/Shelter
  exist, but `license: null` on new RSIs — not publish-ready; animations/in-hands/manuals absent.
- **World hooks**: carrion/blitz/borer threat detection needs their ports to attach
  `NeoTheologyThreatComponent`; objectives are local-balance adaptations.
- Remaining adapted areas are itemized per-area in `neotheology-feature-gap-pass.md` (areas 3–13,15).

## 2. Does it function — tested vs untested

### Automated coverage

- **289/289 NeoTheology integration tests pass**, 0 failures/skips (latest run 10-08).
- 21 NT/skill unit tests pass. Shared/server/client/integration builds: 0 errors.
- YAML linter clean (13→0 errors this session; the historical `fake_moustache.yml` blocker is fixed).
- Engine invariant verified (`verify-engine-master.sh`): zero engine changes.

### Verified live in-game this session (computer use)

- Cast gates: holiness debit (Atonement −45, Excommunication −60, Scrying −100), insufficient-cost
  denial, personal-cooldown denial, entitlement denial (preacher→inquisitor set), soul-lost denial,
  already-preacher denial, no-target denial.
- Epiphany activates dormant implanted cruciforms; cloned cruciforms start dormant.
- Named rites resolve by spoken name (Piaculo, Excommunicatio) — target must be an active follower.
- Excommunication strips clearance → None.
- AdjacentFollower/front-tile targeting semantics (own or front tile; uid-sort shadowing fixed;
  buckled-mob physics gap fixed).
- Commitment: full altar posture (front-tile altar + loose cruciform + buckled undressed NPC)
  installs the cruciform — `success=True`.
- Reincarnation: end-to-end positive path — soulreader snapshot → clone pod growth → implant →
  mind transferred to the clone entity (verified via session transfer log).
- DivineIntervention: 200 biomatter consumed from loose stacks within 7 m of the Eye.
- Choice-rite flow: spoken Choice litanies open the prompt window (no more RegisterControl crash),
  countdown renders, Choices pane scrolls, Cancel works.
- Deprivation on a corpse ejects the cruciform; Rejection purges foreign implants; medicine
  reagents metabolize through the native bloodstream.
- Two-client checks used PriestLeader + FollowerTwo sessions.

### Open / partially verified

- **Choice-submit click-through**: server provably sends the snapshot to the prompt BUI
  (18 blueprint options, probe-verified) but the client still drops it before
  `UpdateChoiceSnapshot` — needs a `Log` probe in `LitanyBoundUserInterface.OnMessageReceived`.
  Everything before Submit works; auto-expiry closes cleanly.
- **Initiation positive path** — needs the ascension-kit item path exercised live.
- **Crusader ceremony quorum** — multi-phrase ceremonies verified in code/tests only; live
  two-leader chant not yet done.
- **HolyGuidance offering** (oddity + 40 produce consumption), **Sanctify objective signal**,
  **Scrying privacy** (message only reaches the scryer between two clients), **obelisk/soul
  regen-energy rewards**, **set grant** — tested indirectly or not at all live.
- **Eris-parity differences are documented adaptations**, not bugs: 1 s chant (BreakOnMove
  can't realistically fire), name/diagonal facing quirks for Revelation, native pulling vs grab
  objects, native metabolism vs Eris reagent timing, flattened machinery economics.

## 3. Debug tooling created for SS14 computer-use QA

### `litany:` toolshed group — `Content.Server/_Oxyd/NeoTheology/LitanyDebugCommand.cs` (~970 lines)

`[ToolshedCommand, AdminCommand(AdminFlags.Debug)]`, ~25 subcommands, all
`IEnumerable<EntityUid>`-piped or self-targeted:

- **Setup/grants**: `cruciform <profile>` (installs profile implant + record — works around the
  possession/MindAdded gap), `activate`, `grant <set>`, `module <module>`, `holiness <n>`,
  `give <proto>`, `place <proto>`, `giveoddity`, `setup <blessing|commitment|machines|offering|full>`.
- **Body/posture**: `undress`, `buckle`/`unbuckle`/`buckleprobe`, `face <deg>`, `step`, `kill`,
  `damage`, `material`, `stack`, `biomass`.
- **Cast/machine**: `cast "<litany>" [name]` (server-validated cast, choice windows included),
  `machine <proto>` (spawn on front tile), `cooldowns`, `probe`, `upgrade`, `soulreader`,
  `implant`, `resdebug`, `miracle`, `say`.

### Fixes the tooling exposed (real defects, not tool bugs)

- **Toolshed pipe bug**: `[PipedArgument] EntityUid` (singular) never matched entity pipes —
  every entity-consuming debug command had to take `IEnumerable<EntityUid>`; the fix is in-repo
  so other toolshed users benefit.
- **`entities named` organ collision**: organ entities share their owner's name —
  `litany:kill`/`entities named` now gate on `MobStateComponent`.
- **Buckled mobs dropped from physics lookup**: surfaced by `litany:buckle` + Commitment;
  `ResolveAdjacentMobs` now enumerates `StrapComponent.BuckledEntities` — a real port bug the
  tooling found.
- **RegisterControl double-open crash**: reproduced deterministically via `litany:cast` —
  the 100% Debug-client crash on any prompt litany is now guarded in all four BUIs.

### Unintended consequences / caveats

- Commands mutate live round state unconditionally (kill, holiness set, implant) — they are for
  the sandbox/dev preset only; gated behind `AdminFlags.Debug` so non-admin clients cannot reach
  them.
- `litany:buckle`/`setup` teleport a mob to the nearest altar seat ≤5 m — surprising but
  documented; `litany:machine` on the front tile is the deterministic alternative.
- `litany:cast` needs the quoted proto id (`litany:cast "OxydLitanyConfirmation"`).
- Client toolshed command list syncs at connect — after a server rebuild the client must restart
  or new `litany:` commands show "Unknown command".
- `kill` on a name match previously could delete an organ entity; mob-gated since.

### Standards adherence

- Commands live in `Content.Server/_Oxyd/…` (server-side only), use upstream
  `ToolshedCommand`/`AdminCommand` attributes — same mechanism and gating as stock admin
  commands; no engine/submodule edits (`verify-engine-master.sh` clean).
- Not `#if DEBUG`-gated — consistent with repo's other admin toolshed commands which compile
  into every build; access is permission-gated, not build-gated.
- Integration tests exercise the same code paths the commands hit (`LitanyEffectSystem`,
  `SharedCruciformSystem`), so the tooling rides the tested surface rather than bypassing it.
