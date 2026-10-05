# Eris Moebius medical port. Eris source: CEV-Eris (discordia-space/CEV-Eris).
# Access names live in _Oxyd/moebius/jobs.ftl.

## Surgical tools
oxyd-item-saw-circular-desc-line = For harder materials.

## Medical stacks
oxyd-stack-brutepack = bruise pack
oxyd-stack-ointment = ointment
oxyd-stack-traumakit = trauma kit
oxyd-stack-burnkit = burn kit
oxyd-stack-splint = splint
oxyd-stack-nanopaste = nanopaste


## Injectors

## Misc items

## Machines
oxyd-proxy-surgery = surgery interface
oxyd-proxy-scanner = scanner interface

## Surgery UI
oxyd-surgery-window-title = Moebius Surgery
oxyd-surgery-patient = Patient: {$patient}
oxyd-surgery-select-organ = Select an organ
oxyd-surgery-held-tools = Using: {$tool}
oxyd-surgery-no-tool = bare hands
oxyd-surgery-not-operable = You can't operate on {$patient} standing up - they need to be lying on an operating surface.
oxyd-surgery-flag-external = [external]
oxyd-surgery-flag-internal = [internal]
oxyd-surgery-flag-robotic = [robotic]
oxyd-surgery-step-diagnosewound = Diagnose wounds
oxyd-surgery-step-cutopen = Cut open
oxyd-surgery-step-retractskin = Retract skin
oxyd-surgery-step-fixbleeding = Clamp bleeders
oxyd-surgery-step-cauterize = Cauterise
oxyd-surgery-step-mendbone = Mend bone
oxyd-surgery-step-breakbone = Break bone
oxyd-surgery-step-fixbone = Set bone
oxyd-surgery-step-removeembedded = Remove embedded object
oxyd-surgery-step-insertitem = Insert item
oxyd-surgery-step-removeitem = Remove item
oxyd-surgery-step-attachorgan = Attach organ
oxyd-surgery-step-detachorgan = Detach organ
oxyd-surgery-step-amputate = Amputate
oxyd-surgery-step-roboopen = Unscrew panel
oxyd-surgery-step-robofixbrute = Repair brute damage
oxyd-surgery-step-robofixburn = Repair burn damage
oxyd-surgery-step-roboclose = Screw panel shut
oxyd-surgery-step-extractshrapnel = Extract shrapnel
oxyd-surgery-step-closewounds = Close surface wounds
oxyd-surgery-step-fixorgan = Treat organ damage
oxyd-surgery-mods-wounds = Mods/Wounds
oxyd-surgery-diagnostics = Diagnostics
oxyd-surgery-volume = Volume
oxyd-surgery-conditions = Conditions
oxyd-surgery-diagnose = Diagnose
oxyd-surgery-wounds = Wounds
oxyd-surgery-health = Health
oxyd-surgery-efficiency = Efficiency: {$pct}%
oxyd-surgery-occupied = Occupied space: {$cur} / {$max}
oxyd-surgery-occupied-label = Occupied space:
oxyd-surgery-remove-shrapnel = Remove shrapnel
oxyd-surgery-undiagnosed = Undiagnosed wounds - probe or scan
oxyd-surgery-cond-bleeding = Bleeding
oxyd-surgery-cond-fracture = Fracture
oxyd-surgery-cond-embedded = Embedded
oxyd-surgery-cond-damage = Organ damage
oxyd-surgery-cond-robo = Mechanical damage

# Eris organ card + internal view chrome
oxyd-surgery-wounds-count = Wounds:
oxyd-surgery-oxygen = Oxygen:
oxyd-surgery-blood = Blood:
oxyd-surgery-brute = Brute:
oxyd-surgery-burn = Burn:
oxyd-surgery-efficiency-label = Efficiency:
oxyd-surgery-organ-types = Organ types (efficiency):
oxyd-surgery-amputate = Amputate
oxyd-surgery-disconnect = Disconnect
oxyd-surgery-insert = Insert
oxyd-surgery-internal-return = Return to External Diagnostics
oxyd-surgery-internal-diagnostics = Diagnostics - {$organ}
oxyd-surgery-examine = Examine
oxyd-surgery-modifications = Modifications
oxyd-surgery-attach = Attach
oxyd-surgery-remove = Remove
oxyd-surgery-no-mods = no implants
oxyd-surgery-treat = Treat
oxyd-surgery-type = Type:
oxyd-surgery-severity = Severity:
oxyd-surgery-treatments = Treatments:
oxyd-surgery-no-wounds = No wounds detected.
oxyd-surgery-running = In progress: {$step}
oxyd-surgery-status-fractured = [fractured]
oxyd-surgery-status-splinted = [splinted]
oxyd-surgery-status-clamped = [clamped]
oxyd-surgery-status-open = [open]
oxyd-surgery-status-retracted = [retracted]

# Synthesised wound cards (Eris wounddatums)
oxyd-surgery-wound-incision = Open surgical incision
oxyd-surgery-wound-bleeding = Bleeding surface wound
oxyd-surgery-wound-fracture = Bone fracture
oxyd-surgery-wound-embedded = Embedded objects
oxyd-surgery-wound-brute = Internal bruising
oxyd-surgery-wound-burn = Burn tissue damage
oxyd-surgery-wound-mech = Mechanical damage
oxyd-surgery-wound-short = Burned circuits
oxyd-surgery-treat-incision = clamp bleeders (hemostat) or cauterise
oxyd-surgery-treat-cauterise = cauterise (cautery)
oxyd-surgery-treat-fracture = bone setter or bone gel
oxyd-surgery-treat-embedded = surgical extraction (hemostat)
oxyd-surgery-treat-trauma = advanced trauma kit
oxyd-surgery-treat-burn = advanced burn kit
oxyd-surgery-treat-robo = welder
oxyd-surgery-treat-coil = cable coil

oxyd-medical-surgery-start = You begin the procedure.
oxyd-medical-surgery-success = You complete the procedure.
oxyd-medical-surgery-fail = The procedure fails!
oxyd-medical-surgery-malpractice = Your hand slips, mangling {THE($organ)}!
oxyd-medical-diagnose-healthy = The organ appears healthy.
oxyd-medical-diagnose-damage = The organ shows {$amount} damage.
oxyd-medical-diagnose-fracture = Bones are fractured!
oxyd-medical-diagnose-bleeding = The incision is bleeding.
oxyd-medical-diagnose-embedded = Embedded objects detected: {$count}.
oxyd-medical-fracture = You feel a bone fracture in {THE($organ)}!
oxyd-medical-bone-mended = Your bone knits back together.
oxyd-medical-nsa-overload = Your nerves burn - neural system accumulation overload!

## Sleeper UI
oxyd-sleeper-window-title = Moebius Sleeper
oxyd-sleeper-occupant = Occupant: {$name}
oxyd-sleeper-empty = Empty
oxyd-sleeper-health = Health: {$health}
oxyd-sleeper-beaker = Beaker: {$vol} / {$max}
oxyd-sleeper-no-beaker = no beaker
oxyd-sleeper-inject-chem = Inject {$name} ({$dose}u, {$current}u in patient)
oxyd-sleeper-eject = Eject patient
oxyd-sleeper-eject-beaker = Eject beaker
oxyd-sleeper-chem-inaprovaline = Inaprovaline
oxyd-sleeper-chem-tramadol = Tramadol
oxyd-sleeper-chem-alkysine = Alkysine
oxyd-sleeper-chem-antitoxin = Anti-toxin
oxyd-sleeper-chem-dexalin = Dexalin
oxyd-medical-sleeper-verb-insert = Get inside
oxyd-medical-sleeper-verb-eject = Eject occupant
oxyd-medical-sleeper-chem-max = Patient is saturated with {$chem}.
oxyd-medical-sleeper-chem-saturated = Cannot inject: bloodstream saturated.

## Autodoc UI
oxyd-autodoc-window-title = Autodoc Surgeon
oxyd-autodoc-occupant = Occupant: {$name}
oxyd-autodoc-empty = Empty
oxyd-autodoc-idle = Idle
oxyd-autodoc-running = Running: {$step}
oxyd-autodoc-start = Start procedure
oxyd-autodoc-clear = Clear queue
oxyd-autodoc-eject = Eject occupant
oxyd-medical-autodoc-skip = Autodoc skips {$step} - no suitable organ.
oxyd-medical-autodoc-step-diagnosewound = Diagnose wounds
oxyd-medical-autodoc-step-cutopen = Cut open
oxyd-medical-autodoc-step-retractskin = Retract skin
oxyd-medical-autodoc-step-fixbleeding = Clamp bleeders
oxyd-medical-autodoc-step-cauterize = Cauterise
oxyd-medical-autodoc-step-mendbone = Mend bone
oxyd-medical-autodoc-step-breakbone = Break bone
oxyd-medical-autodoc-step-fixbone = Set bone
oxyd-medical-autodoc-step-removeembedded = Remove embedded object
oxyd-medical-autodoc-step-insertitem = Insert item
oxyd-medical-autodoc-step-removeitem = Remove item
oxyd-medical-autodoc-step-attachorgan = Attach organ
oxyd-medical-autodoc-step-detachorgan = Detach organ
oxyd-medical-autodoc-step-amputate = Amputate
oxyd-medical-autodoc-step-roboopen = Unscrew panel
oxyd-medical-autodoc-step-robofixbrute = Repair brute damage
oxyd-medical-autodoc-step-robofixburn = Repair burn damage
oxyd-medical-autodoc-step-roboclose = Screw panel shut

## Scanner UI
oxyd-scanner-window-title = Moebius Health Scanner
oxyd-scanner-patient = Patient: {$name}
oxyd-scanner-vitals = Vitals - Health {$health} | Brute {$brute} | Burn {$burn} | Toxin {$toxin} | Oxy {$oxy}
oxyd-scanner-extras = Pain {$pain} | NSA {$nsa} | Blood {$blood}/{$bloodmax}
oxyd-scanner-none = none detected

## Reagent guidebook
oxyd-medical-effect-nsa = Increases neural system accumulation by { $value }.
oxyd-medical-effect-nsa-tolerance = Raises the NSA threshold by { $value }.
oxyd-medical-effect-stim = Boosts { $skill } by { $amount } while metabolised.
oxyd-medical-effect-chem-sanity = Applies { $amount } sanity while metabolised.
oxyd-medical-effect-mend-bone = Mends a random fractured organ.
oxyd-medical-effect-heal-organ = Repairs damage to the most wounded organs.
oxyd-medical-effect-reduce-addiction = Advances addiction recovery.
oxyd-medical-effect-suppress-withdrawal = Suppresses withdrawal cravings.
oxyd-medical-effect-seal-wounds = Stops active bleeding.
oxyd-medical-scan-normal = normal
oxyd-medical-scan-robotic = robotic
oxyd-medical-scan-incision = incision
oxyd-medical-scan-bleeding = bleeding
oxyd-medical-scan-fractured = fractured
oxyd-medical-scan-embedded = embedded objects ({$count})

## Chem processor UI
oxyd-processor-window-title = Chemical Processor
oxyd-processor-mode-centrifuge = Centrifuge
oxyd-processor-mode-electrolyzer = Electrolyzer
oxyd-processor-no-beaker = Insert a beaker into the main slot.
oxyd-processor-beaker-status = Beaker {$n}: {$vol}u
oxyd-processor-beaker-empty = Beaker {$n}: empty
oxyd-processor-beaker-n = separation beaker {$n}
oxyd-processor-leave = leave in main beaker
oxyd-processor-start = Start
oxyd-processor-eject = Eject
oxyd-processor-eject-main = Eject main beaker
oxyd-medical-processor-insert-beaker = Insert beaker

## IV drip
oxyd-medical-iv-verb-insert-beaker = Attach beaker
oxyd-medical-iv-verb-inject = Transfuse into patient
oxyd-medical-iv-verb-draw = Draw blood from patient
oxyd-medical-iv-verb-detach = Detach from patient
oxyd-medical-iv-attached = You attach the drip to {THE($patient)}.
oxyd-medical-iv-detached = You detach the drip.

## Stasis bag
oxyd-medical-stasis-seal = Seal inside
oxyd-medical-stasis-sealed = You seal {$patient} inside the stasis bag.
oxyd-medical-stasis-open = Open bag
oxyd-medical-stasis-discarded = The stasis bag is spent and discarded.

## Entity auto-localization (ent-<id>)
ent-OxydMedicalSleeper = sleeper
    .desc = A stasis pod that injects stabilising chemicals into its occupant.
ent-OxydMedicalAutodoc = autodoc
    .desc = An automated surgery pod that performs queued procedures on its occupant.
ent-OxydMedicalCentrifuge = centrifuge
    .desc = Separates a beaker's reagents across separation beakers.
ent-OxydMedicalElectrolyzer = electrolyzer
    .desc = Decomposes a reagent back into its constituent reactants.
ent-OxydMedicalIvDrip = IV drip
    .desc = A medical stand that transfuses fluids into or out of an attached patient.
ent-OxydMedicalOperatingTable = operating table
    .desc = A sterile table for performing surgery.
ent-OxydMedicalMorgueTray = morgue tray
    .desc = A refrigerated tray for long-term storage of cadavers.
ent-OxydMedicalScannerPod = body scanner
    .desc = A diagnostic pod for deep tissue scans.
ent-OxydMedicalSurgeryUiProxy = surgery interface
ent-OxydMedicalScannerUiProxy = scanner interface
ent-OxydSurgicalScalpel = scalpel
    .desc = A standard surgical scalpel. Sharp enough to cut through flesh.
ent-OxydSurgicalScalpelAdvanced = advanced scalpel
    .desc = A finer blade that completes incisions faster.
ent-OxydSurgicalScalpelLaser = laser scalpel
    .desc = A laser scalpel that cauterises as it cuts - doubles as a cautery.
ent-OxydSurgicalHemostat = hemostat
    .desc = Clamps bleeders shut and extracts embedded objects.
ent-OxydSurgicalRetractor = retractor
    .desc = Holds incisions open so you can work inside.
ent-OxydSurgicalCautery = cautery
    .desc = Seals wounds and attaches organs by burning tissue shut.
ent-OxydSurgicalBoneSetter = bone setter
    .desc = Sets fractured and dislocated bones back in place.
ent-OxydSurgicalSaw = surgical saw
    .desc = A large saw for amputations. Slowly.
ent-OxydSurgicalSawCircular = circular saw
    .desc = A powered surgical saw. Amputations go much faster.
ent-OxydSurgicalDrill = surgical drill
    .desc = Drills through bone for precision work.
ent-OxydSurgicalTray = surgical tray
    .desc = A sterile tray for holding organs and instruments.
ent-OxydErisHealthScanner = moebius health scanner
    .desc = A handheld scanner that reads vitals and organ status.
ent-OxydErisStasisBag = stasis bag
    .desc = A folded, one-use bag that puts a patient into stasis, halting deterioration.
ent-OxydErisBodyBag = body bag
    .desc = A plastic bag designed for the storage and transportation of cadavers.
ent-OxydErisMedkit = first aid kit
    .desc = An emergency medical kit for those serious boo-boos.
ent-OxydErisMedkitAdvanced = advanced first aid kit
    .desc = An advanced kit, for when a boo-boo becomes a catastrophe.
ent-OxydErisSurgeryKit = surgical kit
    .desc = A sterile case containing a full set of surgical tools.
ent-OxydErisOrganFreezer = organ freezer
    .desc = A refrigerated container for preserving harvested organs.
ent-OxydErisBruisePack = bruise pack
    .desc = A pack of trauma gel patches for bruises and cuts.
ent-OxydErisOintment = ointment
    .desc = A soothing burn ointment applicator.
ent-OxydErisTraumaKit = advanced trauma kit
    .desc = An advanced trauma kit for serious blunt and sharp trauma.
ent-OxydErisBurnKit = advanced burn kit
    .desc = An advanced burn kit for serious thermal injuries.
ent-OxydErisSplint = medical splint
    .desc = A rigid splint that stabilises broken bones while they knit.
ent-OxydErisNanopaste = nanopaste
    .desc = A tube of nanite paste that repairs synthetic and prosthetic parts.
ent-OxydErisAutoinjectorInaprovaline = autoinjector (inaprovaline)
    .desc = A rapid and safe way to administer small amounts of drugs by untrained or trained personnel.
ent-OxydErisAutoinjectorAntitoxin = autoinjector (anti-toxin)
    .desc = A rapid and safe way to administer small amounts of drugs by untrained or trained personnel.
ent-OxydErisAutoinjectorTricordrazine = autoinjector (tricordrazine)
    .desc = A rapid and safe way to administer small amounts of drugs by untrained or trained personnel.
ent-OxydErisAutoinjectorKelotane = autoinjector (kelotane)
    .desc = A rapid and safe way to administer small amounts of drugs by untrained or trained personnel.
ent-OxydErisAutoinjectorBicaridine = autoinjector (bicaridine)
    .desc = A rapid and safe way to administer small amounts of drugs by untrained or trained personnel.
ent-OxydErisAutoinjectorDexalin = autoinjector (dexalin)
    .desc = A rapid and safe way to administer small amounts of drugs by untrained or trained personnel.
ent-OxydErisAutoinjectorSpaceacillin = autoinjector (spaceacillin)
    .desc = A rapid and safe way to administer small amounts of drugs by untrained or trained personnel.
ent-OxydErisAutoinjectorTramadol = autoinjector (tramadol)
    .desc = A rapid and safe way to administer small amounts of drugs by untrained or trained personnel.
ent-OxydErisAutoinjectorOxycodone = autoinjector (oxycodone)
    .desc = A rapid and safe way to administer small amounts of drugs by untrained or trained personnel.
ent-OxydErisAutoinjectorPolystem = autoinjector (polystem)
    .desc = A rapid and safe way to administer small amounts of drugs by untrained or trained personnel.
ent-OxydErisAutoinjectorMeralyne = autoinjector (meralyne)
    .desc = A rapid and safe way to administer small amounts of drugs by untrained or trained personnel.
ent-OxydErisAutoinjectorRyetalyn = autoinjector (ryetalyn)
    .desc = A rapid and safe way to administer small amounts of drugs by untrained or trained personnel.
ent-OxydErisAutoinjectorBloodclot = autoinjector (blood clotting)
    .desc = A rapid and safe way to administer small amounts of drugs by untrained or trained personnel.
ent-OxydErisAutoinjectorHyperzine = autoinjector (hyperzine)
    .desc = A rapid and safe way to administer small amounts of drugs by untrained or trained personnel.
ent-OxydErisHypospray = moebius hypospray
    .desc = The Moebius Medical department hypospray is a sterile, air-needle autoinjector for rapid administration of drugs to patients.
ent-OxydErisNanopasteSingle = nanopaste
ent-OxydErisBruisePackSingle = bruise pack
ent-OxydErisOintmentSingle = ointment
ent-OxydErisTraumaKitSingle = advanced trauma kit
ent-OxydErisBurnKitSingle = advanced burn kit
ent-OxydErisSplintSingle = medical splint
