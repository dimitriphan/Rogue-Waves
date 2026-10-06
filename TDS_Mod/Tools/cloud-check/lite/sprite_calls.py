# Pulls the SaveSheet(...) calls out of <P>Builder.WriteSprites so the preview writes exactly the sheets the build does.
import re, sys
src = open(sys.argv[1]).read()
body = src[src.index('static void WriteSprites()'):]
body = body[:body.index('log.AppendLine')]
calls = re.findall(r'SaveSheet\((.*?), "(?:Vessel|UI|Unit|Tower|Fx)", "([^"]+)", (\d+), (\d+), \w+\);', body)
print('\n'.join('            Save("%s", %s, %s);' % (n, expr, cw) for expr, n, cw, ch in calls))
