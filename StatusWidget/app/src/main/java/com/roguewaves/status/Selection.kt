package com.roguewaves.status

// What the widget shows, worked out from the threads' status files. Plain Kotlin (no Android), so the rules can be
// tested off the phone: StatusWidget/README.md "What it shows".

data class Helper(val task: String, val model: String)

data class Entry(
    val id: String,
    val thread: String,
    val state: String,
    val step: String,
    val startedMs: Long?,
    val updatedMs: Long?,
    val helpers: List<Helper>,
)

data class Board(val main: Entry?, val others: List<Entry>)

object Selection {
    const val MINUTE = 60_000L

    /** Busy first; "your turn" leads because it is the one thing that needs the person. */
    val PRIORITY = listOf("waiting", "failed", "testing", "building", "reviewing", "art", "shipped")

    /** How long a state counts without a new write. Null: until the thread changes it. */
    fun lifetimeMs(state: String): Long? = when (state) {
        "waiting" -> null
        "testing", "failed" -> 120 * MINUTE       // a PC self-test can run long without a new step
        else -> 30 * MINUTE                      // building, reviewing, art; shipped shows 30 minutes after the merge
    }

    fun isLive(e: Entry, nowMs: Long): Boolean {
        if (e.state !in PRIORITY) return false
        val life = lifetimeMs(e.state) ?: return true
        val at = e.updatedMs ?: return false
        return nowMs - at <= life
    }

    fun board(entries: List<Entry>, nowMs: Long): Board {
        val live = entries.filter { isLive(it, nowMs) }
            .sortedWith(compareBy<Entry> { PRIORITY.indexOf(it.state) }.thenByDescending { it.updatedMs ?: 0L })
        return Board(live.firstOrNull(), live.drop(1).take(3))
    }
}
