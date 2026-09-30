# NeoTheology litany progress

Status: **60/60 catalog entries enabled; gameplay parity not verified.** PR #33
(`C4BRA:eris-litany-port`) is the only open litany PR. Its branch includes the
medical and altar recovery pass, the client sandbox fix, and AEV-Oxyd `master`
through `97c050d91a`. The upstream merge brings the newer game UI and the
upstream `RobustToolbox` revision. It keeps the PR's visibility fix in
`ViewCalcSystem` where the two branches conflicted.

The [2026-09-29 manual client playtest handoff](neotheology-litany-playtest-2026-09-29.md)
records the partial live results and the remaining gameplay matrix.

[neotheology-litany-dependencies.md](neotheology-litany-dependencies.md) lists
the implemented dependency packets and their limits. The tests check catalog
coverage, selected effects, and security rules. They do not prove Eris gameplay
parity. The current merged build still needs a live-round check.

## Review-fix worktree

The original review and merged-build counts below are historical. Current fixes
and fresh validation are tracked in [the resolution checklist](neotheology-litany-review-fixes.md).
`RobustToolbox` remains identical to `upstream/master`; content-level adapters
must not require engine edits. No complete-port or multiplayer certification is implied.

## What works in code and focused tests

- All 60 entries have enabled definitions and handlers. Nine sets authorize
  litanies through installed modules; profiles declare starting-module recipes.
  No dependency-gated entries remain.
- Speech and the private Bible UI start server-validated casts. The server
  checks rank, target, holiness, and cooldown. It refunds an unsuccessful cast.
  The book supplies target and blueprint choices. Speech uses a fixed fallback
  when a choice is necessary.
- The six profiles exist in `profiles.yml`. Only Chaplain maps to
  `OxydNtPreacher` in `rules.yml`. Six development-only
  `RandomHumanoidSpawnerOxydNt*` markers in `test_roles.yml` create test ghost
  roles. Each role receives its matching cruciform when a mind takes it and
  starts with `OxydNtBible`. An administrator must spawn a marker in a live
  round before the role appears in the ghost-role list. These roles do not
  add station jobs or job slots. Chaplain still does not receive
  `OxydNtBible` automatically.
- Medical rites use local reagents, pain, addiction, implants, robotic organs,
  and the cloner. Altar procedures check position, restraint, posture, and
  clothing as applicable. `Asacris` removes installed upgrades, not rank
  modules.
- The Eye, machines, hidden store, construction, upgrade effects, and group
  ceremony engine have implementations and focused integration tests.

## Checks after the upstream merge

| Check | Result |
| --- | --- |
| Server build | 0 errors, 496 warnings. |
| Client build | 0 errors, 250 warnings. |
| NeoTheology integration tests after test-role fix | 232 passed, 0 failed, 0 skipped. |
| Client build after test-role fix | 0 errors. |
| NeoTheology and shared-skill unit tests | 21 passed, 0 failed, 0 skipped. |
| Merged client and server smoke check | The server reached `Ready` on port 1212. The client reached `InGame`. Startup still reported prototype and sprite errors. No litany cast was checked. |
| Full YAML linter | Stopped at the existing syntax error in `Resources/Prototypes/_Oxyd/erisPorted/mask/fake_moustache.yml` (line 5). The litany PR does not change that file. |

These are counts for the merged checkout. Earlier test counts in past commits
are historical results, not results for this merge. The merge did not replace
NeoTheology files from the PR with older upstream versions.

## Known differences from Eris

- **Target choice:** `Atonement` and `Penance` use `VisibleFollower`.
  `Excommunication`, `Scrying`, and `Sending` use `StationFollower`. Adjacent
  rites check the actor or faced tile within 1.5 m. `OrderArmaments` selects
  the local armaments printer. Manual speech cannot show the book's choices.
- **Cooldown:** `Relief`, `SoulHunger`, `Entreaty`, `RevealAdversaries`,
  `CruciformSense`, and `Revelation` have a one-minute personal cooldown.
  Ceremonies have a one-second per-starter guard against overlap.
- **Cost:** `DivineGuidance` and `Knowledge` cost zero. Eris lists a cost for
  these rites but does not debit it when their effect returns false.
- **Medicine:** Native reagent metabolism replaces Eris reagent behavior.
  Local pain and stamina replace hallucination loss. `WordsOfPurging` advances
  addiction recovery without removing blood reagents. `Rejection` removes
  foreign implants and robotic organs but keeps natural organs and the
  cruciform. Exact recovery timing needs a live check.
- **Cloning and ranks:** Resurrection grows an unoccupied stored-profile vessel;
  Commitment and Reincarnation reunite the saved soul, with matching DNA and
  inactive-implant checks. The local rank-based clone-damage exemption exists.
  Clearance and specialization are separate from rank. Initiation requires
  an installed, removable ascension kit.
- **Machines and construction:** Eris multipart/liquid machines use native
  single entities and material storage. Front-machine rites require the faced
  machine; holy doors have a broken state. Divine Guidance includes canisters,
  bioprinter, solidifier, armaments printer and door variants. Manifestation
  honors the selected blueprint's build time. New fabrication costs are
  explicitly local balance data, not an assertion of source economy parity.
- **Economy:** The Eye uses local observation rewards. Oddity rewards and
  faithless penalties lack the necessary entities. `Knowledge` and `Bounty`
  use a local hidden store and banked balance, not Eris equipment or its
  hidden uplink item.
- **Ceremonies:** Eight multi-phrase ceremonies persist until their leader dies
  or loses the recorded implant; final recipients must still be nearby and linked.
  Three Crusader rites are ordinary casts, not additional multi-phrase ceremonies.
  Short skill blessings stack and last ten minutes. Sanctify has local obelisk
  activation, and an atheist marker/knockdown exists; full Church objectives and
  source Crusade world signaling remain gaps. The disciple HUD uses no power
  and stops with the implant.

## Open gaps and live-round checks

1. Spawn each `RandomHumanoidSpawnerOxydNt*` marker with administrator tools
   in a live round. Take the ghost role. Check its cruciform, rank, Bible,
   and book UI with the new game UI. The focused test checks role creation,
   mind transfer, implants, and books; it does not check live client use.
2. Check spoken phrases, front and grabbed targets, book choices, costs,
   cooldowns, failed casts, and target privacy with two players.
3. Check healing, local NT reagents, addiction recovery, pain, and
   `Rejection` on implants and robotic organs.
4. Check `Reincarnation`, `Resurrection`, corpse handling, and the stored
   profile. Check altar posture, restraint, clothing, and material use.
5. Check `Asacris` on installed upgrades. Check that the rite keeps rank
   modules. Check `Initiation` and the hidden Inquisitor store.
6. Check the Eye, armaments disks, powered machines, construction, and the
   reclaimer in a live round. Oddity and faithless behavior remain incomplete.
7. Check followers, distance, interrupted casts, `Sanctify`, `Crusade`,
   `EternalBrotherhood`, and `SearingRevelation` in live ceremonies.
8. Check the Sanctify church-objective signal, Crusade objectives, normal
   Inquisitor access, and Chaplain Bible access. These gaps still need work;
   the passing focused tests do not close them.

The six test ghost roles require administrator-spawned markers. They are not
round-start jobs. Check each role with a real ghost in a live round before
claiming that the test path or its litanies work in the client.
