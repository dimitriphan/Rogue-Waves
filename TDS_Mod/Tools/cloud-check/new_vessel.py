"""Port of tools/New-Vessel.ps1: copies the reference vessel (Mummer) into <project>/ModSource/<Name> and
<project>/Assets/<Name>Mod/Editor with the same case-sensitive rename pairs, adds the shared kit (or the kit files the
project lacks, warning on any that differ), and writes DESIGN.md from the form."""
import os, re, sys, shutil, datetime
resource, project, name = sys.argv[1], sys.argv[2], sys.argv[3]
template, unit = 'Mummer', 'Understudy'
assert re.match(r'^[A-Z][a-z]{2,14}$', name), name
assert name not in ('Smacky', 'Snosno', 'Boutman', 'Fie', 'Buster', 'Smocky', template)
assert template.lower() not in name.lower() and name.lower() not in template.lower()
src = os.path.join(resource, 'template', template)
mod_source = os.path.join(project, 'ModSource', name)
assets = os.path.join(project, 'Assets', name + 'Mod')
for p in (mod_source, assets):
    assert not os.path.exists(p), 'already exists: ' + p
lower, upper = name.lower(), name.upper()
pairs = [(unit[:-1] + 'ies', name + 'units'), (unit, name + 'unit'), (unit.lower()[:-1] + 'ies', (name + 'units').lower()),
         (unit.lower(), (name + 'unit').lower()), (template.upper(), upper), (template, name), (template.lower(), lower)]
def rename(t):
    for a, b in pairs: t = t.replace(a, b)
    return t
written = []
def copy(f, to_dir):
    os.makedirs(to_dir, exist_ok=True)
    target = os.path.join(to_dir, rename(os.path.basename(f)))
    text = open(f, encoding='utf-8-sig').read()
    open(target, 'w', encoding='utf-8', newline='').write(rename(text))
    written.append(target)
for sub, to in (('Code', os.path.join(mod_source, 'Code')), ('Editor', os.path.join(assets, 'Editor')), ('Tools', os.path.join(mod_source, 'Tools'))):
    d = os.path.join(src, sub)
    for f in sorted(os.listdir(d)):
        if f.endswith('.cs') or f.endswith('.ps1'): copy(os.path.join(d, f), to)
for f in sorted(os.listdir(src)):
    if f.endswith('.ps1'): copy(os.path.join(src, f), mod_source)
form = open(os.path.join(resource, 'template', 'DESIGN_TEMPLATE.md'), encoding='utf-8').read()
open(os.path.join(mod_source, 'DESIGN.md'), 'w', encoding='utf-8').write(form.replace('{{NAME}}', name).replace('{{DATE}}', datetime.date.today().isoformat()))
shared, kit = os.path.join(project, 'ModSource', 'Shared'), os.path.join(resource, 'kit', 'Shared')
if not os.path.exists(shared):
    shutil.copytree(kit, shared)
else:   # like New-Vessel.ps1: add the kit files the project lacks, warn on each that differs
    for f in sorted(os.listdir(kit)):
        if not f.endswith('.cs'): continue
        there = os.path.join(shared, f)
        if not os.path.exists(there): shutil.copy(os.path.join(kit, f), there); print('added ' + there)
        elif open(there, 'rb').read() != open(os.path.join(kit, f), 'rb').read():
            print('WARNING: ModSource/Shared/%s differs from the kit. Keep the newer one (read both headers); every mod '
                  'in the project compiles the same Shared folder.' % f)
left = []
for p in written:
    for i, l in enumerate(open(p, encoding='utf-8')):
        if template in l or unit in l: left.append('%s:%d' % (os.path.basename(p), i + 1))
print('Scaffolded %s from %s (%d files)%s' % (name, template, len(written), ('; template names left: ' + ', '.join(left[:10])) if left else ''))
