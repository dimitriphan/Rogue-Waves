#!/usr/bin/env python3
"""Tell the phone widget what a thread is doing: writes threads/<id>.json on the repository's `status` branch.

Each thread owns one file, so two threads writing at once never clash. The branch holds nothing else and never merges.
The widget (StatusWidget/app) reads the files every 15 minutes or on a tap. Rules for when to call it:
StatusWidget/README.md "When a thread reports".

    python3 StatusWidget/status/set_status.py --id <thread id> --thread "<thread title>" --state testing --step "Uloe self-test"
    python3 StatusWidget/status/set_status.py --id <thread id> --helper "Reading the Uloe test log|Haiku"
    python3 StatusWidget/status/set_status.py --id <thread id> --helper-done "Reading the Uloe test log"
    python3 StatusWidget/status/set_status.py --id <thread id> --clear          # the thread is finished: remove its file
    python3 StatusWidget/status/set_status.py --list                           # print every thread's file

States: idle, building, testing, reviewing, art, waiting, shipped, failed. `started` moves only when the state changes;
`updated` moves on every write. Each write also removes other threads' files untouched for a day (a week for
"waiting"). Needs git and push access to the repository (the cloud threads' origin, or the PC's gh).
"""
import argparse
import datetime
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time

STATES = ('idle', 'building', 'testing', 'reviewing', 'art', 'waiting', 'shipped', 'failed')
BRANCH = 'status'
DIR = 'threads'
README = """# Status branch

Written by `StatusWidget/status/set_status.py`, read by the phone widget (`StatusWidget/` on the main branches).
One file per thread under `threads/`. Never merge this branch anywhere.
"""


def git(cwd, *args, check=True):
    r = subprocess.run(['git', *args], cwd=cwd, capture_output=True, text=True)
    if check and r.returncode:
        raise RuntimeError('git %s failed: %s' % (' '.join(args), (r.stderr or r.stdout).strip()))
    return r


def now():
    return datetime.datetime.now(datetime.timezone.utc).replace(microsecond=0).isoformat().replace('+00:00', 'Z')


def safe_id(s):
    s = re.sub(r'[^A-Za-z0-9_-]+', '-', s).strip('-')
    if not s:
        sys.exit('--id needs letters or digits')
    return s[:80]


def origin_url(repo_dir):
    return git(repo_dir, 'remote', 'get-url', 'origin').stdout.strip()


def checkout(work, url):
    """A fresh clone of only the status branch's tip in `work`; an empty orphan if the branch doesn't exist yet."""
    if os.path.isdir(os.path.join(work, '.git')):
        shutil.rmtree(work)
    os.makedirs(work, exist_ok=True)
    git(work, 'init', '-q')
    git(work, 'remote', 'add', 'origin', url)
    if git(work, 'fetch', '-q', '--depth', '1', 'origin', BRANCH, check=False).returncode == 0:
        git(work, 'checkout', '-q', '-B', BRANCH, 'FETCH_HEAD')
        return True
    git(work, 'checkout', '-q', '--orphan', BRANCH)
    with open(os.path.join(work, 'README.md'), 'w') as fh:
        fh.write(README)
    return False


PRUNE_HOURS = {'waiting': 24 * 7}          # every other state: a day


def prune(work, keep):
    """Remove other threads' files nobody has touched for a day (a week for "your turn"), so the phone reads few files."""
    d = os.path.join(work, DIR)
    cut = datetime.datetime.now(datetime.timezone.utc)
    gone = False
    for name in os.listdir(d):
        path = os.path.join(d, name)
        if name == keep or not name.endswith('.json'):
            continue
        try:
            with open(path) as fh:
                o = json.load(fh)
            at = datetime.datetime.fromisoformat(o['updated'].replace('Z', '+00:00'))
            if at.tzinfo is None:                        # a hand-edited time without a zone: read it as UTC
                at = at.replace(tzinfo=datetime.timezone.utc)
            old = (cut - at).total_seconds() > 3600 * PRUNE_HOURS.get(o.get('state'), 24)
        except (ValueError, KeyError, TypeError, AttributeError, OSError):
            continue
        if old:
            os.remove(path)
            gone = True
    return gone


def apply(work, a):
    """Change this thread's file; returns False when there is nothing to commit."""
    os.makedirs(os.path.join(work, DIR), exist_ok=True)
    path = os.path.join(work, DIR, a.id + '.json')
    pruned = prune(work, a.id + '.json')
    if a.clear:
        if not os.path.exists(path):
            return pruned
        os.remove(path)
        return True
    old = {}
    if os.path.exists(path):
        with open(path) as fh:
            try:
                old = json.load(fh)
            except ValueError:
                old = {}
    t = now()
    new = dict(old)
    new['id'] = a.id
    if a.thread is not None:
        new['thread'] = a.thread
    new.setdefault('thread', a.id)
    if a.state is not None and a.state != old.get('state'):
        new['state'] = a.state
        new['started'] = t
    new.setdefault('state', 'building')
    new.setdefault('started', t)
    if a.step is not None:
        new['step'] = a.step
    helpers = [] if a.helpers_clear else list(old.get('helpers', []))
    for h in [x.strip() for x in a.helper_done or [] if x.strip()]:   # an empty value never clears them all
        helpers = [x for x in helpers if not x.get('task', '').lower().startswith(h.lower())]
    for h in a.helper or []:
        task, _, model = h.partition('|')
        helpers = [x for x in helpers if x.get('task') != task.strip()]
        helpers.append({'task': task.strip(), 'model': model.strip() or 'Opus'})
    new['helpers'] = helpers
    new['updated'] = t
    with open(path, 'w') as fh:
        json.dump(new, fh, indent=2, ensure_ascii=False)
        fh.write('\n')
    return True


def main():
    p = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    p.add_argument('--id', help="the thread's id (its root message id) or a short slug; one file per id")
    p.add_argument('--thread', help="the thread's title as the person sees it")
    p.add_argument('--state', choices=STATES)
    p.add_argument('--step', help='one line: what is happening now')
    p.add_argument('--helper', action='append', help='add a sub-agent: "task|Model"')
    p.add_argument('--helper-done', action='append', help='remove the sub-agent whose task starts with this text')
    p.add_argument('--helpers-clear', action='store_true', help='remove every sub-agent')
    p.add_argument('--clear', action='store_true', help="remove this thread's file (the thread is finished)")
    p.add_argument('--list', action='store_true', help="print every thread's file and exit")
    p.add_argument('--repo-dir', default=os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')),
                   help='a checkout whose origin is the repository (default: this one)')
    a = p.parse_args()
    url = origin_url(a.repo_dir)
    work = tempfile.mkdtemp(prefix='rw-status-')
    try:
        if a.list:
            if checkout(work, url) and os.path.isdir(os.path.join(work, DIR)):
                for name in sorted(os.listdir(os.path.join(work, DIR))):
                    with open(os.path.join(work, DIR, name)) as fh:
                        print(fh.read().strip())
            return
        if not a.id:
            p.error('--id is needed')
        a.id = safe_id(a.id)
        for attempt in range(5):
            checkout(work, url)
            if not apply(work, a):
                print('nothing to change')
                return
            git(work, 'add', '-A')
            git(work, '-c', 'user.name=Rogue Waves status', '-c', 'user.email=noreply@anthropic.com',
                'commit', '-q', '-m', 'status: %s %s' % (a.id, 'cleared' if a.clear else (a.state or 'update')))
            r = git(work, 'push', '-q', 'origin', 'HEAD:refs/heads/' + BRANCH, check=False)
            if r.returncode == 0:
                print('status %s: %s' % ('cleared' if a.clear else 'written', a.id))
                return
            if attempt == 4:
                sys.exit('push failed: ' + (r.stderr or r.stdout).strip())
            time.sleep(2 ** (attempt + 1))                 # someone else wrote first, or the network blinked: redo on the new tip
    finally:
        shutil.rmtree(work, ignore_errors=True)


if __name__ == '__main__':
    main()
