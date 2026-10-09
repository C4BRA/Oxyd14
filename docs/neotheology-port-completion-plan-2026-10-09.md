# NeoTheology port — path to ≥99% (plan, 2026-10-09)

Scope: the five items from the completion gap analysis, addressed in order.
Verification standard: every change is proven live in-game (two-client where
needed) with screenshots appended to `neotheology-litany-verify-2026-10-07.md`,
matching prior passes. Dev-tool gaps are filled with new `litany:` commands
(server-side, `AdminFlags.Debug`, each with a distinct purpose). Skill +
memory files updated at the end of every phase with whatever the phase taught.

Ordering note: 4 (analyzer cleanup) runs *in parallel* as background-ish work —
full builds stay blocked until it lands, so it gets fixed before the first
feature commit, but verification tooling for 1–3 doesn't wait on it.

---

## Phase 1 — EOTP disk-purchase list (port `eopt.tmpl` shop)

**Gap:** the Eye's Eris UI sells design disks for observation points
(Utilities 50 → Destroyer 325). The port's EOTP window only reports
observation/armament points + miracle cooldown; gear routes via the
armaments printer + uplink.

**Design first (must not guess):** in the port the armaments printer already
sells the same *category* of disks for *armament* points. Decide one of:
- (a) Port the Eris catalog as an **observation-points** shop on the eye,
  distinct currency from the printer (Eris parity — two currencies, two shops).
- (b) Merge: EOTP window shows the same armament catalog but spends
  observation points where Eris did.
- (c) Document divergence, skip implementation.
Default unless told otherwise: **(a)**, since it matches `eopt.tmpl` and the
two-point-economy already exists (`Observation` vs `ArmamentsPoints`).

**Work (a):**
- `EotpShopEntryPrototype` (id, name, desc, cost, path) or reuse
  `ArmamentPrototype` with an `observationCost` field — check which is less
  invasive against `armaments.yml`.
- `EyeOfTheProtectorSystem.TryPurchaseDisk(eye, user, entryId)`: gates =
  operational, cruciform profile in rules, range, `Observation >= cost`;
  then debit observation points, spawn `entry.Path` at the eye.
- Window: add scrollable disk list to `EyeOfTheProtectorWindow` using the
  existing `ErisNanoStyle` kit (ErisLink rows, cost column) replicating
  `eopt.tmpl`'s product list; reuses the proven 1s re-push.
- Integration test mirroring `ArmamentsPrinter` purchase tests
  (spend/spawn/insufficient/range).

**New dev tools:** `litany:obspoints [amount]` — grant observation points to
the first eye (mirrors `litany:armpoints`; needed because observation points
only accrue via released-miracle mechanics otherwise).

**Verify live:** fund via `litany:obspoints` → open eye UI → disk list
populates → buy → points debited + disk spawns at eye → insufficient-funds
row states when broke.

**Effort:** ~1–1.5 days incl. tests + live verify.

## Phase 2 — Holiness HUD indicator

**Gap:** cruciform holiness is only visible inside the Bible window; Eris
surfaces it continuously.

**Work:**
- Pick placement matching the fork's HUD conventions: small bar + numeric
  readout in the left HUD cluster (where hunger/thirst analogs would sit),
  or an alert icon with holiness in tooltip/examine. Eris renders a tiny
  cruciform HUD element — replicate with `ErisNanoStyle`-consistent flat
  styling, NOT `OxTag` (that texture caused the giant-frame incident).
- Plumbing: holiness already exists server-side on `CruciformComponent`;
  confirm whether it's networked to the bearer today (litany UI state
  carries it? if not, add it to cruciform component state or a dedicated
  `HolinessHudState`).
- Client: HUD element updating on component state change + refresh on
  litany casts (cast debit is the main dynamic event — already networked).

**Dev tools:** none new — `litany:holiness` already sets arbitrary values
for boundary testing (0, max, mid, post-cast debit).

**Verify live:** HUD shows 100/100 after cruciform grant → cast a paid
litany → HUD drops live → `litany:holiness 0` boundary → regenerate vs.
obelisk → HUD rises. Screenshot each transition.

**Effort:** ~0.5–1 day.

## Phase 3 — VFX/audio polish

**Gap:** Eris litanies carry glow/particle feedback and chant audio; the
port is text-only.

**Work:**
- Enumerate Eris effects in cev-eris (`icons/effects`, `sound/`): litany
  cast glow, Epiphany flash, obelisk/altar idle glow, cruciform-activate
  burst, crusader/armament activation shines.
- Map to RT equivalents: `SharedEffectSystem` prototype effects for one-shot
  glows, `PointLight` pulse for radiance, `SoundSpecifier` per litany yaml
  (channel respect: spoken chant audio vs. UI sound).
- Scope control: prioritize (1) cast-complete glow on caster, (2) Epiphany/
  activate flash, (3) obelisk idle aura indication. Secondary effects get a
  follow-up list, not a blocker.
- Sprites: reuse already-extracted Eris effect sprites where licensing is
  covered; otherwise RT-native effects — flag anything needing new art.

**Dev tools:** `litany:effect <id>` only if an effect can't be triggered by
an existing command (casts/machines already cover most); decide during work.

**Verify live:** before/after screenshots of a cast glow + activation flash;
audio can't be screenshotted — verify via client log + describe_audio on a
recording if needed.

**Effort:** ~1–1.5 days for the priority trio.

## Phase 4 — Analyzer cleanup (full Release build red: ~180 violations)

**Gap:** `TreatWarningsAsErrors` in `MSBuild/Content.props` escalates
pre-existing diagnostics to errors on any full compile: `RA0051` readonly
`[Dependency]` fields (~170), `RA0049` non-partial dep class, `RA0030`
generic Transform misuse, `CS0414` dead fields. Prior "green" builds were
incremental and hid it.

**Work (isolated commit, no feature changes):**
- `RA0051`: strip `readonly` on `[Dependency]` field lines tree-wide
  (mechanical; DI assigns them). Pattern verified on 5 files this pass.
- `RA0049`: add `partial` to `ViewCalcSystem` (+ any others reported).
- `RA0030`: `Transform(uid)` generic → non-generic in `BioreactorSystem`.
- `CS0414`: remove genuinely dead fields (`_timing` in
  `NeoTheologyFoundationSystem`, `_transform` in `BioreactorSystem`, …).
- Rebuild **with** `TreatWarningsAsErrors` → must be 0 errors. Run the full
  NT integration suite (289 tests) to catch any behavior slip.
- Keep it a separate commit/PR — review noise otherwise.

**Effort:** ~0.5 day (mechanical, mostly scripted edits + one green build).

## Phase 5 — Live verification sweep (close remaining untested surfaces)

**Method:** numbered scenario checklist; two clients where observation from
a second perspective matters; every result + screenshot appended to the
verify doc as Pass 7. Server restart between scenario groups when entity
state gets messy (known-good restart recipe is in the skill now).

**Scenario list:**
- **Cruciform upgrades (7):** install each `litany:upgrade`-able upgrade,
  exercise its effect (stat deltas visible via `litany:status` fields).
- **Modules (13):** install via new tool, verify each module's granted
  litany/power actually works (cloning module already proven via
  Reincarnation).
- **Reagent litanies:** verify bloodstream reagent injection —
  `litany:reagents` dump before/after (new tool).
- **Obelisk unverified branches:** hostile-faction damage (spawn hostile
  NPC near obelisk — `litany:hostile`), weed removal (`litany:weed` spawns
  Eris weeds in radius), sanity delta readout, cooldown pulse effect on an
  active litany cooldown.
- **Sanctify objective flip:** spawn a sanctify-type objective entity in
  the area first (`litany:objective`), then run the ceremony and observe
  the Completed flip — Pass 5 verified the mechanism but no live objective
  existed.
- **Edge cases:** caster dies mid-chant (cast cleanup), client disconnect
  mid-cast (kill client process mid-chant → session cleanup + no orphan
  doAfter/session), two casters targeting the same mob, scrying during
  target death (marker cleanup — ScryingSystem has shutdown handlers,
  observe them fire).
- **Regression spot-checks:** choice-window full click-through after the
  `SendViewerSnapshot` fix, uplink NT listings purchase, powered-door
  clearance behavior.

**New dev tools (only what's missing — each distinct):**
- `litany:obspoints [amount]` (phase 1 also needs it)
- `litany:module <id>` — install a module into a cruciform implant
- `litany:upgrade <id>` — install a cruciform upgrade
- `litany:reagents` — dump a mob's bloodstream reagent list
- `litany:weed` — spawn weed entities in radius around a target
- `litany:hostile` — spawn a hostile-faction NPC near a target
- `litany:objective <type>` — spawn an objective entity in the sanctify area
(Any found redundant against existing commands gets dropped — no duplicates.)

**Effort:** ~1–1.5 days depending on what the sweep surfaces.

## Continuous — skills & memory loop

- After each phase: append new env/tool lessons to
  `ss14-oxyd-in-game-testing` (already carries: ALSOFT_DRIVERS, quoted proto
  args, taskkill-before-build, stale-frame client restart,
  TreatWarningsAsErrors escape hatch) and `~/memory/repos/oxyd14.md`, then
  `memory_sync`.
- Keep `[DBGLIT]` probe convention: temporary instrumentation reverted
  before commit; permanent debug verbs stay small and distinct.
- Each phase ends: build + targeted live verify → screenshot evidence →
  commit → doc append. No phase merges unverified.

## Out of scope (owner decisions, tracked separately)

- NT jobs + map/chapel placement (how NT enters real rounds).
- Eris sprite licensing resolution.
- Maintainer review resolution + merge of PR #33.
