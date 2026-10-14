# Eris PDA port — current Oxyd14 stack & phased plan

Planning snapshot of the PDA / cartridge / modular-computer surface in Oxyd14
(`master`) plus relevant work on `origin/eris-asters-guild-port` and Eris sources
in `CEV-Eris`. Goal: decide what to reuse for an Eris PDA port that can nest under
the economy / Asters Guild plan.

---

## Verdict (short)

| Layer | Status | Implication |
|---|---|---|
| Stock SS14 PDA + `CartridgeLoader` + UI fragments | Complete & production | **Reuse as the PDA host** for Eris shells + most programs |
| `_Oxyd` DigitalData / `DigitalDataExecutable` | Stub only (lathe disks live) | Not a runnable OS yet; do **not** block TaxQuickly on it |
| `_Oxyd` PDA cartridges | MIRC pattern on asters/moebius branch only | Proven Cartridge recipe; not on `master` yet |
| Asters economy bank | Shared guild account only (asters branch) | TaxQuickly needs **personal accounts** beyond current bank |
| Eris modular computer / NTOS | Full BYOND stack; absent in Oxyd | Full rewrite cost; port programs as Cartridges first |
| PDA sprites | Stock SS14 `pda.rsi` only; no Eris `modular_pda` | Shell pass = RSI port + job PDA protos |

**TaxQuickly:** implement as a **Cartridge** (not a modular-computer rewrite). It is
`PROGRAM_PDA`-only on Eris and maps cleanly onto `UIFragment` + bank messages.
Blocker is personal money accounts + PIN auth, not the PDA host.

---

## 1. Stock SS14 PDA stack (reuse)

### Entities

- Prototypes: `Resources/Prototypes/Entities/Objects/Devices/pda.yml`
- Abstract hosts: `BasePDA`, `BaseWidePDA`, `BaseSecurityPDA`, `BaseMedicalPDA`
- ~70 concrete job/role PDAs (Passenger…Wizard), all parenting `BasePDA`
- Default preinstalls on `BasePDA`:
  `CrewManifestCartridge`, `NotekeeperCartridge`, `NanoTaskCartridge`, `NewsReaderCartridge`
- Role extras via `EntityTableContainerFill` on `preinstalled-program-container`:
  - Security / HoS / Detective / Brigmedic: `WantedListCartridge`, often `LogProbeCartridge`
  - Medical / Chem / Paramedic / CMO: `MedTekCartridge`
  - Salvage / Cargo (some): `AstroNavCartridge`
  - Admin / ERT: large pocketknife sets of the above

### Host components (every PDA)

| Component | Role |
|---|---|
| `Pda` | ID / pen / pAI slots, owner name, station alert |
| `CartridgeLoader` | Program install/activate; `DiskSpace: 8`; cartridge item slot |
| `ActivatableUI` + `UserInterface` | `PdaBoundUserInterface` (+ Store, Ringer, Instrument, HealthAnalyzer) |
| `Ringer` / `RingerUplink` | Ringtone + traitor uplink hook |
| `UnpoweredFlashlight` / `PointLight` | Built-in light |
| `DeviceNetwork` + `WirelessNetworkConnection` | PDA net (range 500) |
| Sprite layers | Base / flashlight / ID light via `PdaVisuals` |

Containers: `PDA-id`, `PDA-pen`, `PDA-pai`, `Cartridge-Slot`,
`preinstalled-program-container`, `removable-program-container`.

### CartridgeLoader runtime

Shared under `Content.Shared/CartridgeLoader/`:

- `CartridgeLoaderSystem` (+ Programs / Relay / UserInterface partials)
- Programs are **entities** in install containers (or the physical cartridge slot)
- Activate / deactivate / install / uninstall via `CartridgeLoaderUiMessage`
- Active program receives relayed interact + `CartridgeUiMessage` events
- Loader UI state is `CartridgeLoaderUiState`; PDA wraps it as `PdaUpdateState`

Client:

- `PdaBoundUserInterface` : `CartridgeLoaderBoundUserInterface`
- Host chrome: `PdaMenu` / `PdaWindow` / nav + program list XAML
- Per-program UI: `UIFragment` + `UIFragmentComponent` (`Content.Client/UserInterface/Fragments/`)

### Stock cartridges

Prototypes: `Resources/Prototypes/Entities/Objects/Devices/cartridges.yml`
Sprites: `Resources/Textures/Objects/Devices/cartridge.rsi`

| Cartridge | UI model | Notes |
|---|---|---|
| Notekeeper | `UIFragment` | Notes list |
| NanoTask | `UIFragment` | Task list + print |
| NewsReader | `UIFragment` | Newscaster reader |
| CrewManifest | `UIFragment` | Crew list |
| NetProbe | `UIFragment` | Device net probe |
| LogProbe | `UIFragment` | Access-log probe |
| WantedList | `UIFragment` | Criminal records |
| MedTek | **No fragment** | Adds `HealthAnalyzerComponent` to loader on insert |
| AstroNav | **No fragment** | Adds `HandheldGPSComponent` to loader on insert |

Two cartridge patterns to copy:

1. **Fragment program** — component + system (`CartridgeUiReadyEvent` → `UpdateCartridgeUiState`) + `UIFragment` subclass + XAML fragment + yaml `UIFragment` / `Cartridge` / feature component.
2. **Capability program** — on `CartridgeAdded`/`Removed`, add/remove a component on the PDA (MedTek / AstroNav). No program pane.

### Eris ↔ stock overlaps (already covered)

| Eris NTOS program | Stock SS14 analogue |
|---|---|
| `crew_manifest` | `CrewManifestCartridge` |
| `news_browser` | `NewsReaderCartridge` |
| `chem_catalog` (MIRC) | `OxydMircCartridge` on asters/moebius branch (not master) |
| medical scanner hardware | `MedTekCartridge` |
| GPS hardware | `AstroNavCartridge` |
| chat / email / wordprocessor / records / bounty / trade-order | **No** PDA cartridge yet (trade is console-side on asters) |

---

## 2. `_Oxyd` computer / DigitalData work

### On `master` today

`Content.Shared/_Oxyd/ComputerSystems/`:

- `DigitalData` base + `DigitalDataText`, `DigitalDataLathe`, **`DigitalDataExecutable`**
- `DigitalDataHolderComponent` (server `files` + networked `networkedFiles`)
- `DigitalDataSystem` — client reconstructs `files` from networked set only

**Live consumers:** lathe design disks only

- `Resources/Prototypes/_Oxyd/Disks/recipeDisks.yml` → `DigitalDataHolder` + `DigitalDataLathe`
- `Content.Server/Lathe/LatheSystem.cs` reads disk recipes / uses

`DigitalDataExecutable` fields (`ramuse`, `cpuuse`, `uiKey`, `data`) are **unused**.
No PDA, no program runner, no NTNet, no hardware slots.

Intent (from comments): data-hiding filesystem without entity-per-file pollution —
a future modular-computer substrate, not the current PDA path.

### On `origin/eris-asters-guild-port` (relevant)

- **MIRC cartridge** (proven Eris→Cartridge port):
  - `Content.Shared/_Oxyd/Medical/OxydMircCartridgeSystem.cs`
  - Client `OxydMircUi` / `OxydMircUiFragment`
  - Proto `Resources/Prototypes/_Oxyd/Moebius/mirc.yml`
  - Wires into `OxydMoebiusPDA` / `OxydMoebiusChemistPDA` preinstalls
- **Economy (guild-scoped, not personal):**
  - `OxydBankSystem` — single shared station guild balance
  - ATM / cash register / EFTPOS / credits stacks
  - Explicit gap in `docs/asters-guild-port-gaps.md`: *no per-crew personal accounts*
- Trade program lives on **consoles**, not PDAs (Eris also ran trade on modular
  computers; Oxyd adapted to dedicated machines)

No TaxQuickly, no personal PIN accounts, no Eris PDA shell sprites on either branch.

---

## 3. PDA sprites under `Resources/Textures`

### Present (stock SS14)

- `Resources/Textures/Objects/Devices/pda.rsi/` — ~60 job states (`pda`, `pda-captain`,
  `pda-security`, `pda-medical`, …) + flashlight / ID overlays + inhands
- `Resources/Textures/Objects/Devices/cartridge.rsi/` — cart color variants
- Ancillary: box icons, PDA charger module frame
- Economy (asters textures path may differ by branch): `Objects/Economy/cash.rsi`

### Absent (needed for Eris shells)

Eris source of truth: `CEV-Eris/icons/obj/modular_pda.dmi` states include:

`pda`, `pda-c`, `pda-e`, `pda-s`, `pda-m`, `pda-h`, `pda-hop`, `pda-hos`, `pda-ce`,
`pda-cmo`, `pda-rd`, `pda-sup`, `pda-robot`, `pda-syn`, `pda-nt`, `pda-neo`,
`pda-club`, `pda-clown`, `pda-exp`, plus **screen overlays** (`menu`, `generic`,
`command`, `uplink` / `uplink_blue` / `uplink_green`, `alert-*`, `bsod`, …)

Also: legacy `icons/obj/pda.dmi`, NTOS chrome under `icons/NTOS/` (program icons,
battery/signal status GIFs) — only needed if UI is reskinned to Eris NTOS look.

`Resources/Textures/Oxyd/erisported/` has no PDA RSI today.

---

## 4. What Eris PDA port means (two scopes)

### A. Shells only (visual + job mapping) — small

Port Eris modular PDA **look** onto stock `BasePDA`:

1. Convert `modular_pda.dmi` → `Resources/Textures/Oxyd/erisported/.../modular_pda.rsi`
   (base shells; screen overlays optional for phase 1).
2. Add `_Oxyd` PDA entity protos mirroring `preset_pda.dm` subtypes
   (engineering, security, moebius/*, church, heads/*, captain, cargo, club,
   artist/clown, syndicate, ert) with Eris sprite states + flash/ID overlays.
3. Point faction job starting gear at those PDAs (Club / Moebius / Ironhammer /
   Asters / NeoTheology as those ports land).
4. Keep stock CartridgeLoader preinstalls; add faction extras where already
   ported (e.g. MIRC on Moebius when that branch merges).

**Does not require:** DigitalDataExecutable, NTNet, hardware slots, TaxQuickly.

### B. Full NTOS program suite — large

Eris PDAs are modular computers (`/obj/item/modular_computer/pda`) with:

- Hardware: network card, HDD, CPU, cell, card slot, GPS, LED, optional scanner / tesla link
- Filesystem: `datum/computer_file` programs + data files
- NTNet download / P2P / chat / email
- Default PDA programs (`preset_pda.dm`): chatclient, email_client, crew_manifest,
  wordprocessor, records, bounty_board, trade/order, **60% TaxQuickly / else uplink**
- Many programs default to `PROGRAM_ALL & ~PROGRAM_PDA` (excluded from PDAs unless
  overridden); TaxQuickly and uplink are **PDA-only**

Porting “full NTOS” means either:

1. **Cartridge-first (recommended):** implement each Eris PDA-relevant program as a
   Cartridge (+ UIFragment), install via yaml preinstalls / vendors; leave DigitalData
   for disks / future OS; or
2. **Modular rewrite:** finish `DigitalDataExecutable` into a real program runner with
   RAM/CPU/NTNet/hardware — multi-system effort, duplicates CartridgeLoader.

NTOS chrome (window chrome, status icons) is optional polish on either path.

**PDA-relevant Eris programs vs Oxyd:**

| Priority for PDA | Eris program | Oxyd approach |
|---|---|---|
| Economy-critical | TaxQuickly (`taxes.dm`) | New Cartridge + personal accounts |
| Default flavor | email, NTNRC chat, wordprocessor, records, bounty | Cartridges or skip / console-only |
| Faction | MIRC / chem_catalog | Cartridge (exists on asters branch) |
| Faction | drink/cook catalog (club) | Cartridge when Club content lands |
| Already stock | crew_manifest, news | Keep stock |
| Non-PDA / console | suit_sensors, camera, most engi/command monitors | Keep as machines / skip for PDA |
| Antag | uplink program | Stock already has `RingerUplink` — do not dual-port |

---

## 5. TaxQuickly: Cartridge vs modular rewrite

### Eris surface (`taxes.dm`)

- Filename `taxapp`, UI “TaxQuickly 2565”, `usage_flags = PROGRAM_PDA`, NTNet required
- Login with account number + PIN; transaction log; fund transfer
- Aster accounts (`can_make_accounts`): register personal (500cr) / department (2000cr)
- Department payroll: link employees, set/reset wage, resign, disavow
- Screen icon states flip uplink / uplink_blue / uplink_green (PDA shell overlay)

### Dependencies vs current Oxyd

| Dependency | Oxyd today | Needed for TaxQuickly |
|---|---|---|
| PDA host UI | Stock CartridgeLoader | Reuse |
| Shared guild balance | `OxydBankSystem` (asters) | Keep for guild machines |
| Personal accounts + PIN | **Missing** (explicit guild gap) | New account model |
| Transfers / transaction log | Missing | New |
| Department accounts + wage link | Missing | New (payroll phase) |
| Account registration fees | Missing | New (Aster privilege flag) |
| NTNet “online” gate | N/A | Optional flavor / skip |

### Recommendation

**Ship TaxQuickly as a Cartridge** (`OxydTaxQuicklyCartridge` + fragment UI), talking to
a shared `OxydAccountsSystem` (personal + department accounts). Do **not** wait on
`DigitalDataExecutable` or a modular-computer rewrite.

Reasons:

1. Eris itself marks TaxQuickly as PDA-only and “cloud based” (`size = 0`) — no local
   hardware simulation.
2. Oxyd already has a successful Eris-program→Cartridge example (MIRC).
3. Modular rewrite would reimplement CartridgeLoader + delay economy nesting.
4. Guild ATM/EFTPOS can later retarget from “one shared balance” to personal accounts
   without changing the TaxQuickly UI contract.

When (if) DigitalDataExecutable matures, TaxQuickly can be *rehosted* as an executable
file later; the account APIs stay the same.

---

## 6. Reuse points / gaps

### Reuse

- Entire stock PDA entity + BUI + CartridgeLoader install model
- `UIFragment` pattern for every interactive Eris PDA program
- MedTek / AstroNav capability pattern for hardware-like scanners
- MIRC cartridge as the template for catalog-style programs
- Asters `OxydBankSystem` + credits / ATM / EFTPOS as the **cash rail**; extend, don’t replace
- Job/department ports (Club, Moebius, Guild, …) as consumers of Eris PDA shells
- Lathe `DigitalDataLathe` disks unchanged (orthogonal to PDA programs)

### Gaps

- No Eris modular PDA RSI / job shell protos
- No personal / department money accounts (TaxQuickly blocked)
- No TaxQuickly / email / chat / wordprocessor / records cartridges
- `DigitalDataExecutable` is a stub — not an OS
- No NTNet / program downloader (Eris ntdownloader) — optional for v1 if programs are preinstalled
- Screen-state overlays (TaxQuickly color feedback) not in stock visualizer
- MIRC + Moebius PDA wiring not merged to `master`
- Guild docs intentionally skipped full Accounts_DB — that skip is exactly TaxQuickly’s dependency

---

## 7. Recommended phases (nests into economy plan)

Phases are ordered so each can merge independently and feed the next economy slice.

### Phase 0 — Account foundation (economy prerequisite)

- Personal money accounts: id, PIN, balance, transaction log, owner name
- Spawn/link on character join (or first TaxQuickly login / ATM create)
- Extend ATM / EFTPOS to optional personal auth (guild shared account remains for trade)
- **Nest:** unblocks TaxQuickly; upgrades Asters ATM from “guild till” toward Eris ATM

### Phase 1 — PDA shells (visual / identity)

- Port `modular_pda.dmi` → Oxyd RSI
- `_Oxyd` Eris PDA protos + job starting-gear swaps
- Optional: border colors / ErisNanoStyle tweaks on `PdaMenu` (low priority)
- **Nest:** faction ports look correct; no economy dependency

### Phase 2 — TaxQuickly Cartridge (economy PDA MVP)

- `OxydTaxQuicklyCartridge` + UIFragment (login, balance, transfer, logs)
- Preinstall on Asters / command / probabilistic civilian PDAs (match Eris 60% roll or always-on for clarity)
- Aster registration fees once Phase 0 has `can_make_accounts`-class flag
- **Nest:** primary player-facing banking UI; guild trade keeps using shared account until Phase 3

### Phase 3 — Payroll / department accounts

- Department accounts, employee link, wage set/reset, payday ticker
- TaxQuickly department UI panels
- **Nest:** completes Eris tax app; connects job wages to cash economy

### Phase 4 — Faction / catalog cartridges

- Land MIRC (from moebius/asters work) on Moebius PDAs
- Club drink/cook catalogs when Club content is ready
- Signaller / other small `PROGRAM_ALL` utilities as needed
- **Nest:** deepens PDA as Eris “toolkit” without OS rewrite

### Phase 5 — Optional NTOS / DigitalData runner (defer)

- Only if multiple non-Cartridge hosts (laptops, consoles) need the same executables
- Flesh out `DigitalDataExecutable` + holder UI + install-from-disk
- Rehost TaxQuickly / catalogs as files **or** keep dual Cartridge wrappers
- NTNet relay / downloader last
- **Nest:** research/engineering laptop fantasy; not required for economy

### Explicit non-goals for early phases

- Full hardware simulation (CPU/RAM/tesla/scanner slots) on PDAs
- Dual uplink program (stock `RingerUplink` stays)
- Porting console-only monitors into PDAs
- Replacing stock CrewManifest / News / Notekeeper unless Eris UI parity is demanded

---

## 8. File map (quick index)

| Area | Path |
|---|---|
| PDA protos | `Resources/Prototypes/Entities/Objects/Devices/pda.yml` |
| Cartridge protos | `Resources/Prototypes/Entities/Objects/Devices/cartridges.yml` |
| PDA sprites | `Resources/Textures/Objects/Devices/pda.rsi/` |
| Shared PDA | `Content.Shared/PDA/` |
| Server PDA | `Content.Server/PDA/PdaSystem.cs` |
| Client PDA UI | `Content.Client/PDA/` |
| CartridgeLoader | `Content.Shared/CartridgeLoader/` |
| Client fragments | `Content.Client/CartridgeLoader/`, `Content.Client/UserInterface/Fragments/` |
| DigitalData stub | `Content.Shared/_Oxyd/ComputerSystems/` |
| Lathe disks | `Resources/Prototypes/_Oxyd/Disks/recipeDisks.yml` |
| Guild economy (branch) | `Content.*/_Oxyd/AstersGuild/`, `docs/asters-guild-port-*.md` |
| MIRC example (branch) | `Resources/Prototypes/_Oxyd/Moebius/mirc.yml`, `*_Oxyd/Medical/OxydMirc*` |
| Eris PDA device | `CEV-Eris/code/modules/modular_computers/computers/subtypes/dev_pda.dm` |
| Eris PDA presets | `.../preset_pda.dm` |
| Eris TaxQuickly | `.../file_system/programs/generic/taxes.dm` |
| Eris PDA icons | `CEV-Eris/icons/obj/modular_pda.dmi` |

---

## 9. Decision checklist

- [ ] Confirm Cartridge-first (not DigitalDataExecutable) for TaxQuickly
- [ ] Confirm Phase 0 personal accounts as economy plan item (lifts guild gap)
- [ ] Confirm Phase 1 shell RSI can ship without any new programs
- [ ] Decide TaxQuickly preinstall policy (always vs Eris 60% / uplink alternate)
- [ ] Decide whether ATM personal login lands with Phase 0 or waits for Phase 2
