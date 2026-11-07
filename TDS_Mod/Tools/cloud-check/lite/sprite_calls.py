# Pulls the SaveSheet(...) calls out of <P>Builder.WriteSprites so the preview writes the sheets the build does. A loop
# over a table of string arrays (Argo's Naut sheets: foreach (var crew in new[] { new[] { "Port", "r", "R" }, ... }))
# is unrolled first: crew[i] becomes the string, crew[i][0] its first character. Any other call whose sheet name still
# isn't a plain string is skipped, with a warning on stderr.
import re, sys
src = open(sys.argv[1]).read()
body = src[src.index('static void WriteSprites()'):]
body = body[:body.index('log.AppendLine')]

LOOP = re.compile(r'foreach \(var (\w+) in new\[\] \{((?:\s*new\[\] \{[^{}]*\},?)+)\s*\}\)\s*\{')


def unroll(m):
    var, rows = m.group(1), [re.findall(r'"([^"]*)"', r) for r in re.findall(r'new\[\] \{([^{}]*)\}', m.group(2))]
    depth, i = 1, m.end()
    while depth:
        depth += {'{': 1, '}': -1}.get(body[i], 0)
        i += 1
    inner, out = body[m.end():i - 1], []
    for row in rows:
        s = re.sub(r'\b%s\[(\d+)\]\[0\]' % var, lambda k: "'%s'" % row[int(k.group(1))][0], inner)
        s = re.sub(r'\b%s\[(\d+)\]' % var, lambda k: '"%s"' % row[int(k.group(1))], s)
        out.append(re.sub(r'"\s*\+\s*"', '', s))  # "Naut_" + "Port" + "-Sheet" -> "Naut_Port-Sheet"
    return ''.join(out), i


m = LOOP.search(body)
while m:
    text, end = unroll(m)
    body = body[:m.start()] + text + body[end:]
    m = LOOP.search(body)

calls = re.findall(r'SaveSheet\((.*?), "(?:Vessel|UI|Unit|Tower|Fx)", "([^"]+)", (\d+), (\d+), \w+\);', body)
skipped = body.count('SaveSheet(') - len(calls)
if skipped:
    sys.stderr.write('sprite_calls: %d SaveSheet call(s) without a plain sheet name are not previewed\n' % skipped)
print('\n'.join('            Save("%s", %s, %s);' % (n, expr, cw) for expr, n, cw, ch in calls))
