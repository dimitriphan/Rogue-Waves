export type BatchPhase = 'idle' | 'confirm' | 'running' | 'done' | 'failed'
export type BatchRun = {
  phase: BatchPhase
  kind: 'plan' | 'run' | 'full'
  lines: string[]
  code: number | null
  startedAt: number
  endedAt: number
}

declare module 'claude-code' {
  interface PluginState {
    'rw-batch-button': {
      run: Shaped<BatchRun>
      repo: string | null
    }
  }
}
