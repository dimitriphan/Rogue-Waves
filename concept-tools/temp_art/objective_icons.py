"""Temporary art: the icons of the five optional objectives (docs/scope/14 Look lines: "one found object for its icon,
readable at 12x12", docs/scope/15's template), 12 x 12 in b11_icons.py's palette and rules (opaque, no outline, lit from
the upper left). The Board card and the DONE popup show RogueWaves_Objective_<Name> (19_Objectives.cs `RWObjectives.Icon`).

    python3 concept-tools/temp_art/objective_icons.py [contact.png] [--force]
        -> TDS_Mod/ModSource/RogueWavesMode/Art/Temporary/game_size/RogueWaves_Objective_<Name>.png, preview_8x/,
           aseprite/RogueWaves_Icons_Objectives.aseprite

The names are RWObjectives.All's (Meteor, Hull, Clock, Hands, Nest). A name already in game_size/ is kept unless --force.
"""
import sys
from b11_icons import build as _build, contact as _contact

ICONS = {
    'RogueWaves_Objective_Meteor': [         # Mind the Meteor: a node with a white crystal (a tall one and a small one on grey rock)
        "......w.....",
        ".....wPp....",
        ".....wPp....",
        "....wwPpp...",
        "....wPPpp...",
        "....wPPpp.w.",
        ".w..wPPppwPp",
        "wPp.wPPppwPp",
        "wPpiiiiggggp",
        "iiiiigggghhh",
        ".igggggghhh.",
        "..hhhhhhhh..",
    ],
    'RogueWaves_Objective_Hull': [           # Keep the Basin Clean: a mop standing in a coral bucket with its wire handle
        "..........bB",
        ".........bB.",
        "........bB..",
        "...hhhhbBh..",
        "..h...bB..h.",
        "..hFfFbBfFh.",
        ".FfFfFfFfFf.",
        ".kkkCCCCCCc.",
        "..kcccccccc.",
        "..kCCCCCCCc.",
        "...kCCCCCc..",
        "...cccccc...",
    ],
    'RogueWaves_Objective_Clock': [          # Beat the Clock: a cracked stopwatch, brass crown, the crack down the glass's left
        ".....YZ.....",
        "....ZooZ.o..",
        "...iiiiggo..",
        "..iFFFFFfg..",
        ".igFFFeFFfg.",
        ".iFgFFeFFfg.",
        ".iFgFFeeeFh.",
        ".igFFFFFFfh.",
        ".iFgFFFFFfh.",
        "..gFgFFffh..",
        "...ghhhhh...",
        "............",
    ],
    'RogueWaves_Objective_Hands': [          # Mind the Vessels: a life ring afloat, half in the water, with its rope
        "....CCFF....",
        "..kCCcFFFf..",
        ".kCcS..SFf..",
        ".CcS....SFf.",
        "FFS......Cc.",
        "FFs......CC.",
        "FFf......Cc.",
        "lFFs....sCcl",
        "TlFFfsscCcTT",
        "TTTlFCCcTTlT",
        "tTTTTTTTTtTT",
        "DDtDDDDDtDDD",
    ],
    'RogueWaves_Objective_Nest': [           # Raze the Nest: a twig nest on fire
        ".....Y......",
        "....yY...Y..",
        "..Y.yOy..y..",
        "..yOyOOy.Oy.",
        ".yOOrOOyOOy.",
        ".OOrrQOOrQO.",
        "SbSsbSbSsbSs",
        "bSsBbsSbBsSb",
        ".BbsBbBsbBb.",
        "..BBbBBbBB..",
        "...BBBBBB...",
        "............",
    ],
}


if __name__ == '__main__':
    force = '--force' in sys.argv
    args = [a for a in sys.argv[1:] if a != '--force']
    w = _build(force=force, icons=ICONS, ase='RogueWaves_Icons_Objectives.aseprite')
    print('wrote', len(w), 'icons')
    if args:
        _contact(args[0], icons=ICONS)
