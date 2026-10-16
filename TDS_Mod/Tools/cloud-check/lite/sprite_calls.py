# Pulls the SaveSheet(...) calls out of <P>Builder.WriteSprites so the preview writes the sheets the build does. A call
# whose sheet name isn't a plain string (Argo's Naut sheets, named in a loop) can't be pulled out: it is skipped, with a
# warning on stderr.
import re, sys
src = open(sys.argv[1]).read()
body = src[src.index('static void WriteSprites()'):]
body = body[:body.index('log.AppendLine')]
calls = re.findall(r'SaveSheet\((.*?), "(?:Vessel|UI|Unit|Tower|Fx)", "([^"]+)", (\d+), (\d+), \w+\);', body)
skipped = body.count('SaveSheet(') - len(calls)
if skipped:
    sys.stderr.write('sprite_calls: %d SaveSheet call(s) without a plain sheet name are not previewed\n' % skipped)
print('\n'.join('            Save("%s", %s, %s);' % (n, expr, cw) for expr, n, cw, ch in calls))
