#!/usr/bin/env python3
"""Generate maps/ui.json: HUD theme, inventory, storage and alert textures."""

from __future__ import annotations

import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ES = "icons/mob/screen/ErisStyle.dmi"
HOLO = "icons/mob/screen/ErisStyleHolo.dmi"
ROBOT = "icons/mob/screen1_robot.dmi"
UI16 = "icons/ui_icons/dmis/uiicons16.dmi"


def eris(state, dmi=ES, **kw):
    return {"dmi": dmi, "state": state, **kw}


def own(path, **kw):
    """Existing SS14 art pulled onto the Eris HUD palette."""
    return {"rsi": path[0], "state": path[1], **kw} if isinstance(path, tuple) else None


def panel(glyph_src):
    """Eris empty HUD panel with an SS14 glyph recoloured on top."""
    return {"layers": [eris("block"), glyph_src]}


entries: list[dict] = []


def file(path, src, standin=False):
    entries.append({"file": path, "src": src, "standin": standin})


def rsi(target, states, standin=False):
    entries.append({"target": target, "states": states, "standin": standin})


# --- Inventory slot theme (Interface/Default) ----------------------------
D = "Interface/Default/"
slots = {
    "back": "back", "belt": "belt", "ears": "ears0", "glasses": "glasses", "gloves": "gloves",
    "hand_l": "hand-l", "hand_l_no_letter": "hand-l", "hand_r": "hand-r", "hand_r_no_letter": "hand-r",
    "head": "hair", "id": "id", "mask": "mask", "pocket": "pocket_l", "shoes": "shoes",
    "suit": "equip", "suit_storage": "suit-belt", "toggle": "b-open", "uniform": "center",
}
for name, st in slots.items():
    file(f"{D}Slots/{name}.png", eris(st))
# No Eris equivalents: SS14 glyph recoloured onto an Eris panel.
for name in ("neck", "web", "hand_m"):
    file(f"{D}Slots/{name}.png", {"layers": [eris("block"), {"file_png": f"{D}Slots/{name}.png", "recolor": "eris_ui"}]}, True)
file(f"{D}SlotBackground.png", eris("block"))
file(f"{D}template_small.png", eris("block"))
file(f"{D}blocked.png", eris("x"))
for name in ("slot_highlight", "item_status_left", "item_status_left_highlight", "item_status_right",
             "item_status_right_highlight"):
    file(f"{D}{name}.png", {"file_png": f"{D}{name}.png", "recolor": "eris_ui"}, True)
for name in ("back", "exit", "marked_first", "marked_second", "piece_bottom", "piece_bottomLeft",
             "piece_bottomRight", "piece_center", "piece_left", "piece_right", "piece_top", "piece_topLeft",
             "piece_topRight", "sidebar_bottom", "sidebar_fat", "sidebar_mid", "sidebar_top", "tile_blocked",
             "tile_blocked_opaque", "tile_empty", "tile_empty_opaque"):
    file(f"{D}Storage/{name}.png", {"file_png": f"{D}Storage/{name}.png", "recolor": "eris_ui"}, True)
file("Interface/Inventory/inv_slot_background.png", eris("block"))
file("Interface/Inventory/hand_slot_highlight.png",
     {"file_png": "Interface/Inventory/hand_slot_highlight.png", "recolor": "eris_ui"}, True)
file("Interface/Inventory/hand_active.png", eris("act_hand-l"))
file("Interface/handsbox.png", eris("block"))

# --- Alerts ---------------------------------------------------------------
A = "Interface/Alerts/"
rsi(A + "human_alive.rsi", {f"health{i}": eris(f"health{e}") for i, e in enumerate((0, 2, 3, 4, 5))})
rsi(A + "human_critical.rsi", {"critical": eris("health6")})
rsi(A + "human_dead.rsi", {"dead": eris("health7")})
rsi(A + "hunger.rsi", {"overfed": eris("nutrition0"), "peckish": eris("nutrition2"), "starving": eris("nutrition4")})
rsi(A + "temperature.rsi", {"cold1": eris("temp-1"), "cold2": eris("temp-2"), "cold3": eris("temp-4"),
                            "hot1": eris("temp1"), "hot2": eris("temp2"), "hot3": eris("temp4")})
rsi(A + "pressure.rsi", {"lowpressure1": eris("pressure-1"), "lowpressure2": eris("pressure-2"),
                         "highpressure1": eris("pressure1"), "highpressure2": eris("pressure2")})
rsi(A + "breathing.rsi", {"not_enough_oxy": eris("oxy1"), "too_much_tox": eris("tox1"),
                          "not_enough_co2": eris("oxy1"), "not_enough_nitro": eris("oxy1"),
                          "not_enough_tox": eris("oxy1"), "too_much_co2": eris("tox1"),
                          "too_much_nitro": eris("tox1"), "too_much_oxy": eris("tox1")})
rsi(A + "internals.rsi", {"internal0": eris("internal0"), "internal1": eris("internal1"), "internal2": eris("internal2")})
rsi(A + "walking.rsi", {"walking": eris("walking")})
rsi(A + "borg_alive.rsi", {f"health{i}": eris(f"health{e}") for i, e in enumerate((0, 2, 3, 4, 5))})
rsi(A + "borg_critical.rsi", {"critical": eris("health6")})
rsi(A + "borg_dead.rsi", {"dead": eris("health7")})
rsi(A + "battery.rsi", {
    **{f"battery{i}": eris(f"charge{min(4, i * 4 // 10)}", ROBOT) for i in range(1, 11)},
    "battery0": eris("charge-empty", ROBOT), "battery-none": eris("charge-empty", ROBOT)})
file(A + "Fire/fire.png", eris("fire2"))
file(A + "Pull/pulling.png", eris("pull1"))
file(A + "Pull/pulled.png", eris("pull0"))

# Alerts with no Eris art: SS14 glyph on the Eris panel in Eris HUD colours.
for target in ("thirst.rsi", "stamina.rsi", "bleed.rsi", "ensnared.rsi", "pacified.rsi", "status_effects.rsi",
               "stunnable.rsi", "shuttle.rsi", "stealthy.rsi", "changeling.rsi", "ninja_power.rsi",
               "chemicals_counter.rsi", "essence_counter.rsi", "generic_counter.rsi"):
    entries.append({"target": A + target, "panel_all": True, "standin": True, "states": {}})
for png in ("Abilities/silenced.png", "Buckle/buckled.png", "Handcuffed/Handcuffed.png", "Rooted/rooted.png",
            "Weightless/weightless.png"):
    file(A + png, {"layers": [eris("block"), {"file_png": A + png, "recolor": "eris_ui"}]}, True)

# --- Window chrome (Interface/Nano) -----------------------------------------
N = "Interface/Nano/"
for name in ("black_panel_dark_thin_border", "black_panel_light_thin_border", "chat_sub_background",
             "light_panel_background_bordered", "lineedit", "tabcontainer_panel", "tooltip",
             "transparent_window_background_bordered", "whisper", "window_background", "window_background_bordered"):
    file(f"{N}{name}.png", {"file_png": f"{N}{name}.png", "recolor": "eris_nano"}, True)
file(f"{N}window_header.png", {"file_png": f"{N}window_header.png", "recolor": "eris_header"}, True)

# --- Radial menus ---------------------------------------------------------
R = "icons/mob/radial/menu.dmi"
file("Interface/Radial/button_normal.png", eris("radial_slice", R))
file("Interface/Radial/button_hover.png", eris("radial_slice_focus", R))
file("Interface/Radial/back_normal.png", eris("radial_center", R))
file("Interface/Radial/back_hover.png", eris("radial_center_focus", R))
file("Interface/Radial/close_normal.png", eris("radial_center", R))
file("Interface/Radial/close_hover.png", eris("radial_center_focus", R))

# --- HUD icons (job, security, health) --------------------------------------
H = "icons/mob/hud.dmi"
jobs = {
    "Captain": "hudcaptain", "HeadOfPersonnel": "hudfirstofficer", "Nanotrasen": "hudcentcom", "Admin": "hudcentcom",
    "ChiefEngineer": "hudtechnomancerexultant", "AtmosphericTechnician": "hudatmospherictechnomancer",
    "StationEngineer": "hudtechnomancer", "SeniorEngineer": "hudtechnomancer", "TechnicalAssistant": "hudtechnomancer",
    "ERTEngineer": "hudtechnomancer",
    "ResearchDirector": "hudmoebiusexpeditionoverseer", "Scientist": "hudmoebiusscientist",
    "SeniorResearcher": "hudmoebiusscientist", "ResearchAssistant": "hudmoebiusscientist",
    "Roboticist": "hudmoebiusroboticist", "ChiefMedicalOfficer": "hudmoebiusbiolabofficer",
    "MedicalDoctor": "hudmoebiusdoctor", "SeniorPhysician": "hudmoebiusdoctor", "MedicalIntern": "hudmoebiusdoctor",
    "ERTMedical": "hudmoebiusdoctor", "Chemist": "hudmoebiuschemist", "Paramedic": "hudmoebiusparamedic",
    "Psychologist": "hudmoebiuspsychiatrist", "Geneticist": "hudmoebiusgeneticist", "Virologist": "hudmoebiusvirologist",
    "Chaplain": "hudneotheologypreacher", "ERTChaplain": "hudneotheologypreacher", "Botanist": "hudneotheologyagrolyte",
    "Janitor": "hudneotheologycustodian", "ERTJanitor": "hudneotheologycustodian",
    "QuarterMaster": "hudguildmerchant", "CargoTechnician": "hudguildtechnician", "SeniorCourier": "hudguildtechnician",
    "ShaftMiner": "hudguildminer",
    "HeadOfSecurity": "hudironhammercommander", "ERTLeader": "hudironhammercommander",
    "SecurityOfficer": "hudironhammeroperative", "SecurityCadet": "hudironhammeroperative",
    "SeniorOfficer": "hudironhammeroperative", "ERTSecurity": "hudironhammeroperative",
    "Detective": "hudironhammerinspector", "Warden": "hudironhammergunnerysergeant",
    "Brigmedic": "hudironhammermedicalspecialist",
    "Bartender": "hudclubworker", "Chef": "hudclubworker", "Musician": "hudclubworker", "Boxer": "hudclubworker",
    "ServiceWorker": "hudclubworker", "Reporter": "hudclubworker", "Zookeeper": "hudclubworker",
    "TramDriver": "hudguildtechnician",
    "Librarian": "hudlibrarian", "Lawyer": "hudlawyer", "Mime": "hudmime", "Clown": "hudguildartist",
    "Passenger": "hudvagabond", "Visitor": "hudvagabond", "NoId": "hudunknown", "Unknown": "hudunknown",
    "Ninja": "hudninja", "DeathSquad": "huddeathsquad", "Syndicate": "hudsyndicate", "SyndicateCommander": "hudoperative",
    "SyndicateOperative": "hudoperative", "SyndicateCorpsman": "hudoperative", "Wizard": "hudwizard",
    "HeadRevolutionary": "hudheadrevolutionary", "Revolutionary": "hudrevolutionary", "Prisoner": "hudprisoner",
    "MindShield": "hud_imp_loyal", "Pirate": "hudmercenary", "InitialInfected": "hudbrainworm", "Zombie": "hudbrainworm",
    "Borg": "hudmalborg", "StationAi": "hudmalai", "Sus": "hudunknown1", "ParadoxClone": "hudunknown1",
    "CBURN": "huddeathsquad", "Cluwne": "hudguildartist",
}
rsi("Interface/Misc/job_icons.rsi", {k: eris(v, H) for k, v in jobs.items()})
rsi("Interface/Misc/security_icons.rsi", {
    "hud_wanted": eris("hudwanted", H), "hud_incarcerated": eris("hudprisoner", H),
    "hud_paroled": eris("hudparolled", H), "hud_discharged": eris("hudreleased", H),
    "hud_suspected": eris("hudwanted", H, tint="#ffd000"), "hud_hostile": eris("hudwanted", H),
    "hud_eliminated": eris("huddead", H)})
rsi("Interface/Misc/health_icons.rsi", {"Fine": eris("hudhealthy", H), "Critical": eris("hudill", H),
                                         "Dead": eris("huddead", H), "Rotting": eris("huddead", H)})
rsi("Interface/Misc/pointing.rsi", {"pointing": eris("arrow")})

# --- Combat mode buttons ----------------------------------------------------
file("Interface/Actions/harm.png", eris("harm"))
file("Interface/Actions/harmOff.png", eris("help"))
file("Interface/Actions/disarm.png", eris("disarm"))
file("Interface/Actions/disarmOff.png", eris("help"))

# --- Zone doll and progress bar ---------------------------------------------
# Their geometry drives hit-testing and fill offsets in C#, so they keep their shape in Eris HUD colours.
for name in ("target-doll.svg.192dpi", "target-doll-high.svg.192dpi", "target-doll-high.svg.96dpi",
             "target-doll-middle.svg.192dpi", "target-doll-middle.svg.96dpi", "target-doll-low.svg.192dpi",
             "target-doll-low.svg.96dpi"):
    file(f"Interface/{name}.png", {"file_png": f"Interface/{name}.png", "recolor": "eris_ui"}, True)
rsi("Interface/Misc/progress_bar.rsi",
    {"icon": {"rsi": "Interface/Misc/progress_bar.rsi", "state": "icon", "base": True, "recolor": "eris_ui"}}, True)

(HERE / "maps").mkdir(exist_ok=True)
(HERE / "maps" / "ui.json").write_text(json.dumps(entries, indent=1) + "\n", encoding="utf-8")
print(f"wrote {len(entries)} UI entries")
