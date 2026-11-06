package com.roguewaves.status

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class SelectionTest {
    private val now = 1_800_000_000_000L
    private val min = Selection.MINUTE

    private fun e(id: String, state: String, agoMin: Long, helpers: Int = 0) =
        Entry(id, id, state, "", now - agoMin * min, now - agoMin * min, List(helpers) { Helper("t$it", "Haiku") })

    @Test fun nothingBusyIsIdle() {
        val b = Selection.board(listOf(e("a", "idle", 1), e("b", "building", 31)), now)
        assertNull(b.main)
        assertEquals(0, b.others.size)
    }

    @Test fun yourTurnLeadsAndNeverGoesStale() {
        val b = Selection.board(listOf(e("t", "testing", 5), e("w", "waiting", 600), e("f", "failed", 10)), now)
        assertEquals("w", b.main!!.id)
        assertEquals(listOf("f", "t"), b.others.map { it.id })
    }

    @Test fun testingLastsTwoHoursBuildingHalfAnHour() {
        assertEquals("t", Selection.board(listOf(e("t", "testing", 119), e("b", "building", 31)), now).main!!.id)
        assertNull(Selection.board(listOf(e("t", "testing", 121)), now).main)
    }

    @Test fun shippedShowsThirtyMinutes() {
        assertEquals("s", Selection.board(listOf(e("s", "shipped", 29)), now).main!!.id)
        assertNull(Selection.board(listOf(e("s", "shipped", 31)), now).main)
    }

    @Test fun atMostThreeOthersNewestFirstWithinAState() {
        val b = Selection.board(listOf(e("b1", "building", 9), e("b2", "building", 2), e("b3", "building", 5),
            e("b4", "building", 7), e("b5", "building", 8)), now)
        assertEquals("b2", b.main!!.id)
        assertEquals(listOf("b3", "b4", "b5"), b.others.map { it.id })
    }

    @Test fun unknownStateAndMissingTimeAreIgnored() {
        val noTime = Entry("x", "x", "building", "", null, null, emptyList())
        assertNull(Selection.board(listOf(e("u", "sleeping", 1), noTime), now).main)
    }
}
