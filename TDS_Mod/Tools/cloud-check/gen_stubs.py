"""Turns the TDS API dump (reference/api_assembly_csharp.txt) into C# reference stubs of Assembly-CSharp, so mod code
can be compile-checked away from the game. Unknown external types become `object` (logged)."""
import re, sys, collections
DUMP, UNITY, OUT = sys.argv[1], sys.argv[2], sys.argv[3]

KEYWORDS = set('abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null object operator out override params private protected public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while'.split())

SYSTEM = {'String':'string','Single':'float','Int32':'int','Boolean':'bool','Void':'void','Int64':'long','Double':'double',
  'Byte':'byte','UInt64':'ulong','UInt32':'uint','Int16':'short','UInt16':'ushort','Char':'char','SByte':'sbyte','Decimal':'decimal',
  'IntPtr':'System.IntPtr','Type':'System.Type','Exception':'System.Exception','IAsyncResult':'System.IAsyncResult',
  'AsyncCallback':'System.AsyncCallback','Action':'System.Action','Guid':'System.Guid','DateTime':'System.DateTime','TimeSpan':'System.TimeSpan',
  'Random':'System.Random','Delegate':'System.Delegate','Enum':'System.Enum','ValueType':'System.ValueType',
  'Threading.Tasks.Task':'System.Threading.Tasks.Task','Threading.CancellationToken':'System.Threading.CancellationToken',
  'Collections.IEnumerator':'System.Collections.IEnumerator','Text.StringBuilder':'System.Text.StringBuilder'}
SYS_GENERIC = {'List`1':'System.Collections.Generic.List','Dictionary`2':'System.Collections.Generic.Dictionary',
  'KeyValuePair`2':'System.Collections.Generic.KeyValuePair','Nullable`1':'System.Nullable','ValueTuple`3':'System.ValueTuple',
  'ValueTuple`2':'System.ValueTuple','Action`1':'System.Action','Action`2':'System.Action','Action`3':'System.Action','Func`1':'System.Func','Func`2':'System.Func',
  'Func`3':'System.Func','Threading.Tasks.Task`1':'System.Threading.Tasks.Task','HashSet`1':None,'Queue`1':None,
  'Collections.Generic.IEnumerator`1':'System.Collections.Generic.IEnumerator','IEnumerable`1':'System.Collections.Generic.IEnumerable',
  'NetworkVariable`1':'Unity.Netcode.NetworkVariable','NetworkList`1':'Unity.Netcode.NetworkList','NetworkListEvent`1':'Unity.Netcode.NetworkListEvent',
  'BufferSerializer`1':None,'Cysharp.Threading.Tasks.UniTask`1':'Cysharp.Threading.Tasks.UniTask','Events.UnityEvent`1':'UnityEngine.Events.UnityEvent'}
THIRD = {'NetworkBehaviour':'Unity.Netcode.NetworkBehaviour','NetworkObject':'Unity.Netcode.NetworkObject','NetworkManager':'Unity.Netcode.NetworkManager',
  'FixedString64Bytes':'Unity.Collections.FixedString64Bytes','FixedString32Bytes':'Unity.Collections.FixedString32Bytes',
  'FixedString128Bytes':'Unity.Collections.FixedString128Bytes','FixedString512Bytes':'Unity.Collections.FixedString512Bytes',
  'ServerRpcParams':'Unity.Netcode.ServerRpcParams','ClientRpcParams':'Unity.Netcode.ClientRpcParams',
  'Cysharp.Threading.Tasks.UniTask':'Cysharp.Threading.Tasks.UniTask','Cysharp.Threading.Tasks.UniTaskVoid':'Cysharp.Threading.Tasks.UniTaskVoid',
  'TMPro.TextMeshProUGUI':'TMPro.TextMeshProUGUI','TMPro.TMP_Text':'TMPro.TMP_Text','TMPro.TextMeshPro':'TMPro.TextMeshPro',
  'TextMeshProUGUI':'TMPro.TextMeshProUGUI','TMP_Text':'TMPro.TMP_Text','TextMeshPro':'TMPro.TextMeshPro','TMP_FontAsset':'TMPro.TMP_FontAsset'}
for n in 'Image RawImage Text Button Toggle Slider Scrollbar ScrollRect LayoutElement HorizontalLayoutGroup VerticalLayoutGroup GridLayoutGroup ContentSizeFitter Mask RectMask2D Outline Shadow Dropdown InputField Graphic MaskableGraphic Selectable CanvasScaler GraphicRaycaster'.split():
    THIRD['UI.' + n] = 'UnityEngine.UI.' + n

unity = set()
for line in open(UNITY):
    asm, full = line.rstrip('\n').split('\t')
    if '+' in full: full = full.replace('+', '.')
    unity.add(full)

# ---------------------------------------------------------------- parse the dump
class T:
    def __init__(s, vis, kind, path, base, ns):
        s.vis, s.kind, s.path, s.base, s.ns = vis, kind, path, base, ns
        s.fields, s.methods, s.props, s.values, s.children = [], [], [], [], []
types = []
cur = None
for raw in open(DUMP, encoding='utf-8', errors='replace'):
    line = raw.rstrip('\n')
    m = re.match(r'^TYPE (public|internal) (class|struct|enum|interface) (\S+) : (\S*) \[ns=([^\]]*)\]', line)
    if m:
        cur = T(*m.groups()); types.append(cur); continue
    if cur is None: continue
    if line.startswith('  values: '): cur.values = [v for v in line[10:].split(',') if v]; continue
    m = re.match(r'^  F (public|private|protected|internal|nonpublic) (static )?(\S+) (\S+)$', line)
    if m: cur.fields.append(m.groups()); continue
    m = re.match(r'^  P (public|private|protected|internal|nonpublic) (static )?(\S+) (\S+)$', line)
    if m: cur.props.append(m.groups()); continue
    m = re.match(r'^  M (public|private|protected|internal) (static )?(\S+) (\S+)\((.*)\)$', line)
    if m: cur.methods.append(m.groups()); continue

def skipname(p): return '<' in p or '>' in p or 'DisplayClass' in p
# Key = namespace-qualified dump name ("Blockage.Address.Bl_Address/Loadlist", "Entity_Master/Link_Entitydataref").
bykey = {}
roots = []
last_by_path = {}
for t in types:
    parts = t.path.split('/')
    if len(parts) > 1:
        parent = last_by_path.get('/'.join(parts[:-1]))
        t.parent = parent
        t.fullns = parent.fullns if parent is not None else ''
    else:
        t.parent = None
        t.fullns = t.ns
    last_by_path[t.path] = t
    if t.vis != 'public' or skipname(t.path): continue
    if len(parts) > 1 and (parent is None or getattr(parent, 'key', None) is None): continue
    t.key = ((t.fullns + '.') if t.fullns else '') + t.path
    if t.key in bykey: continue
    bykey[t.key] = t
    if len(parts) > 1: parent.children.append(t)
    else: roots.append(t)
for t in bykey.values():
    t.csname = 'global::' + ((t.fullns + '.') if t.fullns else '') + t.path.replace('/', '.')
lookup = dict(bykey)

unresolved = collections.Counter()
def split_args(s):
    out, depth, curp = [], 0, ''
    for ch in s:
        if ch == '<': depth += 1
        if ch == '>': depth -= 1
        if ch == ',' and depth == 0: out.append(curp); curp = ''
        else: curp += ch
    out.append(curp)
    return [x.strip() for x in out]

def res(name, objctx=True):
    """Dump type expression -> C# type, or None if it cannot be expressed."""
    name = name.strip()
    if name.endswith('&'): return None
    if name.endswith('[]'):
        inner = res(name[:-2]); return inner + '[]' if inner else None
    m = re.match(r'^([^<]+)<(.*)>$', name)
    if m:
        gen, args = m.group(1), split_args(m.group(2))
        base = SYS_GENERIC.get(gen)
        if base is None:
            unresolved[gen] += 1; return None
        rargs = [res(a) for a in args]
        if any(r is None for r in rargs): return None
        if gen == 'Nullable`1' and rargs[0] == 'object': return None
        return base + '<' + ', '.join(rargs) + '>'
    if name in SYSTEM: return SYSTEM[name]
    if name == 'Object': return 'UnityEngine.Object' if objctx else 'object'
    if name in lookup: return lookup[name].csname
    if name in THIRD: return THIRD[name]
    if 'UnityEngine.' + name in unity: return 'UnityEngine.' + name
    if name in unity: return name
    if 'System.' + name in ('System.Collections.IEnumerator',): return 'System.' + name
    if re.match(r'^[A-Z]$', name) or name in ('T', 'TKey', 'TValue'): return None
    unresolved[name] += 1
    return 'object'

def ident(n):
    n = n.replace('`', '_')
    if not re.match(r'^[A-Za-z_][A-Za-z0-9_]*$', n): return None
    return '@' + n if n in KEYWORDS else n

# method signatures per type for override detection
def params_of(ps):
    if not ps.strip(): return []
    out = []
    for p in split_args(ps):
        i = p.rfind(' ')
        out.append((p[:i], p[i+1:]))
    return out
def base_type(t):
    b = t.base
    if b in lookup: return lookup[b]
    return None
def sig(name, ps): return name + '(' + ','.join(res(pt) or '?' for pt, _ in ps) + ')'
def ancestors(t):
    seen = []
    b = base_type(t)
    while b is not None and b not in seen:
        seen.append(b); b = base_type(b)
    return seen
declared = {}
for t in bykey.values():
    declared[t.key] = set(sig(m[3], params_of(m[4])) for m in t.methods if m[0] == 'public' and not m[1] and m[3] != '.ctor')

# Default parameter values the dump doesn't record (method -> {param index: default}); mod code relies on them.
DEFAULTS = {('Bl_Menus', 'Changemenu'): {1: 'false', 2: 'null', 3: 'false'}}
out = ['// Generated reference stubs of Assembly-CSharp (TDS), from reference/api_assembly_csharp.txt. Bodies never run.',
       '#pragma warning disable', 'using System;', 'using System.Collections.Generic;', '']

def emit(t, ind):
    I = '    ' * ind
    name = t.path.split('/')[-1]
    nm = ident(name)
    if nm is None: return
    if t.kind == 'enum':
        out.append(I + 'public enum ' + nm + ' { ' + ', '.join(ident(v) or ('_' + v) for v in t.values) + ' }')
        return
    if t.base == 'MulticastDelegate':
        inv = [m for m in t.methods if m[3] == 'Invoke']
        if inv:
            ret = res(inv[0][2]) or 'void'
            ps = params_of(inv[0][4])
            pl = []
            for pt, pn in ps:
                r = res(pt)
                pl.append((r or 'object') + ' ' + (ident(pn) or 'p'))
            out.append(I + 'public delegate ' + ret + ' ' + nm + '(' + ', '.join(pl) + ');')
        return
    if t.kind == 'struct':
        head = I + 'public partial struct ' + nm
    else:
        b = t.base
        bt = None
        if b and b not in ('Object', 'ValueType'):
            bt = res(b)
            if bt in ('object', None): bt = None
        head = I + 'public partial class ' + nm + (' : ' + bt if bt else '')
    out.append(head)
    out.append(I + '{')
    J = I + '    '
    used = set()
    anc = ancestors(t) if t.kind == 'class' else []
    for vis, st, ty, fn in t.fields:
        if vis != 'public' and vis != 'protected': continue
        r = res(ty); idn = ident(fn)
        if r is None or idn is None or fn == name or idn in used: continue
        used.add(idn)
        out.append(J + vis + ' ' + ('static ' if st else '') + r + ' ' + idn + ';')
    static_fields = set(f[3] for f in t.fields if f[1])
    for vis, st, ty, pn in t.props:
        if vis not in ('public', 'protected') or '.' in pn: continue
        if ('_' + pn) in static_fields: st = 'static '
        r = res(ty); idn = ident(pn)
        if r is None or idn is None or pn == name or idn in used: continue
        used.add(idn)
        out.append(J + vis + ' ' + ('static ' if st else '') + r + ' ' + idn + ' { get { throw null; } set { } }')
    msigs = set()
    has_ctor = False
    for vis, st, ret, mn, ps in t.methods:
        if vis not in ('public', 'protected'): continue
        pl = params_of(ps)
        rps = [res(pt) for pt, _ in pl]
        if any(r is None for r in rps): continue
        pnames = []
        for k, (_, pn) in enumerate(pl):
            pnames.append(ident(pn) or ('p' + str(k)))
        dflt = DEFAULTS.get((name, mn), {})
        plist = ', '.join(r + ' ' + n + ((' = ' + dflt[k]) if k in dflt else '') + (' = default' if r in ('Unity.Netcode.ServerRpcParams', 'Unity.Netcode.ClientRpcParams') and k == len(rps) - 1 else '') for k, (r, n) in enumerate(zip(rps, pnames)))
        if mn == '.ctor':
            if t.kind == 'struct' and not pl: continue
            key = 'ctor(' + ','.join(rps) + ')'
            if key in msigs: continue
            msigs.add(key); has_ctor = True
            out.append(J + vis + ' ' + nm + '(' + plist + ')' + (' : this()' if t.kind == 'struct' else '') + ' { }')
            continue
        idn = ident(mn)
        if idn is None or mn == name or mn.startswith('op_') or idn in used: continue
        r = res(ret)
        if r is None: continue
        key = mn + '(' + ','.join(rps) + ')'
        if key in msigs: continue
        msigs.add(key)
        mod = vis + ' '
        if st: mod += 'static '
        elif t.kind == 'class':
            s = sig(mn, pl)
            if any(s in declared.get(a.key, ()) for a in anc): mod += 'override '
            elif mn in ('ToString',) and not pl: mod += 'override '
            elif mn in ('Equals',) and rps == ['UnityEngine.Object']: mod += 'new virtual '
            elif mn in ('GetHashCode',) and not pl: mod += 'override '
            else: mod += 'virtual '
        elif mn in ('ToString', 'GetHashCode') and not pl: mod += 'override '
        body = '{ }' if r == 'void' else '{ throw null; }'
        out.append(J + mod + r + ' ' + idn + '(' + plist + ') ' + body)
    if t.kind == 'class' and has_ctor and 'ctor()' not in msigs:
        out.append(J + 'protected ' + nm + '() { }')
    for c in t.children: emit(c, ind + 1)
    out.append(I + '}')

byns = collections.defaultdict(list)
for t in roots: byns[t.ns].append(t)
for ns, ts in sorted(byns.items()):
    if ns: out.append('namespace ' + ns + ' {')
    for t in ts: emit(t, 1 if ns else 0)
    if ns: out.append('}')
open(OUT, 'w').write('\n'.join(out) + '\n')
sys.stderr.write('types %d, unresolved: %s\n' % (len(bykey), ', '.join('%s(%d)' % kv for kv in unresolved.most_common(60))))
