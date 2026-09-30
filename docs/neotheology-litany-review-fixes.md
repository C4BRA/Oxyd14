# NeoTheology review-fix checklist

Review fixes on `eris-litany-port`, following the five litany follow-ups through
`7df3c31239`. This checklist accompanies the PR #33 review-fix update. The
[original review](neotheology-litany-review.md) is retained as historical evidence;
this document separates changes from executed validation.

## Engine invariant

`RobustToolbox` HEAD, parent HEAD gitlink, and index all equal Oxyd14
`upstream/master`: `edf061e7450a4074f173e3000bf1552b6f54082f`.
There are no tracked engine edits, PR engine commits, `.gitmodules` changes,
or PR project/build-setting changes. Verify with:

```sh
bash Tools/verify-engine-master.sh
```

All new events, adapters, and machine implementations are content-side.

## Findings addressed in code

| Finding | Change | Regression / limit |
| --- | --- | --- |
| F01 | Activation and normal job/admin grants install cloning and record identity; mind assignment refreshes the snapshot. | Normal grants tested across all six profiles; soul/module lifecycle tests no longer manually install cloning. |
| F02 | Reincarnation validates inactive saved implant, DNA, available saved mind and vacant living vessel, then restores saved identity/mind. Death snapshots survive corpse changes. Resurrection prepares an empty vessel and guards duplicate living vessels. | Soul tests now assert saved-mind restoration and unoccupied growth, not snapshot overwrite. Full two-client death/ghost/revival remains a live check. |
| F03 | Choice submission and commit re-resolve current eligibility, preserve original recipients, reject queued entities and swapped/deimplanted follower links. Book blessing pins the original held oddity. | Pending follower casts exercise moved and extracted-implant targets before payment. Geometry/station/facing share this central path. |
| F04 | Reveal scans from caster, distinguishes 14 m live hostile fauna and 7 m visible traps, and retains source random rolls. | Seeded live/dead native carp regression; source 20% hidden miss and 80% trap roll remain. |
| F05 | Growth scans once from caster, checks visible plants, and has non-mutating validation. | No second mob required; validation leaves growth multiplier unchanged. |
| F06 | Installed modules alone authorize sets; removing specialization cannot regain it from profile metadata. | Removal/recompute cases for all three specializations. |
| F07 | Normal clergy rank grants initialize clergy clearance; restricted access tags obey clearance; public doors require no cruciform. | Six normal-grant cases and public/clergy door events. Common clearance still comes from Adoption, not an implicit grant. |
| F08 | Scrying marker is parented to target; deletion ends session and restores eye. | Moving target and target-deletion regression. |
| F09 | Profile starting modules are the single grant recipe; validator calculates module-derived capacity. Preacher gets Red Light, Inquisitor does not; capacities are 80/100, without duplicate regen. Confirmation preserves clergy rank. | Normal role grants and conversion tests. Capacity modifiers use double precision; rank transitions retain absolute holiness and clamp only to final capacity, not an intermediate base capacity. |
| F10 | Base regen is 20/min; live cognition/righteousness/follower inputs recompute. Preacher grants channel; ceremonies add righteous life; native successful metabolism charges narcotics/non-Cahors alcohol. | Lifecycle 20/min check plus live-follower/righteousness and Beer/Cahors/narcotics event regressions pass. Native metabolism timing and player-facing behavior still need a live check. |
| F11 | Uproot queues before refund; ingredients/constructs queued for deletion cannot be reused. Entire construction recipe is preflighted before consumption. | Two Uproot attempts in the same update; no second refund. |
| F12 | BuildTime controls Manifestation delay; materials are compared in caster-parent coordinates. Native canisters, solidifier, bioprinter, armaments printer and door variants are constructible. Upgrade/kit recipes supply acquisition paths. | Construction tests and reservoir transfer. Multipart liquid plumbing remains explicitly adapted, not a complete source port; new costs are local balance data. |
| F13 | Failed fuel debit disables supply and skips generator output. | Positive remaining fuel with unaffordable accumulated debit produces zero power. |
| F14 | Core upgrade items use a separate container/module map from physical attachments. Initiation needs an installed ascension kit; Asacris removes core upgrades. Martyr excludes dead/non-mob/actually linked bearers; Cleansing withers native kudzu and does not inherit healing. | Kit installation/promotion/removal preserves attachment. Additional source core-upgrade products and maintenance-shroom specifics remain full-port gaps. |
| F15 | Bible may remain active while the sole other held oddity is blessed; ambiguous/switching oddities fail. | Book-origin begin with an off-hand nonzero-giving oddity, alongside existing blessing-effect tests. The regression supplies a test oddity component; source oddity acquisition/products remain a full-port gap. |
| F16 | Epiphany fixture uses a normal Preacher grant, providing the actual Acolyte module. Old soul/activation/rank fixtures are updated for corrected behavior. | Fresh full focused run: 262 passed, zero failed/skipped. Transaction fixtures isolate regeneration instead of weakening debit assertions. |
| F17 | Open book viewers receive private snapshots each second, including cooldown availability/end time. Client shows countdowns; docs distinguish real roles, eight ceremonies, soul transfer and adaptations. | Snapshot cooldown expiry and existing privacy/UI tests. Original user playtest handoff and progress link retained. |

## Validation

| Check | Final result |
| --- | --- |
| Content shared/server/client + integration compilation | Succeeded through the final integration build; warnings remain. |
| NeoTheology integration tests | **262 passed, 0 failed, 0 skipped** on the final code/fixtures. |
| NeoTheology and shared-skill unit tests | **21 passed, 0 failed, 0 skipped** on the final code. |
| Engine/master verifier | Passed: parent HEAD/index and clean engine HEAD equal `upstream/master`; no PR engine/build-config changes. |
| `git diff --check` | Passed. |
| Full YAML linter | Still blocked by unrelated `fake_moustache.yml`, line 5, invalid mapping; exit 134. This is not a clean-YAML claim. |

Earlier runs exposed obsolete rank expectations, natural-regeneration drift in
transaction fixtures, float-capacity precision, and skipped fixture failures.
Skipped cases were isolated: this found the real spawn-to-grid scrying issue,
incorrect old marker-coordinate assertions, an empty test oddity, an unpowered
airlock veto, and a construction event test missing broadcast delivery. These
were corrected before the final **zero-skip** full focused run. The same-update
Uproot double-refund regression now passes, upgrading F11 from source-only risk
to executed coverage.

```sh
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-build --no-restore --filter 'FullyQualifiedName~NeoTheology'
dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter 'FullyQualifiedName~NeoTheology|FullyQualifiedName~SharedSkillSystemTests'
bash Tools/verify-engine-master.sh
git diff --check
dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj --no-restore
```

Final TRXs: `Content.IntegrationTests/TestResults/neotheology-review-fixes-complete.trx`
and `Content.Tests/TestResults/neotheology-review-fixes-unit-final.trx`.
Logs: `/tmp/oxyd-fix-integration-complete.log`, `/tmp/oxyd-fix-unit-final.log`,
`/tmp/oxyd-fix-yaml-final.log`. To rebuild rather than reuse binaries, omit
`--no-build` from the integration command.

No current-head CI or two-client multiplayer certification has been performed.

## Still not a full Eris port

These fixes do not add Church round-start jobs/maps, all source offerings/store
products, complete objective/global world hooks, every art/audio asset, a powered
Crusader HUD, or exact multipart liquid machinery. Catalog coverage remains
**60/60 phrases, not 100% gameplay parity**. The original review's published-head
and commit/file inventories describe its historical baseline, not the updated PR head.
