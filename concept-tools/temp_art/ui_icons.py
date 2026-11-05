"""Temporary art: the Rogue Waves mode's 12 x 12 icons (Icon_Types and Icon_Stats size, kit docs/03 section 2), plus the
crest at 32 x 32 for the main menu's Rogue Waves button. Pack palette and motifs from docs/STYLE.md; objects from
docs/scope (04 weather Looks, 11 Directions' Looks, 12 Vampire Dentures). Opaque, no outline, lit from the upper left.

    python3 concept-tools/temp_art/ui_icons.py   -> TDS_Mod/ModSource/RogueWavesMode/Art/Temporary/
Also prints each icon as the string rows RogueWavesMode's 08_Icons.cs draws (the weather three are wired there).
"""
import os
import sys
from tart import Canvas, H, save_png, save_ase, preview, REPO

PAL = {
    # docs/STYLE.md pack palette and its shades (shade keeps the hue, kit docs/18 section 3)
    'D': '2E4A62', 'd': '22384B', 'T': '3F8F96', 't': '327378', 'l': '6DB4B9', 'F': 'E9F1EE', 'f': 'C3D8D3',
    'C': 'E2725B', 'c': 'B55240', 'k': 'EE9A86', 'S': 'D8C49A', 's': 'A8936A', 'P': 'F4EEF8', 'p': 'C9E4F2',
    'q': 'F2D9E6',
    # shared world palette (kit docs/18 section 3.6)
    'w': 'FFFFFF', 'G': 'DEDEDE', 'g': '939292', 'h': '6D6C6C', 'e': '333232', 'b': '826D60', 'B': '64544A',
    'y': 'F6E43B', 'Y': 'FFF48B', 'o': 'D9C52E', 'r': 'D63A44', 'R': 'A12D37', 'K': 'DA867A', 'L': '7BCF80',
    'M': '65AD69', 'u': '97AFE5', 'U': '4764A4', 'a': 'C5B294', 'A': 'DFC9A8', 'm': '5A4C42',
    # Spool's own (docs/STYLE.md roster; his purple stays as is)
    'v': '7E4D90', 'V': '633773', 'i': 'B5B1B0', 'z': 'BFBA3F', 'n': 'D77BBA', 'x': '6FAE7E', 'X': '4F8C5E',
    'j': '9AC9A5',
}
PAL = {k: H(v) for k, v in PAL.items()}

ICONS = {
    # ---- sea weather (docs/scope/04 Looks); wired: RogueWavesMode/Code/08_Icons.cs
    'RogueWaves_RisingWater': [   # a teal swell with a foam crest, and the rising mark beside it
        ".F..........",
        "FFF...FFF...",
        ".F...FFFfF..",
        ".F..FlTTTfF.",
        ".F.FlTTTTTf.",
        "..FlTTTTTTTt",
        "..lTTTTTTTTt",
        "FFlTTTTTTTtt",
        "TTTTTTTTTTTt",
        "tTTTtTTTTtTt",
        "DtDDDtDDDtDD",
        "dDDdDDDdDDDd",
    ],
    'RogueWaves_Quickening': [    # three foam chevrons over the running water
        "............",
        "FF..FF..FF..",
        ".FF..FF..FF.",
        "..Ff..Ff..Ff",
        ".Ff..Ff..Ff.",
        "Ff..Ff..Ff..",
        "............",
        "lTTTlTTTTlTT",
        "TTlTTTTlTTTt",
        "TTTTTlTTTTTt",
        "tTtTTTtTTtTt",
        "DDDDDDDDDDDD",
    ],
    'RogueWaves_Nesting': [       # a coral egg in a sand nest
        "............",
        ".....kC.....",
        "....kCCC....",
        "...kCCCCc...",
        "...CCCCCc...",
        "...CCCCCc...",
        "...CCCCcc...",
        ".SsSCCcccSs.",
        "SSSSsSSSSsSs",
        "sSSSSSSSSSSs",
        ".sSsSSsSSs..",
        "..ssssssss..",
    ],
    # ---- the mode
    'RogueWaves_Crest': [         # the pack's mark: one breaking curl with a Foam rim
        "....FFFFF...",
        "..FFTTTTTF..",
        ".FTTTDDDTTF.",
        "FTTD...FFTF.",
        "FTD...FTTt..",
        "FTD...FTt...",
        "FTTD...t....",
        "FTTTD.......",
        "FTTTTDD.....",
        "FlTTTTTDDD..",
        "lTTTTTTTTTTt",
        "DDDDDDDDDDDD",
    ],
    'RogueWaves_PureAura': [      # a Pearl drop with its two-tone sheen
        ".....P......",
        ".....PP.....",
        "....PPPp....",
        "....PPPp....",
        "...PPPPPp...",
        "..PwPPPPqp..",
        "..PwPPPPqp..",
        ".PPwPPPqqpp.",
        ".PPPPPPqqpp.",
        "..PPPPqqpp..",
        "..ppPqqppp..",
        "....pppp....",
    ],
    'RogueWaves_Depth': [         # Depth: a coral sounding lead on its line
        ".....F......",
        ".....f......",
        ".....F......",
        "....FfF.....",
        "....kCc.....",
        "....CCc.....",
        "...kCCCc....",
        "...CCCCc....",
        "..kCCCCcc...",
        "..CCCCCcc...",
        "..CCCCccc...",
        "...ccccc....",
    ],
    'RogueWaves_Retry': [         # a retry: a life ring, coral and foam
        "....CCCC....",
        "..FFCCCCFF..",
        ".FFFfccFFFF.",
        ".FFf....FFF.",
        "CCf......FCc",
        "CCc......CCc",
        "CCC......CCc",
        "CCF......Fcc",
        ".FFF....FFf.",
        ".FFFFCCFFff.",
        "..FfcCCcff..",
        "....cccc....",
    ],
    'RogueWaves_Vote': [          # the chart vote: a teal pennant on a pole
        "..h.........",
        "..GTTT......",
        "..GTTTTT....",
        "..GTlTTTTT..",
        "..GTTTTTt...",
        "..GTTtt.....",
        "..Gt........",
        "..G.........",
        "..G.........",
        "..g.........",
        ".SSs........",
        "SSSss.......",
    ],
    # ---- Directions (docs/scope/11 Looks)
    'Direction_Tougher': [        # Made of Tougher Stuff: a Stitched with a bandaid, holding back tears
        "...vvvvvv...",
        "..AKAvvvvV..",
        ".vvAKAvvvvV.",
        ".vvvvvvvvvV.",
        ".vzzvvvvzzV.",
        ".vzevvvvzeV.",
        ".vuvvvvvvuV.",
        ".vuvvvvvvVV.",
        ".vviiiiiivV.",
        "..vVvVvVVV..",
        "..wwvVVww...",
        "..ww....ww..",
    ],
    'Direction_CompostBin': [     # Compost Bin: a small grey bin of dirt with stink lines
        "..L...L..L..",
        ".L...L..L...",
        "..L...L..L..",
        "............",
        ".GGGGGGGGGg.",
        ".mbBmbBmbBg.",
        "..GGGGGGGg..",
        "..GgGGgGGg..",
        "..GgGGgGgg..",
        "..GgGGgGgg..",
        "..GGGGGGgg..",
        "...ggggggg..",
    ],
    'Direction_DandelionSeason': [  # Dandelion Season: a dandelion flower on its stem and jagged leaf
        "...y.yy.y...",
        "..yYyYYyoy..",
        ".yYYYyYYyoy.",
        "..yYyoyyoyo.",
        ".yoyooooyo..",
        "...o.oo.o...",
        ".....M......",
        "..L..M......",
        ".LL.MM..L...",
        "..LLM..LL...",
        "...LMLLL....",
        "....MM......",
    ],
    'Direction_MotherOfMushrooms': [  # Mother of Mushrooms: a mushroom growing kit, a red cap over its box
        "....rrrr....",
        "..rrwrrrrR..",
        ".rwwrrrwrRR.",
        ".rrrrrrrrRR.",
        "...AaAAaA...",
        "....AAAa....",
        ".SSSSAASSSs.",
        ".SbBbBbBbSs.",
        ".SSSSSSSSSs.",
        ".SSssSSSSSs.",
        ".SSSSSSSSss.",
        "..ssssssss..",
    ],
    'Direction_EfficientRecycler': [  # Efficient Recycler: the recycling arrows, made of a string of yarn
        ".....xj.....",
        "...xxjxxj...",
        "..xxjxxjxxx.",
        ".xxj....jxx.",
        ".xj......x..",
        "xjx......XXX",
        "jxx......XXX",
        ".x.......XX.",
        "xjx.....XXX.",
        "jxx.XXXXXX..",
        "...XXXXXX...",
        ".....XX.....",
    ],
    'Direction_SewingMachine': [  # Sewing Machine: the classic light blue machine
        "............",
        ".uuuuuuuuu..",
        ".uwwuuuuuuU.",
        ".uuuuuuuuuU.",
        ".uU.....UuU.",
        ".UU.....iUU.",
        ".UU.....i...",
        "........G...",
        "..uuuuuuuuu.",
        ".uwwuuuuuuuU",
        ".UUUUUUUUUUU",
        "..hh....hh..",
    ],
    # ---- Trinkets (docs/scope/12; the file names keep Cardinal_)
    'Cardinal_VampireDentures': [  # Vampire Dentures: cheap plastic fangs on pink gums
        "............",
        "..rrrrrrrr..",
        ".rrrrrrrrrR.",
        "rrkkkkkkkkrR",
        "rwwGwwwwGwwR",
        "rwwGwwwwGwwR",
        ".wGwwwwwwGw.",
        "..w......w..",
        "..wwwwwwww..",
        ".rwwGwwGwwR.",
        ".rrRRRRRRRR.",
        "..RRRRRRRR..",
    ],
}

# Spool's ground Thread (temporary art for SpoolClient's 8 x 8 tuft): a fluffed tuft of lilac thread with a loose end.
TUFT = [
    "............",
    "....jjx.....",
    "..jjjxjxj...",
    ".jxjjxxjxx..",
    ".jjxjxjxxX..",
    "jxjjxxjxXX..",
    ".jxjxxxXxX..",
    ".xjxxXxXXX..",
    "..XxXXXXX...",
    "...XXXX..X..",
    ".........X..",
    "..........X.",
]


def crest32():
    """The crest at 32 x 32 for the main menu's Rogue Waves button: the curl drawn at 32 px, not scaled."""
    rows = [
        "................................",
        "...........FFFFFFFF.............",
        "........FFFFTTTTTTFFF...........",
        "......FFTTTTTTTTTTTTFFF.........",
        ".....FTTTTTlllTTTTTTTTFF........",
        "....FTTTTllTTTTTDDDTTTTFF.......",
        "...FTTTlTTTTTDD....DDTTTFF......",
        "..FTTTlTTTTDD.......FFDTTTF.....",
        "..FTTlTTTTD.......FFTTTFTTF.....",
        ".FTTlTTTTD.......FTTTTTtFF......",
        ".FTTTTTTD.......FTTTTtt.........",
        "FTTlTTTTD.......FTTTtt..........",
        "FTTTTTTTD........FTtt...........",
        "FTTlTTTTD.........tt............",
        "FTTTTTTTTD......................",
        "FTTTTTTTTD......................",
        "FlTTTTTTTTD.....................",
        "FTTTTTTTTTTD....................",
        "FlTTTTTTTTTTDD..................",
        "FTTTTTTTTTTTTTDD................",
        "FlTTTTTTTTTTTTTTDDD.........F...",
        "FTlTTTTTTTTTTTTTTTTDDDD....FTF..",
        "FTTlTTTTTTTTTTTTTTTTTTTDDDDTTTF.",
        "lTTTlTTTTTTTTTTTTTTTTTTTTTTTTTtF",
        "TTTTTTlTTTTTTTTTTTTTTTTTTTTTTTt.",
        "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTtt",
        "tTTTTTtTTTTTTTtTTTTTTTtTTTTTTTTt",
        "DtTTTTDtTTTTTTDtTTTTTTDtTTTTTDtD",
        "DDttTDDDttTTTDDDttTTTDDDttTTDDDD",
        "dDDDDDdDDDDDDdDDDDDDDdDDDDDDdDDd",
        "ddDDDddDDDDDddDDDDDDddDDDDDDddDd",
        "dddddddddddddddddddddddddddddddd",
    ]
    return rows


def check(name, rows, size):
    bad = [i for i, r in enumerate(rows) if len(r) != size]
    if len(rows) != size or bad:
        raise SystemExit('%s: %d rows, bad row lengths at %s' % (name, len(rows), bad))
    miss = {ch for r in rows for ch in r if ch not in PAL and ch not in '. '}
    if miss:
        raise SystemExit('%s: unknown colours %s' % (name, ''.join(sorted(miss))))


def canvas(rows):
    c = Canvas(len(rows[0]), len(rows))
    c.stamp(rows, PAL)
    return c


def build(out=None):
    out = out or os.path.join(REPO, 'TDS_Mod', 'ModSource', 'RogueWavesMode', 'Art', 'Temporary')
    names = list(ICONS)
    for n in names:
        check(n, ICONS[n], 12)
        c = canvas(ICONS[n])
        save_png(c, os.path.join(out, 'game_size', n + '.png'))
        save_png(preview(c, 12, 8), os.path.join(out, 'preview_8x', n + '_8x.png'))
    # one .aseprite holding every icon, a tag each, to hand-edit them together
    frames = [canvas(ICONS[n]) for n in names]
    save_ase(os.path.join(out, 'aseprite', 'RogueWaves_Icons.aseprite'), 12, frames,
             [(n, i, i, 'TEMP;icon=%s.png' % n) for i, n in enumerate(names)])
    big = crest32()
    check('RogueWaves_Crest32', big, 32)
    c = canvas(big)
    save_png(c, os.path.join(out, 'game_size', 'RogueWaves_Crest32.png'))
    save_png(preview(c, 32, 8), os.path.join(out, 'preview_8x', 'RogueWaves_Crest32_8x.png'))
    save_ase(os.path.join(out, 'aseprite', 'RogueWaves_Crest32.aseprite'), 32, [c], [('crest', 0, 0, 'TEMP;icon=RogueWaves_Crest32.png')])
    # Spool's tuft goes to Spool's own temp folder
    sp = os.path.join(REPO, 'TDS_Mod', 'ModSource', 'Spool', 'Art', 'Temporary')
    check('Spool_Tuft', TUFT, 12)
    t = canvas(TUFT)
    save_png(t, os.path.join(sp, 'game_size', 'Spool_Tuft.png'))
    save_png(preview(t, 12, 8), os.path.join(sp, 'preview_8x', 'Spool_Tuft_8x.png'))
    save_ase(os.path.join(sp, 'aseprite', 'Spool_Tuft.aseprite'), 12, [t], [('tuft', 0, 0, 'TEMP;icon=Spool_Tuft.png')])
    return names


def contact(names, path):
    """All icons side by side at 8x with their names, for review (not shipped)."""
    from PIL import Image, ImageDraw
    k, pad = 8, 10
    items = [(n, ICONS[n]) for n in names] + [('Spool_Tuft', TUFT)]
    cols = 5
    cw = 12 * k + 2 * pad
    ch = 12 * k + 2 * pad + 14
    img = Image.new('RGB', (cols * cw, ((len(items) + cols - 1) // cols) * ch), (138, 153, 138))
    d = ImageDraw.Draw(img)
    for i, (n, rows) in enumerate(items):
        x0, y0 = (i % cols) * cw + pad, (i // cols) * ch + pad
        for y, r in enumerate(rows):
            for x, chh in enumerate(r):
                if chh in PAL:
                    d.rectangle([x0 + x * k, y0 + y * k, x0 + x * k + k - 1, y0 + y * k + k - 1], fill=PAL[chh][:3])
        d.text((x0, y0 + 12 * k + 2), n.replace('RogueWaves_', '').replace('Direction_', 'D: ').replace('Cardinal_', 'C: '), fill=(30, 30, 30))
    img.save(path)


if __name__ == '__main__':
    names = build()
    if len(sys.argv) > 1:
        contact(names, sys.argv[1])
    for n in ('RogueWaves_RisingWater', 'RogueWaves_Quickening', 'RogueWaves_Nesting'):
        print(n)
        print('\n'.join('        "%s",' % r for r in ICONS[n]))
