// Rogue Waves batch button: a "Batch checks" band above the prompt with Plan (free) and Run buttons
// that start the project's batch checks tool (TDS_Mod/Tools/batch), and a pane with the run's output.
// On Windows it runs Run-Batch.ps1 (which needs Python 3.10+ and the key saved once with -SaveKey);
// elsewhere it runs run_checks.py with python3. Run = Haiku 5.5, only what changed since each check's
// last run, at most $3 of API credit, the morning routine's settings. Nothing runs without a press.
import { atom, read, update } from 'claude-code'
import type { EngineInterface, Register } from 'claude-code'

import type { BatchRun } from '../types'

const PANE = 'rw-batch'
const SCRIPT = 'TDS_Mod/Tools/batch/run_checks.py'
const WRAPPER = 'TDS_Mod/Tools/batch/Run-Batch.ps1'
const MAX_COST = '3'
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
let repoPath = ''

async function findRepo($: EngineInterface): Promise<string | null> {
  const configured = repoPath.trim()
  const root = (configured || (await $.session.cwd())).replace(/[\\/]+$/, '')
  const found = await $.fs.stat(`${root}/${SCRIPT}`).catch(() => undefined)

  return found?.kind === 'file' ? root : null
}

async function isWindows($: EngineInterface): Promise<boolean> {
  return (await $.env.get('OS')) === 'Windows_NT'
}

async function argvFor($: EngineInterface, root: string, kind: Kind): Promise<string[]> {
  if (await isWindows($)) {
    const flags = kind === 'plan' ? ['-Plan'] : kind === 'full' ? ['-Full', '-MaxCost', MAX_COST] : []
    return ['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', `${root}/${WRAPPER}`, ...flags]
  }
  const since = kind === 'full' ? [] : ['--since', 'last']
  if (kind === 'plan') {
    return ['python3', SCRIPT, 'plan', ...since, '--model', 'haiku']
  }
  return ['python3', SCRIPT, 'submit', ...since, '--model', 'haiku', '--max-cost', MAX_COST, '--max-wait', '90']
}

function append($: EngineInterface, text: string) {
  return update($, run, (r): BatchRun => ({ ...r, lines: [...r.lines, ...text.replace(/\r/g, '').split('\n')].slice(-KEEP_LINES) }))
}

async function start($: EngineInterface, kind: Kind): Promise<void> {
  const current = await read($, run)
  if (current.phase === 'running') {
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

  void (async () => {
    let code: number | null = null
    try {
      // The cloud container has python3 but not always the SDK; the Windows wrapper installs its own.
      if (kind !== 'plan' && !(await isWindows($))) {
        const has = await $.process.run(['python3', '-c', 'import anthropic'], { cwd: root })
        if (has.exitCode !== 0) {
          await append($, 'Installing the anthropic package, once...')
          const pip = await $.process.run(['python3', '-m', 'pip', 'install', '--quiet', 'anthropic'], {
            cwd: root,
            timeoutMs: 300000,
          })
          if (pip.exitCode !== 0) {
            await append($, pip.stderr || 'pip could not install anthropic.')
            throw new Error('pip failed')
          }
        }
      }
      const child = $.process.spawn({ argv: await argvFor($, root, kind), cwd: root })
      for await (const piece of child) {
        await append($, piece.text)
        if (isStopAsked) {
          break // leaving the loop kills the child
        }
      }
      if (isStopAsked) {
        await append($, 'Stopped by you. A batch already sent keeps running at Anthropic; the report can be collected later (README, collect).')
      } else {
        code = (await child.result).code
      }
    } catch (error) {
      await append($, `Could not run: ${error instanceof Error ? error.message : String(error)}`)
      code = code ?? 1
    }
    const endedAt = await $.clock.now()
    const isOk = code === 0 && !isStopAsked
    await update($, run, (r): BatchRun => ({ ...r, phase: isOk ? 'done' : 'failed', code, endedAt }))
    $.ui.status(undefined)
    $.ui.toast(isOk ? `Batch ${LABEL[kind].toLowerCase()} finished.` : `Batch ${LABEL[kind].toLowerCase()} ended (exit ${code ?? 'stopped'}).`)
  })()
}

export const register: Register = (on, options) => {
  repoPath = typeof options?.repoPath === 'string' ? options.repoPath : ''

  on('session.start', async ($, e, next) => {
    const started = await next(e)
    await update($, repo, () => null)
    const root = await findRepo($)
    await update($, repo, () => root)
    const last = await read($, run)
    // A run cut off by a reload or a restart left no child behind: show it as ended.
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
      return { text: 'Batch plan started (free): see the Batch checks pane. Use /run-batch run to spend credit.' }
    }
    if (word === 'run' || word === 'full') {
      await start($, word)
      return { text: `Batch ${word} started (Haiku, at most $${MAX_COST}): see the Batch checks pane.` }
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
      : r.phase === 'done' ? `${LABEL[r.kind]} finished. The report is under docs/reports (on Windows, Desktop\\RogueWave\\batch).`
      : r.phase === 'failed' ? `${LABEL[r.kind]} ended with exit ${r.code ?? 'stopped'}${EXIT_WHY[r.code ?? -1] ?? ''}.`
      : 'No run yet. Press Plan (free) or Run above the prompt.'

    return (
      <Box flexDirection="column">
        <Text bold>{head}</Text>
        {r.lines.filter(line => line.trim() !== '').slice(-room).map(line => <Text>{line}</Text>)}
      </Box>
    )
  })
}
