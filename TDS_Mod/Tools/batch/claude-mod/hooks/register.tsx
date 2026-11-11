// Rogue Waves batch button: a "Batch checks" band above the prompt with Plan (free), Run and Full buttons
// that start the project's batch checks tool (TDS_Mod/Tools/batch), and a pane with the run's output.
// On Windows it runs Run-Batch.ps1 -NoPull (which needs Python 3.10+, Git and the key saved once with
// -SaveKey); elsewhere it runs run_checks.py with python3. Run = Haiku 5.5, only what changed since each
// check's last run, at most $3 of API credit, the morning routine's settings. Run and Full always wait
// for the band's "Yes, run"; the command only asks for it.
import { atom, read, update } from 'claude-code'
import type { EngineInterface, Register } from 'claude-code'

import type { BatchRun } from '../types'

const PANE = 'rw-batch'
const SCRIPT = 'TDS_Mod/Tools/batch/run_checks.py'
const WRAPPER = 'TDS_Mod/Tools/batch/Run-Batch.ps1'
const MAX_COST = '3'
const MAX_WAIT = '120' // minutes, as Run-Batch.ps1 waits
const KEEP_LINES = 400
// run_checks.py's own exit codes (its README).
const EXIT_WHY: Record<number, string> = {
  3: ': no API key (RW_BATCH_API_KEY; on Windows run Run-Batch.ps1 -SaveKey once)',
  4: ': over the $3 cap, nothing was sent',
}

const IDLE: BatchRun = { phase: 'idle', kind: 'run', lines: [], code: null, startedAt: 0, endedAt: 0 }
const run = atom({ plugin: 'rw-batch-button', key: 'run' } as const, IDLE, { shape: 'run-1' })
const repo = atom({ plugin: 'rw-batch-button', key: 'repo' } as const, null)

type Kind = BatchRun['kind']
const LABEL: Record<Kind, string> = { plan: 'Plan (free)', run: 'Run', full: 'Full run' }

// The running child's loop leaves when this is set; leaving it kills the child.
let isStopAsked = false
// Held from the press to the 'running' write, so a double press starts one child.
let isStarting = false
let repoPath = ''

// The configured clone, else the session's folder, else a clone named Rogue-Waves in the home folder
// (so the band shows in every session on a PC that keeps one there).
async function findRepo($: EngineInterface): Promise<string | null> {
  const configured = repoPath.trim()
  const home = (await $.env.get('USERPROFILE').catch(() => undefined)) ?? (await $.env.get('HOME').catch(() => undefined))
  const places = configured ? [configured] : [await $.session.cwd(), ...(home ? [`${home}/Rogue-Waves`] : [])]
  for (const place of places) {
    const root = place.replace(/[\\/]+$/, '')
    const found = await $.fs.stat(`${root}/${SCRIPT}`).catch(() => undefined)
    if (found?.kind === 'file') {
      return root
    }
  }

  return null
}

async function isWindows($: EngineInterface): Promise<boolean> {
  return (await $.env.get('OS')) === 'Windows_NT'
}

function argvFor(onWindows: boolean, root: string, kind: Kind): string[] {
  if (onWindows) {
    // -NoPull: the button never moves the clone's branch. The wrapper moves the report to Desktop\RogueWave\batch.
    const flags = kind === 'plan' ? ['-Plan'] : kind === 'full' ? ['-Full', '-MaxCost', MAX_COST] : []
    return ['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', `${root}/${WRAPPER}`, '-NoPull', ...flags]
  }
  const since = kind === 'full' ? [] : ['--since', 'last']
  if (kind === 'plan') {
    return ['python3', SCRIPT, 'plan', ...since, '--model', 'haiku']
  }
  return ['python3', SCRIPT, 'submit', ...since, '--model', 'haiku', '--max-cost', MAX_COST, '--max-wait', MAX_WAIT]
}

function append($: EngineInterface, lines: string[]) {
  const kept = lines.filter(line => line.trim() !== '')
  return update($, run, (r): BatchRun => ({ ...r, lines: [...r.lines, ...kept].slice(-KEEP_LINES) }))
}

// Pieces end wherever the child's write did: each pipe's unfinished line waits for its newline.
function splitter() {
  const partial = { stdout: '', stderr: '' }
  return {
    take(stream: 'stdout' | 'stderr', text: string): string[] {
      const parts = (partial[stream] + text.replace(/\r/g, '')).split('\n')
      partial[stream] = parts.pop() ?? ''
      return parts
    },
    rest: (): string[] => [partial.stdout, partial.stderr],
  }
}

// Stop on Windows: killing powershell leaves the python it started, so end that one by its command line.
async function stopWindowsPython($: EngineInterface): Promise<void> {
  const script =
    "Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -like '*run_checks.py*' -and $_.Name -like 'py*' } " +
    '| ForEach-Object { Stop-Process -Id $_.ProcessId -Force }'
  await $.process.run(['powershell', '-NoProfile', '-Command', script]).catch(() => undefined)
}

// Off Windows: the SDK and Pillow (the art check imports it), once; --user where the system Python is managed.
async function ensurePackages($: EngineInterface, root: string): Promise<void> {
  const has = await $.process.run(['python3', '-c', 'import anthropic, PIL'], { cwd: root })
  if (has.exitCode === 0) {
    return
  }
  await append($, ['Installing the Python packages (anthropic, pillow), once...'])
  const base = ['python3', '-m', 'pip', 'install', '--quiet']
  let pip = await $.process.run([...base, 'anthropic', 'pillow'], { cwd: root, timeoutMs: 300000 })
  if (pip.exitCode !== 0) {
    pip = await $.process.run([...base, '--user', 'anthropic', 'pillow'], { cwd: root, timeoutMs: 300000 })
  }
  if (pip.exitCode !== 0) {
    await append($, (pip.stderr || 'pip could not install anthropic and pillow.').split('\n'))
    throw new Error('pip failed')
  }
}

async function start($: EngineInterface, kind: Kind): Promise<void> {
  if (isStarting) {
    return
  }
  isStarting = true
  try {
    if ((await read($, run)).phase === 'running') {
      $.ui.toast('A batch run is already going.')
      return
    }
    const root = await findRepo($)
    if (!root) {
      $.ui.toast(`No ${SCRIPT} here: open a Rogue Waves clone, or set the clone folder in /config.`)
      return
    }
    isStopAsked = false
    const startedAt = await $.clock.now()
    await update($, run, (): BatchRun => ({ phase: 'running', kind, lines: [], code: null, startedAt, endedAt: 0 }))
    void $.ui.open({ id: PANE, title: 'Batch checks' })
    $.ui.status(`Batch ${LABEL[kind].toLowerCase()} running`)
    void watch($, root, kind)
  } finally {
    isStarting = false
  }
}

// Runs the child for as long as it lives, after the press that started it has returned.
async function watch($: EngineInterface, root: string, kind: Kind): Promise<void> {
  let code: number | null = null
  try {
    const onWindows = await isWindows($)
    if (!onWindows) {
      await ensurePackages($, root) // the Windows wrapper installs its own
    }
    if (!isStopAsked) {
      const child = $.process.spawn({ argv: argvFor(onWindows, root, kind), cwd: root })
      const lines = splitter()
      for await (const piece of child) {
        await append($, lines.take(piece.stream, piece.text))
        if (isStopAsked) {
          break // leaving the loop kills the child
        }
      }
      await append($, lines.rest())
      if (!isStopAsked) {
        code = (await child.result).code
      } else if (onWindows) {
        await stopWindowsPython($)
      }
    }
    if (isStopAsked) {
      await append($, ['Stopped by you. A batch already sent keeps running at Anthropic: run_checks.py collect <its msgbatch id> finishes it.'])
    }
  } catch (error) {
    await append($, [`Could not run: ${error instanceof Error ? error.message : String(error)}`])
    code = code ?? 1
  }
  const endedAt = await $.clock.now()
  const isOk = code === 0 && !isStopAsked
  await update($, run, (r): BatchRun => ({ ...r, phase: isOk ? 'done' : 'failed', code, endedAt }))
  $.ui.status(undefined)
  const name = `Batch ${LABEL[kind].toLowerCase()}`
  $.ui.toast(isOk ? `${name} finished.` : `${name} ended (exit ${code ?? 'stopped'}).`)
}

export const register: Register = (on, options) => {
  repoPath = typeof options?.repoPath === 'string' ? options.repoPath : ''

  on('session.start', async ($, e, next) => {
    const started = await next(e)
    await update($, repo, () => null)
    const root = await findRepo($)
    await update($, repo, () => root)
    // A run cut off by a reload or a restart has no child any more: show it as ended.
    const last = await read($, run)
    if (last.phase === 'running' || last.phase === 'confirm') {
      await update($, run, (r): BatchRun => ({ ...r, phase: r.phase === 'confirm' ? 'idle' : 'failed' }))
    }
    await $.command.register({
      name: 'run-batch',
      description: 'Rogue Waves batch checks: plan (free), run (Haiku, changed only, at most $3), full, stop',
    })

    return started
  })

  on('command.run', { command: 'run-batch' }, async ($, e) => {
    const word = e.args.trim().toLowerCase()
    if (word === 'stop') {
      isStopAsked = true
      return { text: 'Stopping the batch run.' }
    }
    if (word === 'plan' || word === '') {
      await start($, 'plan')
      return { text: 'Batch plan started (free): see the Batch checks pane. /run-batch run spends credit.' }
    }
    if (word === 'run' || word === 'full') {
      // Spending always waits for the band's "Yes, run".
      await update($, run, (x): BatchRun => (x.phase === 'running' ? x : { ...x, phase: 'confirm', kind: word }))
      return { text: `Confirm the batch ${word} (Haiku, at most $${MAX_COST}) on the band above the prompt.` }
    }
    return { text: 'Use /run-batch plan, run, full or stop.' }
  })

  on('ui.render', { component: 'AbovePrompt' }, async ($, e, next) => {
    const root = await read($, repo)
    if (e.props.hasSurvey || root === null) {
      return next(e)
    }
    const { Box, Button, Text } = $.ui.resolve(e)
    const r = await read($, run)
    const show = () => void $.ui.open({ id: PANE, title: 'Batch checks' })

    if (r.phase === 'confirm') {
      return (
        <Box>
          <Text>
            {r.kind === 'full' ? 'Full batch run' : 'Batch run'}: Haiku 5.5, up to ${MAX_COST} of API credit (usually $1-2).{' '}
          </Text>
          <Button key="yes" label="Yes, run" onPress={() => start($, r.kind)} />
          <Text> </Text>
          <Button key="no" label="Cancel" onPress={() => update($, run, (x): BatchRun => ({ ...x, phase: 'idle' }))} />
        </Box>
      )
    }
    if (r.phase === 'running') {
      const minutes = Math.max(0, Math.floor(((await $.clock.now()) - r.startedAt) / 60000))
      return (
        <Box>
          <Text>Batch {LABEL[r.kind].toLowerCase()} running, {minutes} min{' '}</Text>
          <Button key="show" label="Output" onPress={show} />
          <Text> </Text>
          <Button key="stop" label="Stop" onPress={() => { isStopAsked = true }} />
        </Box>
      )
    }
    const ask = (kind: Kind) => () => update($, run, (x): BatchRun => ({ ...x, phase: 'confirm', kind }))

    return (
      <Box>
        <Text dimColor>
          Batch checks{r.phase === 'done' ? ' (last: done)' : r.phase === 'failed' ? ` (last: exit ${r.code ?? 'stopped'})` : ''}{' '}
        </Text>
        <Button key="plan" label="Plan (free)" onPress={() => start($, 'plan')} />
        <Text> </Text>
        <Button key="run" label="Run" onPress={ask('run')} />
        <Text> </Text>
        <Button key="full" label="Full" onPress={ask('full')} />
        {r.lines.length > 0 && <Text> </Text>}
        {r.lines.length > 0 && <Button key="show" label="Output" onPress={show} />}
      </Box>
    )
  })

  on('ui.render', { component: 'Pane', requestId: PANE }, async ($, e) => {
    const { Box, Text } = $.ui.resolve(e)
    const r = await read($, run)
    const room = Math.max(4, (e.viewport?.rows ?? 30) - 3)
    const head =
      r.phase === 'running' ? `${LABEL[r.kind]} running...`
      : r.phase === 'done' ? `${LABEL[r.kind]} finished. Its last lines name the report, or the batch id to collect.`
      : r.phase === 'failed' ? `${LABEL[r.kind]} ended with exit ${r.code ?? 'stopped'}${EXIT_WHY[r.code ?? -1] ?? ''}.`
      : 'No run yet. Press Plan (free) or Run above the prompt.'

    return (
      <Box flexDirection="column">
        <Text bold>{head}</Text>
        {r.lines.slice(-room).map(line => <Text>{line}</Text>)}
      </Box>
    )
  })
}
