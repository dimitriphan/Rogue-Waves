"""Three alternative versions of each of the mode's temporary 12 x 12 icons and the 32 x 32 crest (2026-10-28), for
Dimitri to pick one per icon. Same rules and palette as ui_icons.py (docs/STYLE.md pack palette, the world palette,
Spool's purple; opaque, no outline, lit from the upper left); same names and sizes, so a pick drops in over
Art/Temporary/game_size/<name>.png, which stays as it is until he chooses. Each version is a different idea for the
same object, not a recolour.

    python3 concept-tools/temp_art/variants_icons.py   -> ModSource/RogueWavesMode/Art/Temporary/variants/v1..v3/
Prints the three weather icons' rows for each version (the form RogueWavesMode's 08_Icons.cs uses).
"""
import math
import os
from tart import Canvas, Mask, H, shade, save_png, save_ase, preview, REPO
from ui_icons import PAL, check, canvas

TEMP = os.path.join(REPO, 'TDS_Mod', 'ModSource', 'RogueWavesMode', 'Art', 'Temporary')

V = {
    # ---------------------------------------------------------------------------------------- weather (scope 04)
    'RogueWaves_RisingWater': [
        ('Tide gauge', [          # a striped gauge post, the water halfway up it, the rising arrow
            "......F.....",
            ".....FFF....",
            "....FFFFF...",
            ".GGg..F.....",
            ".rRg..F.....",
            ".GGg..F.....",
            ".rRg........",
            ".GGg........",
            "FlTlFFFlTTFF",
            "TTTTTTTTTTTt",
            "tTTtTTTtTTTt",
            "DDDDDDDDDDDD",
        ]),
        ('Water up the steps', [  # sand steps going up, the water over the lower ones, an arrow
            "..F.........",
            ".FFF........",
            "FFFFF.......",
            "..F.....SSSs",
            "..F.....Ssss",
            "......SSSsss",
            "......Ssssss",
            "....SSSsssss",
            "FfFlTTTTTTTt",
            "lTTTTTTTTTTt",
            "TTTTtTTTTtTt",
            "DDDDDDDDDDDD",
        ]),
        ('Rising column', [       # a column of water with a foam arrow inside, rising out of the sea
            "....FFFF....",
            "...FlTTTF...",
            "..FlTTFTTt..",
            "..lTTFFFTt..",
            "..TTFFFFFt..",
            "..TTTTFTTt..",
            "..TTTTFTTt..",
            "..TTTTFTTt..",
            "FFlTTTTTTtFF",
            "TTTTTTTTTTTt",
            "tTTtTTTtTTTt",
            "DDDDDDDDDDDD",
        ]),
    ],
    'RogueWaves_Quickening': [
        ('Racing streaks', [      # foam streaks racing over the water
            "............",
            "FFFFFF......",
            "..fFFFFFFF..",
            "......FFFFFF",
            "............",
            "lTTTTlTTTTTT",
            "TTFFFFTTTTTt",
            "TTTTTTTFFFFt",
            "TFFFTTTTTTTt",
            "tTTTTtTTTTtt",
            "DDDDDDDDDDDD",
            "dDDdDDDdDDDd",
        ]),
        ('Whirlpool', [           # the current turning on itself
            "...lTTTTt...",
            ".lTTFFFFTTt.",
            ".TTFTTTTFTt.",
            "lTFTTFFTTFTt",
            "TTFTFTTFTFTt",
            "TTFTFTDFTFTt",
            "TTFTTFFTTFTt",
            "TTTFTTTTFTTt",
            ".TTTFFFFTTt.",
            ".tTTTTTTTtt.",
            "...tttttt...",
            "............",
        ]),
        ('Foam arrow', [          # a big foam arrow riding the water, speed lines behind
            "............",
            "......FF....",
            "......FFF...",
            "FF.FFFFFFF..",
            "...FFFFFFFf.",
            "FF.FFFFFFf..",
            "......FFf...",
            "......Ff....",
            "lTTTTTTlTTTT",
            "TTTTlTTTTTTt",
            "tTTTTTTtTTTt",
            "DDDDDDDDDDDD",
        ]),
    ],
    'RogueWaves_Nesting': [
        ('Two eggs in the sand', [
            "............",
            "............",
            "..kC........",
            ".kCCc..kC...",
            ".CCCc.kCCc..",
            ".CCcc.CCCc..",
            "SCccsSCCcc..",
            "SSssSSCccsSs",
            "SSSSSSsssSSs",
            "sSSSsSSSSSss",
            ".sSSSSsSSss.",
            "...ssssss...",
        ]),
        ('Speckled egg in twigs', [
            ".....FF.....",
            "....FfFF....",
            "...FFFCfF...",
            "...FCFFFf...",
            "...FFFFCf...",
            "...FfFFFf...",
            "..bFFffffb..",
            ".bBbbBbbBbB.",
            "bBbSSSSSsbBb",
            ".bBbBbbBbBb.",
            "..BbBbbBbB..",
            "............",
        ]),
        ('Hatching', [            # the shell's top lifted off, a teal hatchling peeking out
            "...kCCc.....",
            "..kCCCCc....",
            "............",
            "...lTTTt....",
            "..lTeTTtt...",
            "..kCkCkCc...",
            "..CCCCCCc...",
            "..CCCCCcc...",
            ".SSCCCcccSs.",
            "SSSScccSSSSs",
            "sSSSSSSSSSss",
            "..ssssssss..",
        ]),
    ],
    # ------------------------------------------------------------------------------------------------ the mode
    'RogueWaves_Crest': [
        ('Curl in a badge', [
            "...dDDDDd...",
            "..DDDDDDDd..",
            ".DDDFFFDDDd.",
            "DDDFlTTFDDDd",
            "DDFlTDDTFDDd",
            "DDFTDDDFFDDd",
            "DDFTTDDDDDDd",
            "DDFTTTDDDDFd",
            "DlTTTTTTTTTd",
            ".TTTTTTTTTt.",
            "..ttTTTTtt..",
            "...tttttt...",
        ]),
        ('Twin curls', [
            "............",
            "..FFF...FFF.",
            ".FTTTF.FTTTF",
            "FTD.FFFTD.FF",
            "FTD..FTD....",
            "FTTD.FTTD...",
            "FTTTDFTTTD..",
            "lTTTTlTTTTDD",
            "TTTTTTTTTTTt",
            "TTlTTTTTlTTt",
            "tTTTtTTTTTtt",
            "DDDDDDDDDDDD",
        ]),
        ('Curl before the sun', [
            "......kCCc..",
            ".....kCCCCc.",
            "....FFFFCCc.",
            "..FFTTTTFcc.",
            ".FTTDDDTTF..",
            "FTTD...FTF..",
            "FTD...FTt...",
            "FTD....t....",
            "FTTD........",
            "FlTTDDD.....",
            "lTTTTTTTTTTt",
            "DDDDDDDDDDDD",
        ]),
    ],
    'RogueWaves_PureAura': [
        ('Pearl in a clam', [
            "............",
            "..SSSSSSSs..",
            ".SAaSAaSAas.",
            ".SSSSSSSSSs.",
            "..sssssssss.",
            "....PPPp....",
            "...PwPPPp...",
            "...PPPPqp...",
            ".SSSPqqppSs.",
            "SAaSSppSSAas",
            ".SSSSSSSSss.",
            "..ssssssss..",
        ]),
        ('Pearl motes', [
            "..P.........",
            ".PwP...Pp...",
            "..P...PwPp..",
            "......PPqp..",
            ".......pp...",
            "...PPp......",
            "..PwPPp.....",
            "..PPPqp...P.",
            "..PPqqp..PwP",
            "...pppp...P.",
            "............",
            "............",
        ]),
        ('Glowing orb', [
            "....PPPP....",
            "..PPwwPPPp..",
            ".PPwPPPPPqp.",
            ".PwPPPPPqqp.",
            "PPPPPPPPqqpp",
            "PPPPPPPqqqpp",
            "PPPPPPqqqqpp",
            "PPPPPqqqqppp",
            ".PPPqqqqppp.",
            ".pPqqqpppp..",
            "..pppppppp..",
            "....pppp....",
        ]),
    ],
    'RogueWaves_Depth': [
        ('Anchor', [
            ".....GG.....",
            "....G..g....",
            ".....Gg.....",
            "...GGGGgg...",
            ".....Gg.....",
            ".....Gg.....",
            ".G...Gg...g.",
            ".GG..Gg..gg.",
            "..GG.Gg.gg..",
            "...GGGGgg...",
            ".....gg.....",
            "............",
        ]),
        ('Darkening water', [     # bands of water darkening downward, a foam arrow going down
            "lllllFllllll",
            "llllFFFlllll",
            "TTTTTFTTTTTt",
            "TTTTTFTTTTTt",
            "ttttTFTttttt",
            "tttFFFFFtttt",
            "DDDDFFFDDDDD",
            "DDDDDFDDDDDD",
            "ddddddddddDd",
            "dddddddddddd",
            "dDddddddDddd",
            "dddddddddddd",
        ]),
        ('Diving bell', [
            ".....g......",
            ".....g......",
            "....SSSs....",
            "...SSSSss...",
            "..SSpppSss..",
            "..SpwppSss..",
            "..SppppSss..",
            "..SSpppSss..",
            ".SSSSSSSsss.",
            ".ssssssssss.",
            "..g......g..",
            "............",
        ]),
    ],
    'RogueWaves_Retry': [
        ('Round arrow', [
            "...kCCCC....",
            "..kCc..cC...",
            ".kC......C..",
            ".C.....CCCCc",
            "kC......CCc.",
            "CC.......c..",
            "CC..........",
            "CC.......Cc.",
            ".Cc.....Cc..",
            ".cCc...CCc..",
            "..cCCCCCc...",
            "....cccc....",
        ]),
        ('Coral heart', [
            "............",
            ".kCC...kCC..",
            "kCkCc.kCCCc.",
            "CkCCCCCCCCc.",
            "CCCCCCCCCcc.",
            "CCCCCCCCccc.",
            ".CCCCCCccc..",
            "..CCCCccc...",
            "...CCccc....",
            "....ccc.....",
            ".....c......",
            "............",
        ]),
        ('Buoy', [
            ".....g......",
            ".....g......",
            "....FFF.....",
            "...kCCCc....",
            "...FFFFf....",
            "..kCCCCCc...",
            "..FFFFFFf...",
            "..CCCCCcc...",
            "lTTTTTTTTTTt",
            "TTTlTTTTlTTt",
            "tTTTTtTTTTtt",
            "DDDDDDDDDDDD",
        ]),
    ],
    'RogueWaves_Vote': [
        ('Chart pin', [
            "....kCCc....",
            "...kCCCCc...",
            "...CCFFCc...",
            "...CCFFCc...",
            "...CCCCcc...",
            "....CCcc....",
            ".....Cc.....",
            "..SSSccSSs..",
            ".SSAaSSAaSs.",
            ".SSSSSSSSSs.",
            "..ssssssss..",
            "............",
        ]),
        ('Two pennants', [
            "h.........h.",
            "GTTT....CCG.",
            "GTTTTT.CCCG.",
            "GTlTt..kCcG.",
            "GTt.....CcG.",
            "G.........G.",
            "G.........G.",
            "G.........G.",
            "G.........G.",
            "g.........g.",
            "SSs......SSs",
            "sss......sss",
        ]),
        ('Shell in the box', [
            ".....kC.....",
            "....kCCc....",
            ".....Cc.....",
            "............",
            "..SSSeeSSs..",
            ".SSSSSSSSSs.",
            ".SAAAAAAASs.",
            ".SAaaaaaaSs.",
            ".SAaaaaaaSs.",
            ".SSSSSSSSss.",
            ".ssssssssss.",
            "............",
        ]),
    ],
    # ---------------------------------------------------------------------------------------- Directions (scope 11)
    'Direction_Tougher': [
        ('Cross-stitched patch', [   # a Stitched with a thread-patched brow, a determined squint
            "...vvvvvv...",
            "..vvvjjjjV..",
            ".vvvvjxjxjV.",
            ".vvvvjjxjjV.",
            ".vvvvjxjxVV.",
            ".vzzvvvvzzV.",
            ".vvevvvvveV.",
            ".vvvvvvvvVV.",
            ".vviiiiiivV.",
            "..vVvVvVVV..",
            "..wwvVVww...",
            "..ww....ww..",
        ]),
        ('Felt shield', [            # a purple felt shield with a white cross-stitch
            ".vvvvvvvvvV.",
            ".vjjvvvvvVV.",
            ".vjvvwwvvVV.",
            ".vvvvwwvvVV.",
            ".vwwwwwwwwV.",
            ".vwwwwwwwwV.",
            ".vvvvwwvvVV.",
            "..vvvwwvvV..",
            "..vvvvvvVV..",
            "...vvvvVV...",
            "....vvVV....",
            ".....VV.....",
        ]),
        ('Thimble armour', [         # a steel thimble, dimpled, worn like a helmet
            "....GGGG....",
            "...GgGgGg...",
            "..GGgGgGgg..",
            "..GgGgGgGg..",
            "..GGgGgGgh..",
            "..GgGgGggh..",
            ".GGGGGGgggh.",
            ".Gwwwwwwwgh.",
            ".hhhhhhhhhh.",
            "..vzzvvzzV..",
            "..vzevvzeV..",
            "...VVVVVV...",
        ]),
    ],
    'Direction_CompostBin': [
        ('Leafy bin', [
            "...L..M.....",
            "..LML.LM.L..",
            "...MLMLLML..",
            ".GGGGGGGGGg.",
            ".mbBmLbmbBg.",
            "..MMMMMMMg..",
            "..MLMMLMMg..",
            "..MMMMMMMg..",
            "..MLMMMLMg..",
            "..MMMMMMgg..",
            "..MMMMMMgg..",
            "...gggggg...",
        ]),
        ('Heap with a worm', [
            "...L....L...",
            "....L..L....",
            "...L....L...",
            "............",
            ".....bB.....",
            "....bmBB....",
            "...bBmbBB...",
            "..bmbBKKB...",
            ".bBbmKBbKB..",
            "bmbBbBbmbBB.",
            "BBmBBmBBmBBm",
            "mmmmmmmmmmmm",
        ]),
        ('Open lid', [
            "..GGGGGg....",
            ".GGGGGGgg...",
            "..L.L.......",
            ".L.L.L.yY...",
            ".GGGGGGoyGg.",
            ".mbBmbBmbBg.",
            "..GGGGGGGg..",
            "..GgGGgGGg..",
            "..GgGGgGgg..",
            "..GgGGgGgg..",
            "..GGGGGGgg..",
            "...ggggggg..",
        ]),
    ],
    'Direction_DandelionSeason': [
        ('Seed clock', [         # the white puff of seeds on its stem
            "...G.w.G....",
            "..GwwGwwG...",
            ".GwGwwwGwG..",
            ".wwwGoGwwg..",
            ".GwwoooGwG..",
            "..GwwGwwg...",
            "...G.M.g....",
            ".....M......",
            "..L..M......",
            ".LL.MM......",
            "..LLM.......",
            "....M.......",
        ]),
        ('Seeds on the wind', [
            "......w..G.w",
            "..w....G..w.",
            "...g....g.g.",
            "...Gw.w..g..",
            "....g.g.....",
            "..GwG.......",
            ".GwwwG......",
            ".wwoGw......",
            ".GwwwG......",
            "..GwG.......",
            "...M........",
            "...M........",
        ]),
        ('Three flowers', [
            "............",
            ".yYy....yy..",
            "yYoyy..yYoy.",
            ".yoy...yooy.",
            "..M....yyy..",
            "..M.yy..M...",
            "..MyYoy.M...",
            "..MyooyM....",
            "...yyyM.....",
            "L...M.M..L..",
            "LL.MMMM.LL..",
            ".LLMMMMLL...",
        ]),
    ],
    'Direction_MotherOfMushrooms': [
        ('Mother and two', [     # a big brown cap over two small red ones
            "...aAAAAa...",
            ".aAAAAAAAAb.",
            "aAAAAAAAAbbb",
            "BbbbbbbbbbbB",
            "....GGGg....",
            "....GGGg....",
            "rr..GGGg..rr",
            "RrR.GGGg.rRR",
            ".G..GGGg..g.",
            ".G..GGGg..g.",
            "SSSSSSSSSSSs",
            "ssssssssssss",
        ]),
        ('Cluster on a log', [
            "...rr.......",
            "..rwrR..rr..",
            "..rrRR.rwrR.",
            "...G...rrRR.",
            "...G.rr..G..",
            "...GrwrR.G..",
            "...GrrRR.G..",
            "..bBbGbBbGb.",
            ".bmBbBbBmBbB",
            ".bBbmbBbBbmB",
            ".BBBBBBBBBBB",
            "..mmmmmmmmm.",
        ]),
        ('Spore shower', [       # one red cap shedding spores, a sprout of new caps below
            "...rrrrrr...",
            ".rrwrrrwrrR.",
            "rrwwrrrrrrRR",
            "RRRRRRRRRRRR",
            ".g.G.GGg.g..",
            ".....GGg....",
            "..g..GGg.g..",
            ".....GGg....",
            ".r...GGg..r.",
            "rRr..GGg.rRr",
            ".G.SSSSSs.g.",
            "SSSSssssSSSs",
        ]),
    ],
    'Direction_EfficientRecycler': [
        ('Three arrows', [       # the recycling triangle of arrows, in thread colours
            ".....jx.....",
            "....jxxX....",
            "...jx..xX...",
            "..XXX...xX..",
            "..jx....XXX.",
            ".jx......x..",
            ".x........x.",
            "jx.......jx.",
            "xX......jx..",
            ".XXXjjxxx...",
            "....XXXX....",
            "............",
        ]),
        ('Spool and arrow', [    # a spool of thread with an arrow running round it
            "...xjjjx....",
            "..j.....X...",
            ".j.aAAAa.X..",
            ".j..xjx..XXX",
            "jXX.jxX...X.",
            ".X..xjX.....",
            "....jxX..X..",
            ".j.aAAAa.X..",
            "..j.....X...",
            "...XXXXX....",
            "............",
            "............",
        ]),
        ('Yarn loop', [          # a single strand of yarn knotted into an endless loop
            "............",
            "..jjx..jjx..",
            ".j...xj...X.",
            "j.....x....X",
            "j....j.x...X",
            ".x..j...x.X.",
            "..xX.....X..",
            "..Xx....xX..",
            ".X..x..x..X.",
            "X....XX....X",
            ".X..X..X..X.",
            "..XX....XX..",
        ]),
    ],
    'Direction_SewingMachine': [
        ('Side view', [          # the light blue machine with a spool on top and thread down to the needle
            "....xj......",
            "....XX......",
            ".uuuuuuuuuu.",
            ".uwwuuuuuuuU",
            ".uuuuuuuuuuU",
            ".uUU....UuuU",
            ".UU.....i.UU",
            "........i...",
            "..uuuuuuGuu.",
            ".uwwuuuuuuuU",
            ".UUUUUUUUUUU",
            "..hh....hh..",
        ]),
        ('Hand wheel', [         # three-quarter view, the big hand wheel at the right
            "............",
            "..uuuuuu....",
            ".uwwuuuuUGG.",
            ".uuuuuuuGggG",
            ".uU...UUGhgG",
            ".UU...UUGggG",
            ".i.....UUGG.",
            ".i..........",
            ".Guuuuuuuuu.",
            "uwwuuuuuuuuU",
            "UUUUUUUUUUUU",
            ".hh......hh.",
        ]),
        ('Stitching a seam', [   # the presser foot and needle running a stitched line across cloth
            "..uuuuu.....",
            ".uwwuuuU....",
            ".uuuuuuU....",
            "...uUU......",
            "....i.......",
            "....i.......",
            "...GiG......",
            "vvvvvvvvvvvV",
            "vwvwvwvXvvvV",
            "vvvvvvvvvvvV",
            "VVVVVVVVVVVV",
            "............",
        ]),
    ],
    # ---------------------------------------------------------------------------------------------- Cardinals (12)
    'Cardinal_VampireDentures': [
        ('Wide open', [          # an open jaw, two long fangs down, two short up
            ".rrrrrrrrrR.",
            "rrkkkkkkkkrR",
            "rwwGwwwwGwwR",
            "rwwwwwwwwwwR",
            ".wG.....wG..",
            ".w.......w..",
            "............",
            "............",
            "..w......w..",
            ".rwwwwwwwwR.",
            "rrrRRRRRRRRR",
            ".RRRRRRRRRR.",
        ]),
        ('Blood drop', [         # closed dentures with the fangs over the lip and a drop
            "............",
            "..rrrrrrrr..",
            ".rkkkkkkkkR.",
            "rwwGwwwwGwwR",
            "rwwwwwwwwwwR",
            "rRwGRRRRwGRR",
            ".Rw.RRRRw.R.",
            "..w......r..",
            ".........R..",
            ".........r..",
            "........rRR.",
            ".........R..",
        ]),
        ('On a cushion', [       # the fangs presented on a purple felt cushion, like a prize
            "............",
            "..rrrrrrrr..",
            ".rkkkkkkkkR.",
            "rwwGwwwwGwwR",
            "rwwwwwwwwwwR",
            ".wG......wG.",
            "..w......w..",
            ".jvvvvvvvvV.",
            "jvvvvvvvvvVV",
            "vvvvvvvvvVVV",
            ".VVVVVVVVVV.",
            "..z......z..",
        ]),
    ],
}


def curl32(c, cx, cy, R, W, a0=180, a1=520, base=None, steps=400):
    """A breaking curl as a tapering spiral: from angle a0 (degrees, y down: 180 is left, 270 is up) to a1, its centre
    line closing from R to 0.4 R and its width from W to 0.25 W, plus the wave's back filled from the start of the
    spiral down to the base row."""
    m = Mask()
    for k in range(steps + 1):
        t = k / steps
        a = math.radians(a0 + (a1 - a0) * t)
        rc = R * (1 - 0.6 * t)
        w = W * (1 - 0.75 * t)
        for j in range(-int(w * 5), int(w * 5) + 1):
            r = rc + j / 10.0
            m.px.add((int(round(cx + math.cos(a) * r)), int(round(cy + math.sin(a) * r))))
    if base is not None:
        x0 = int(round(cx - R - W))
        x1 = int(round(cx - R + W))
        for y in range(int(cy), base + 1):
            grow = (y - cy) * 0.55
            for x in range(int(x0 - grow * 0.2), int(x1 + grow) + 1):
                m.px.add((x, y))
    return m


def crest32(vv):
    T, t, D, d, F, l = PAL['T'], PAL['t'], PAL['D'], PAL['d'], PAL['F'], PAL['l']
    c = Canvas(32, 32)
    if vv == 1:      # the curl inside a round navy badge with a foam rim
        disc = Mask().ellipse(16, 16, 15.6, 15.6)
        for (x, y) in disc.px:
            c.set(x, y, D if x + y < 34 else d)
        rim = disc.px - Mask().ellipse(16, 16, 14.4, 14.4).px
        for (x, y) in rim:
            c.set(x, y, F if x + y < 36 else PAL['f'])
        wave = curl32(c, 16, 14, 7.0, 2.6, base=22)
        water = Mask()
        for x in range(32):
            top = 21 + int(round(math.sin(x / 3.0) * 1.2))
            water.rect(x, top, x, 31)
        wave.px |= water.px
        wave.px &= Mask().ellipse(16, 16, 14.4, 14.4).px
        shade(c, wave, (T, t, t), cut=(-0.55, -9))
        edge = {(x, y) for (x, y) in wave.px if (x, y - 1) not in wave.px or (x - 1, y) not in wave.px}
        for p in edge:
            if p[1] < 21:
                c.set(p[0], p[1], F)
        for (x, y) in wave.px:
            if y >= 26 and (x + y) % 5 == 0:
                c.set(x, y, D)
    elif vv == 2:    # the curl breaking in front of a low coral sun
        sun = Mask().ellipse(22, 10, 7.5, 7.5)
        shade(c, sun, (PAL['k'], PAL['C'], PAL['c']), cut=(-0.2, -0.7))
        wave = curl32(c, 13, 14, 8.0, 2.8, base=24)
        water = Mask()
        for x in range(32):
            water.rect(x, 23 + int(round(math.sin(x / 2.5) * 1.0)), x, 31)
        wave.px |= water.px
        shade(c, wave, (T, t, D), cut=(-0.5, -1.05))
        for (x, y) in wave.px:
            if ((x, y - 1) not in wave.px or (x - 1, y) not in wave.px) and y < 23:
                c.set(x, y, F)
            if y >= 28:
                c.set(x, y, D if (x // 3 + y) % 4 else d)
            if y == 31:
                c.set(x, y, d)
    else:            # twin crests riding over banded water
        for (cx, cy, s) in ((9, 13, 1.0), (22, 16, 0.8)):
            wave = curl32(c, cx, cy, 5.5 * s, 2.0 * s, base=23)
            shade(c, wave, (T, t, D), cut=(-0.5, -1.05))
            for (x, y) in wave.px:
                if (x, y - 1) not in wave.px or (x - 1, y) not in wave.px:
                    c.set(x, y, F)
        for y in range(21, 32):
            for x in range(32):
                band = (y - 21) // 3
                col = [l, T, t, D][min(band, 3)]
                if (x + 2 * y) % 7 == 0 and band < 3:
                    col = F if band == 0 else l
                if y == 21 + int(round(math.sin(x / 2.0))) - 1:
                    col = F
                c.set(x, y, col)
    return c


def build(out=None):
    base = out or os.path.join(TEMP, 'variants')
    rows_out = {}
    for vv in (1, 2, 3):
        o = os.path.join(base, 'v%d' % vv)
        frames, names = [], []
        for n, opts in V.items():
            label, rows = opts[vv - 1]
            check('%s v%d' % (n, vv), rows, 12)
            c = canvas(rows)
            save_png(c, os.path.join(o, 'game_size', n + '.png'))
            save_png(preview(c, 12, 8), os.path.join(o, 'preview_8x', n + '_8x.png'))
            frames.append(c)
            names.append(n)
            rows_out[(n, vv)] = rows
        save_ase(os.path.join(o, 'aseprite', 'RogueWaves_Icons.aseprite'), 12, frames,
                 [(n, i, i, 'TEMP;icon=%s.png' % n) for i, n in enumerate(names)])
        big = crest32(vv)
        save_png(big, os.path.join(o, 'game_size', 'RogueWaves_Crest32.png'))
        save_png(preview(big, 32, 8), os.path.join(o, 'preview_8x', 'RogueWaves_Crest32_8x.png'))
        save_ase(os.path.join(o, 'aseprite', 'RogueWaves_Crest32.aseprite'), 32, [big],
                 [('crest', 0, 0, 'TEMP;icon=RogueWaves_Crest32.png')])
    return rows_out


CREST32_NAMES = ('Curl in a badge', 'Curl before the sun', 'Twin crests')

if __name__ == '__main__':
    rows = build()
    for vv in (1, 2, 3):
        for n in ('RogueWaves_RisingWater', 'RogueWaves_Quickening', 'RogueWaves_Nesting'):
            print('// v%d %s' % (vv, n))
            print('\n'.join('        "%s",' % r for r in rows[(n, vv)]))
