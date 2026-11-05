"""Temporary art: the 47 icons of the Directions, Trinkets and Cardinals built 2026-11-05 (B11, docs/scope/11 and 12 Look
lines), 12 x 12 like the mode's others (ui_icons.py, kit docs/03 section 2). Opaque, no outline, lit from the upper left;
pack palette from docs/STYLE.md plus a few materials (brass, flame, Yossele's clay).

    python3 concept-tools/temp_art/b11_icons.py [contact.png]
        -> TDS_Mod/ModSource/RogueWavesMode/Art/Temporary/game_size/<name>.png, preview_8x/, aseprite/RogueWaves_Icons_B11.aseprite

Writes only these 47 names, so the picked icons beside them (docs/TEMP_ART.md, "Dimitri's picks") are never touched; a
name already in game_size/ is skipped unless --force. The Direction names are the code's (RWDirections.Picture reads
Direction_<Name>, any case); the Trinkets' and Cardinals' are their sheets' Look (17_Cardinals.cs).
"""
import os
import sys
from tart import H, save_png, save_ase, preview, REPO
from ui_icons import PAL as BASE, canvas as _canvas

PAL = dict(BASE)
PAL.update({k: H(v) for k, v in {
    'O': 'F59E3B', 'Q': 'C46A2A',            # flame, flare-gun orange and its shade
    'Z': 'A8862A',                          # brass shade (brass is 'o' D9C52E, lit 'Y')
    'E': '864B41', 'H': '733E35', 'I': '99E550', 'J': '72CC35',   # Yossele's clay, eyes, leaf (docs/STYLE.md roster)
    'N': '3F6E46',                          # deep leaf
}.items()})


def canvas(rows):
    import ui_icons
    keep = ui_icons.PAL
    ui_icons.PAL = PAL
    try:
        return _canvas(rows)
    finally:
        ui_icons.PAL = keep


ICONS = {
    # ---------------------------------------------------------------- Any vessel (docs/scope/11 (Any))
    'Direction_Wound': [          # Wound Spring: a wound clock spring
        "............",
        "Yoooooooooo.",
        "..........Z.",
        ".oooooooo.Z.",
        ".o......Z.Z.",
        ".o.oooo.Z.Z.",
        ".o.o..Z.Z.Z.",
        ".o.o....Z.Z.",
        ".o.ZZZZZZ.Z.",
        ".o........Z.",
        ".ZZZZZZZZZZ.",
        "............",
    ],
    'Direction_Sieve': [          # Fine Sieve: a fine mesh sieve full of sand
        "............",
        "....SSSS....",
        "..SSSSSSss..",
        ".bAAAAAAAab.",
        ".bSSSSSSSsB.",
        ".BbbbbbbbbB.",
        ".BGgGgGgGgB.",
        "..BgGgGgGB..",
        "...BGgGgB...",
        "....BBBB....",
        "....s..s....",
        "...s..s..s..",
    ],
    'Direction_Hands': [          # Spare Hands: a pair of work gloves
        "............",
        "..AAA...SSS.",
        "..AaA...SsS.",
        "..AaAA.SSsS.",
        "A.AAAA.SSSS.",
        "AAAAAA.SSSSs",
        ".AAAAa.SSSs.",
        ".AAAAa.SSSs.",
        ".aaaaa.ssss.",
        ".CCCCc.CCCc.",
        ".ccccc.cccc.",
        "............",
    ],
    'Direction_Kettle': [         # Galley Kettle: a dented galley kettle letting off steam
        "........F...",
        ".......F.F..",
        "....ggg...F.",
        "...g...g.F..",
        "..GGGGGGg.g.",
        ".GwGGGGGGgg.",
        ".GGGGGGGGg..",
        ".GGhGGGGGg..",
        ".GGGhGGGgg..",
        "..gggggggg..",
        "...hhhhhh...",
        "............",
    ],
    'Direction_Chain': [          # Chain Shot: two cannonballs joined by a chain
        "............",
        ".hh.........",
        "hGhe........",
        "hhee........",
        ".ee.G.......",
        ".....g......",
        "......G.....",
        ".......g.hh.",
        "........hGhe",
        "........hhee",
        ".........ee.",
        "............",
    ],
    'Direction_Salt': [           # Salt Barrel: a small barrel of salt pork
        "............",
        "..AAAAAAAa..",
        ".AwKwKGwGaB.",
        ".bAAAAAAaBB.",
        ".bbbbbbbbBB.",
        ".hhhhhhhhhh.",
        ".bAbbbbbbBB.",
        ".bAbbbbbbBB.",
        ".hhhhhhhhhh.",
        ".bbbbbbbbBB.",
        "..BBBBBBBB..",
        "............",
    ],
    'Direction_Fog': [            # Fog Horn: a brass fog horn
        "............",
        "............",
        "..........Yo",
        ".........Yoo",
        "........YooB",
        "gYYYYYYYoooB",
        "hooooooooZoB",
        "........ZZZB",
        ".........ZZB",
        "..........ZB",
        "............",
        "............",
    ],
    'Direction_Clam': [           # Clam Shell: a closed clam shell
        "............",
        "............",
        "...kCkkCc...",
        "..kkCkkCkc..",
        ".kCkCkkCkCc.",
        ".kCkCkkCkCc.",
        ".kkCkCCkCkc.",
        "..kCkCCkCc..",
        "...kCCCCc...",
        "....cCCc....",
        ".....cc.....",
        "............",
    ],
    'Direction_Oar': [            # Notched Oar: an oar with a notch cut for every stroke
        "AA..........",
        "AAa.........",
        ".aAm........",
        "..aAa.......",
        "...mAa......",
        "....aAm.....",
        ".....aAa....",
        "......mAAa..",
        ".......AAAAa",
        "........AAAa",
        "........aAAs",
        ".........ss.",
    ],
    'Direction_Lamp': [           # Lamp Oil: a tin of lamp oil with a rag in the spout
        ".........wG.",
        "........wGw.",
        "........Gh..",
        "..GGGGGGh...",
        ".CCCCCCCCc..",
        ".CkCCCCCCc..",
        ".CkCYYYYCc..",
        ".CkCYoYoCc..",
        ".CCCYYYYCc..",
        ".CCCCCCCCc..",
        "..cccccccc..",
        "............",
    ],
    'Direction_Hook': [           # Fish Hook: a barbed fish hook on a short line
        ".....g......",
        ".....g......",
        ".....g......",
        "....hGh.....",
        ".....G......",
        ".....G......",
        ".....G......",
        ".G...G......",
        ".GG..G......",
        ".G...G......",
        "..G.G.......",
        "...G........",
    ],
    'Direction_Compass': [        # Spare Compass: a compass with two needles
        "............",
        "...YoooooZ..",
        "..YwwwwwwwZ.",
        ".YwwrwwwUwwZ",
        ".owwwrwUwwwZ",
        ".owwwwhwwwwZ",
        ".owwwGwgwwwZ",
        ".owwGwwwgwwZ",
        ".ZwwwwwwwwZ.",
        "..ZwwwwwwZ..",
        "...ZZZZZZ...",
        "............",
    ],
    'Direction_Magpie': [         # Magpie Nest: a nest full of buttons, keys and bottle caps
        "............",
        "............",
        "............",
        "...y........",
        "..yoy.r.u...",
        "..ByBrRuUw..",
        ".bmbbmbbmbb.",
        "bmbBbmbBbmbB",
        ".BbmBbmBbmB.",
        "..BmbBmbBm..",
        "...BBBBBB...",
        "............",
    ],
    'Direction_Fireship': [       # Fire Ship: a burning rowboat with a chain trailing behind
        ".....O......",
        "....OyO..O..",
        "...OyYyO.OO.",
        "..OyYYyOOyO.",
        "..OyYyyyyO..",
        "...OOyOyOO..",
        "bAAAAAAAAAAb",
        ".bbbbbbbbbB.",
        "..BbbbbbbB..",
        "G..BBBBBB...",
        ".g.G........",
        "..G.g.......",
    ],
    'Direction_Pool': [           # Tide Pool: a rock pool brimming over
        "............",
        "............",
        "............",
        "...F...F....",
        "..lFTTFTl...",
        ".GlTTTTTTlG.",
        "GGgTTlTTTgGF",
        "GwGgTTTTgGgT",
        "GGGGggggGGhT",
        ".gGGgGGghht.",
        "..hhhhhhh.T.",
        "..........t.",
    ],
    # ---------------------------------------------------------------- base vessels (docs/scope/11)
    'Direction_Trowel': [         # Mason's Trowel: a mason's trowel with mortar on it
        ".....G......",
        ".....Gg.....",
        "....GwGg....",
        "....GwGg....",
        "...GwGGgg...",
        "...GSSSGg...",
        "..GSSsSSgg..",
        "..GGGGGGgh..",
        "......h.....",
        ".....hh.....",
        "....bAb.....",
        "....BbB.....",
    ],
    'Direction_Shovel': [         # Snow Shovel: a snow shovel with a bent blade
        "...........b",
        "..........bA",
        ".........bA.",
        "........bA..",
        ".......bA...",
        "......bA....",
        "....uuu.....",
        "..uuwuuuu...",
        ".uwwuuuuuU..",
        "uuuuuuuuU...",
        ".UUuuuUU....",
        "..UU..U.....",
    ],
    'Direction_Wormtin': [        # Worm Tin: a rusty tin of bait worms
        "............",
        "..kK.....kK.",
        ".K..K...K..K",
        ".K..K.kK...K",
        ".GGGGGGGGGg.",
        ".GgQcgggggh.",
        ".GggcQgggch.",
        ".GgggggQcgh.",
        ".GggQggcggh.",
        ".gggggggggh.",
        "..hhhhhhhh..",
        "............",
    ],
    'Direction_Opensign': [       # Open Sign: a hand-painted OPEN LATE sign
        ".....h......",
        "....g.g.....",
        "...g...g....",
        ".AAAAAAAAAa.",
        ".ArrArrArAs.",
        ".ArArrArrAs.",
        ".ArrArAArAs.",
        ".AAAAAAAAAs.",
        ".AUAUUAUUAs.",
        ".AAAAAAAAAs.",
        "..ssssssss..",
        "............",
    ],
    'Direction_Leash': [          # Long Leash: a long red dog leash
        "..rrr.......",
        ".r...r......",
        ".r...r......",
        "..rrRr......",
        ".....R......",
        "....rR..rr..",
        "...r..R.r.r.",
        "...r...Rr.r.",
        "....rr..R.r.",
        "........RR..",
        ".........G..",
        "........GhG.",
    ],
    # ---------------------------------------------------------------- Argo (docs/scope/11)
    'Direction_Chumbucket': [     # Chum Bucket: a bucket of chum
        "............",
        "...hhhhhh...",
        "..h......h..",
        ".GrKrRKrRrg.",
        ".GGGGGGGGgg.",
        ".GwGGGGGGgh.",
        "..GwGGGGgh..",
        "..GGGGGGgh..",
        "..GGGGGGgh..",
        "...gggggh...",
        "...hhhhhh...",
        "............",
    ],
    'Direction_Pressuregauge': [  # Pressure Gauge: a brass gauge, its needle in the red
        "............",
        "...ZooooZ...",
        "..owwwwrrZ..",
        ".owwwwwwrrZ.",
        ".owwwwwhwrZ.",
        ".owwwwhwwwZ.",
        ".owwwewwwwZ.",
        ".owwwwwwwwZ.",
        "..ZwwwwwwZ..",
        "...ZZZZZZ...",
        ".....oo.....",
        "....ZooZ....",
    ],
    # ---------------------------------------------------------------- Yossele (docs/scope/11)
    'Direction_Cuttingjar': [     # Cutting Jar: a jar of water with a plant cutting rooting in it
        ".....J......",
        "....JJM.L...",
        "...J.M.LL...",
        ".....M......",
        "..GGGMGGG...",
        ".pPPPMPPPp..",
        ".puuuMuuup..",
        ".puuSMSuup..",
        ".pusuSusup..",
        ".puuuuuuup..",
        "..ppppppp...",
        "............",
    ],
    'Direction_Sapbucket': [      # Sap Bucket: a tin bucket under a tapped tree, sap dripping in
        "bAb.........",
        "bAb.........",
        "bAbG........",
        "bAbGG.......",
        "bAb..o......",
        "bAb.........",
        "bAb..o......",
        "bAbhhhhhhh..",
        "bAbGGGGGGg..",
        "bAbGwGGGgg..",
        "bAb.GGGGg...",
        "bAb.hhhhh...",
    ],
    'Direction_Creepingivy': [    # Creeping Ivy: a sprig of ivy curling round a stone
        "............",
        "......L.....",
        ".....LML....",
        "..GGGG.M....",
        ".GGGGGgM....",
        "GGLLGGgMg...",
        "GLMML.MgghL.",
        "GgMggMgghLM.",
        ".gghMhhhML..",
        "..hhhhhh....",
        "............",
        "............",
    ],
    'Direction_Deepbreath': [     # Deep Breath: Yossele puffing out its cheeks
        ".....J.J....",
        "......M.....",
        "...EEEEEE...",
        "..EEEEEEEE..",
        ".EEIEEEEIEH.",
        "EEEEEEEEEEHH",
        "EkEEEEEEEkHH",
        "EEEEEHHEEEHH",
        ".EEEEEEEEHH.",
        "..HHHHHHHH..",
        "......F.F...",
        ".....F...F..",
    ],
    'Direction_Fairyring': [      # Fairy Ring: a ring of small white mushrooms in the grass
        "............",
        "............",
        "............",
        "...MwGMwGM..",
        ".MMMAMMAMMM.",
        "MwGMMMMMMwGN",
        "MAMNMMMMMAMN",
        "MMMMMNMMMMMN",
        ".MMwGMMwGNN.",
        "...ANNNANN..",
        "............",
        "............",
    ],
    # ---------------------------------------------------------------- Spool (docs/scope/11)
    'Direction_Doubleneedle': [   # Double Needle: a sewing machine needle with two points
        "....hGGh....",
        "....gGGg....",
        "....gGwg....",
        ".....Gg.....",
        "....GggG....",
        "...Gg..gG...",
        "...G....G...",
        "...G....G...",
        "...w....w...",
        "...G....G...",
        "...g....g...",
        "...h....h...",
    ],
    'Direction_Buttonjar': [      # Button Jar: a glass jar of mismatched buttons
        "...hhhhhh...",
        "...gGGGGg...",
        "..pPPPPPPp..",
        ".pPryPuPCPp.",
        ".pPyyPuuPPp.",
        ".pxPPrPPyPp.",
        ".pxxPrrPyyp.",
        ".pPCPPPxPPp.",
        ".pCCuPPxxPp.",
        ".pPPuuPPPPp.",
        "..pppppppp..",
        "............",
    ],
    'Direction_Patternbook': [    # Pattern Book: a dog-eared sewing pattern book
        "............",
        ".TTTTTTTFF..",
        ".TlTTTTTTFf.",
        ".TlFTFTFTTt.",
        ".TlTTTTTTTt.",
        ".TlTFFFTTTt.",
        ".TlTFTFTTTt.",
        ".TlTFFFTTTt.",
        ".TlTTTTTTTt.",
        ".TtttttttTt.",
        ".FFFFFFFFFf.",
        "..ffffffff..",
    ],
    'Direction_Meritbadge': [     # Merit Badge: a felt merit badge sewn on crooked
        "............",
        "....xxxx....",
        "..xxwxxwxx..",
        ".xwxxxxxxXx.",
        ".xxxxyxxxwX.",
        "xwxxyyyxxxX.",
        "xxxxxyxxxwXX",
        ".xwxyxyxxXX.",
        ".xxxxxxxXwX.",
        "..xXwXXwXX..",
        "....XXXX....",
        "............",
    ],
    'Direction_Oddsocks': [       # Odd Socks: a pair of socks that do not match
        ".rwr...UuU..",
        ".rwr...UuU..",
        ".wrw...uUu..",
        ".rwr...UuU..",
        ".wrw...uwU..",
        ".rwrr..UuUu.",
        ".wrwrw.UUuUU",
        "..rwrw..uUuU",
        "...rww...UUu",
        "............",
        "............",
        "............",
    ],
    # ---------------------------------------------------------------- Trinkets (docs/scope/12)
    'Cardinal_BrassSpyglass': [   # a dented brass spyglass
        "........YYo.",
        ".......YooZZ",
        "......YooZZ.",
        ".....hoZZh..",
        "....YoZZ....",
        "...YoZh.....",
        "..hoZ.......",
        ".YoZ........",
        "YoZ.........",
        "oZ..........",
        "............",
        "............",
    ],
    'Cardinal_GuttingKnife': [    # a fish gutting knife
        "..........wG",
        ".........wGg",
        "........wGg.",
        ".......wGg..",
        "......wGg...",
        ".....wGg....",
        "....hhg.....",
        "...bAh......",
        "..bAB.......",
        ".bAB........",
        "bAB.........",
        "BB..........",
    ],
    'Cardinal_OilCan': [          # a long-spouted oil can
        "...........G",
        "..........G.",
        ".........G..",
        "........G...",
        "...hG..G....",
        "...GGGgg....",
        "..rrrrrrr...",
        ".rkrrrrrrR..",
        ".rkrrrrrrR..",
        ".rrrrrrrrR..",
        "..RRRRRRR...",
        "............",
    ],
    'Cardinal_TarBucket': [       # a bucket of pitch with a brush in it
        ".........b..",
        "........bA..",
        ".......bA...",
        "......bA....",
        "..eeeeBeee..",
        ".beeheeeeeB.",
        ".bAbebbbbBB.",
        ".hhhehhhhhh.",
        ".bAbbbbbbBB.",
        ".bAbbbbbbBB.",
        ".hhhhhhhhhh.",
        "..BBBBBBBB..",
    ],
    'Cardinal_RubberDuck': [      # a faded yellow rubber duck
        "............",
        "....YYY.....",
        "...YYYYy....",
        "...YeYyyOO..",
        "...YYyyOQ...",
        "....yyy.....",
        ".Y.YYYYYy...",
        ".YYYYYYYyy..",
        ".YYYYYYYYy..",
        "..yyyyyyyy..",
        "...oooooo...",
        "............",
    ],
    'Cardinal_SparePlank': [      # a spare plank with two nails in it
        "............",
        ".........AAa",
        ".......AAAab",
        ".....AAhAab.",
        "...AAAAAab..",
        ".AAAAaAab...",
        "AAAhAAab....",
        "aAAAab......",
        ".aab........",
        "...G........",
        "......G.....",
        "............",
    ],
    'Cardinal_PunchCard': [       # a shop punch card with three holes punched
        "............",
        "............",
        ".AAAAAAAAAa.",
        ".ACCCCCCCCa.",
        ".AAAAAAAAAa.",
        ".ADADADAsAa.",
        ".AAAAAAAAAa.",
        ".AssssssAAa.",
        ".AAAAAAAAAa.",
        ".aaaaaaaaas.",
        "............",
        "............",
    ],
    'Cardinal_PearlOyster': [     # a closed oyster with a pearl showing at the lip
        "............",
        "............",
        "...hgggh....",
        "..hgaGgagh..",
        ".hgGgagGggh.",
        "hgaGgPPgagh.",
        "hhhhPPpphhhh",
        ".hgagqpgagh.",
        "..hggaggh...",
        "...hhhhh....",
        "............",
        "............",
    ],
    'Cardinal_BottledStorm': [    # a storm cloud corked in a bottle
        ".....bb.....",
        ".....bA.....",
        ".....pp.....",
        "....pUUp....",
        "..pUGGGUp...",
        ".pUGGGGggUp.",
        ".pUgggggUUp.",
        ".pUUyUUUUUp.",
        ".pUUUyyUUUp.",
        ".pUUUUyUUpp.",
        "..pppppppp..",
        "............",
    ],
    'Cardinal_OilskinCoat': [     # a yellow oilskin coat, still dripping
        "...yy..yy...",
        "..yYyyyyYy..",
        ".yYyyoyyyyo.",
        "yYyyyoyyyyyo",
        "yYy.yoyy.yyo",
        "yYy.yoyy.yyo",
        "oo..yoyy..oo",
        "u...yoyyo..u",
        "....yoyyo...",
        "....yoyyo...",
        "...u.oo.u...",
        "............",
    ],
    'Cardinal_GullFeather': [     # a grey gull feather
        ".........hh.",
        "........hgG.",
        ".......hgGw.",
        "......hgGwG.",
        ".....hgGwG..",
        "....hgGwG...",
        "...hgGwG....",
        "..hgGwG.....",
        "..gGwG......",
        "...wG.......",
        "..w.........",
        ".w..........",
    ],
    # ---------------------------------------------------------------- Cardinals (docs/scope/12)
    'Cardinal_CorkLifebuoy': [    # a cork lifebuoy on a frayed rope
        "...aSSSSa...",
        ".aSSAAAASSa.",
        ".SAAs..sASs.",
        "aSAs....sSsa",
        "SAs......Ss.",
        "SAs......Ss.",
        "aSs......ssa",
        ".Sss....ssa.",
        ".aSSssssSa..",
        "...aaaaaa.A.",
        "..........A.",
        ".........A.A",
    ],
    'Cardinal_SignalFlare': [     # a spent flare gun
        "........f.f.",
        ".........f..",
        "..OOOOOOOh..",
        "..OkOOOOOQh.",
        "..OOOOOOOQ..",
        "..QOOQQQQ...",
        "..QOQ.h.....",
        "..QOQhh.....",
        ".QOOQ.......",
        ".QOQ........",
        ".QQQ........",
        "............",
    ],
    'Cardinal_CargoCrate': [      # a slatted cargo crate stencilled THIS SIDE UP
        "............",
        ".bbbbbbbbbb.",
        ".bAAAmmAAAb.",
        ".bAAmmmmAAb.",
        ".bammmmmmab.",
        ".bAAAmmAAAb.",
        ".bAAAmmAAAb.",
        ".baaaaaaaab.",
        ".bAmmAmmAAb.",
        ".bAAAAAAAAb.",
        ".bbbbbbbbbb.",
        "..BBBBBBBB..",
    ],
    'Cardinal_TinWhistle': [      # a dented tin whistle on a lanyard
        "..rrr.......",
        ".r...r......",
        ".r....r.....",
        "..r....r....",
        "...r....r...",
        "....rhGGGg..",
        "...GGGGwGGg.",
        "...GwGGGhGg.",
        "....gggGGGg.",
        "........ggh.",
        "............",
        "............",
    ],
}


def check(name, rows):
    bad = [i for i, r in enumerate(rows) if len(r) != 12]
    if len(rows) != 12 or bad:
        raise SystemExit('%s: %d rows, bad row lengths at %s' % (name, len(rows), bad))
    miss = {ch for r in rows for ch in r if ch not in PAL and ch not in '. '}
    if miss:
        raise SystemExit('%s: unknown colours %s' % (name, ''.join(sorted(miss))))


def build(out=None, force=False, icons=None, ase='RogueWaves_Icons_B11.aseprite'):
    """Writes each icon of `icons` (these 47 by default; objective_icons.py passes its own) and their .aseprite."""
    out = out or os.path.join(REPO, 'TDS_Mod', 'ModSource', 'RogueWavesMode', 'Art', 'Temporary')
    icons = icons or ICONS
    written = []
    for n, rows in icons.items():
        check(n, rows)
        path = os.path.join(out, 'game_size', n + '.png')
        if os.path.exists(path) and not force and n not in written:
            print('kept (exists):', n)
            continue
        c = canvas(rows)
        save_png(c, path)
        save_png(preview(c, 12, 8), os.path.join(out, 'preview_8x', n + '_8x.png'))
        written.append(n)
    names = list(icons)
    save_ase(os.path.join(out, 'aseprite', ase), 12, [canvas(icons[n]) for n in names],
             [(n, i, i, 'TEMP;icon=%s.png' % n) for i, n in enumerate(names)])
    return written


def contact(path, k=6, names=None, icons=None):
    """Every icon at 1x and kx with its name, on the UI's dark panel and on a light ground (review only, not shipped).
    icons: another set of rows in this palette (objective_icons.py), else these 47."""
    from PIL import Image, ImageDraw
    icons = icons or ICONS
    names = names or list(icons)
    pad, cols = 8, 8
    cw = 12 * k + 12 + 3 * pad
    ch = 12 * k + 2 * pad + 12
    img = Image.new('RGB', (cols * cw, ((len(names) + cols - 1) // cols) * ch * 2), (40, 44, 52))
    d = ImageDraw.Draw(img)
    for band, bg in enumerate([(46, 74, 98), (200, 205, 200)]):
        y_off = band * ((len(names) + cols - 1) // cols) * ch
        for i, n in enumerate(names):
            x0, y0 = (i % cols) * cw + pad, y_off + (i // cols) * ch + pad
            d.rectangle([x0 - 2, y0 - 2, x0 + 12 * k + 12 + pad + 2, y0 + 12 * k + 2], fill=bg)
            for y, r in enumerate(icons[n]):
                for x, c in enumerate(r):
                    if c in PAL:
                        col = PAL[c][:3]
                        d.rectangle([x0 + x * k, y0 + y * k, x0 + x * k + k - 1, y0 + y * k + k - 1], fill=col)
                        img.putpixel((x0 + 12 * k + pad + x, y0 + y), col)
            d.text((x0, y0 + 12 * k + 3), n.replace('Direction_', 'D ').replace('Cardinal_', 'C ').replace('Objective_', 'O '), fill=(230, 230, 230) if band == 0 else (20, 20, 20))
    img.save(path)


if __name__ == '__main__':
    force = '--force' in sys.argv
    args = [a for a in sys.argv[1:] if a != '--force']
    if args and args[0] == '--contact-only':
        contact(args[1])
        sys.exit(0)
    w = build(force=force)
    print('wrote', len(w), 'icons')
    if args:
        contact(args[0])
