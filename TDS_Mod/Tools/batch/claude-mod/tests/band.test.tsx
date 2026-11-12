import { expect, test } from 'claude-code/testing'

const BAND = {
  plugin: 'rw-batch-button',
  component: 'AbovePrompt',
  props: { hasSurvey: false, isCollapsed: false, isWorking: false },
} as const

test('in a Rogue Waves clone the band offers Plan and Run, and Run asks before spending', async ($, on) => {
  on('command.register', () => ({ value: { command: 'run-batch' } }))
  on('session.start', (_, e) => ({ cwd: e.cwd }))
  on('session.cwd', () => ({ value: '/repo' }))
  on('fs.stat', () => ({ value: { kind: 'file', size: 1, mtimeMs: 0, isLink: false } }))
  await $.session.start({ cwd: '/repo', surface: 'desktop', isInteractive: true })

  for (const surface of ['terminal', 'desktop'] as const) {
    const ui = await $.ui.mount({ ...BAND, surface })
    expect(await ui.find({ key: 'plan' })).toBeDefined()
    await ui.press({ key: 'run' })
    expect(await ui.find({ type: 'Text', text: /up to \$3/ })).toBeDefined()
    await ui.press({ key: 'no' })
    expect(await ui.find({ key: 'run' })).toBeDefined()
    await ui.unmount()
  }
})

test('outside a Rogue Waves clone the band stays away', async ($, on) => {
  on('command.register', () => ({ value: { command: 'run-batch' } }))
  on('session.start', (_, e) => ({ cwd: e.cwd }))
  on('session.cwd', () => ({ value: '/elsewhere' }))
  on('fs.stat', () => { throw new Error('ENOENT') })
  on('ui.render', ($, e) => {
    const { Text } = $.ui.resolve(e)
    return <Text>engine</Text>
  })
  await $.session.start({ cwd: '/elsewhere', surface: 'desktop', isInteractive: true })

  const ui = await $.ui.mount({ ...BAND, surface: 'desktop' })
  expect(await ui.find({ key: 'plan' })).toBeUndefined()
  await ui.unmount()
})

test('/run-batch run only asks on the band; nothing starts', async ($, on) => {
  on('command.register', () => ({ value: { command: 'run-batch' } }))
  on('session.start', (_, e) => ({ cwd: e.cwd }))
  on('session.cwd', () => ({ value: '/repo' }))
  on('fs.stat', () => ({ value: { kind: 'file', size: 1, mtimeMs: 0, isLink: false } }))
  on('process.run', () => { throw new Error('nothing may run before the Yes') })
  await $.session.start({ cwd: '/repo', surface: 'desktop', isInteractive: true })

  const answer = await $.command.run({ command: 'run-batch', args: 'run' })
  expect(JSON.stringify(answer)).toContain('Confirm')
  const ui = await $.ui.mount({ ...BAND, surface: 'desktop' })
  expect(await ui.find({ key: 'yes' })).toBeDefined()
  await ui.unmount()
})
