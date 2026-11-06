package com.roguewaves.status

import android.app.PendingIntent
import android.appwidget.AppWidgetManager
import android.content.ComponentName
import android.content.Context
import android.content.Intent
import android.graphics.Bitmap
import android.graphics.Canvas
import android.graphics.Paint
import android.util.TypedValue
import android.view.View
import android.widget.RemoteViews
import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Date
import java.util.Locale

/** Draws the widget from what Store holds. */
object Renderer {
    const val FRAMES = 12

    /** How each state reads on the widget. Scene frames: res/drawable/f_<scene>_<nn>.xml (StatusWidget/art/scenes.py). */
    private class Look(
        val scene: String, val title: String, val verb: String, val head: Int,
        val chip: String, val chipBg: Int, val chipFg: Int, val alarm: Boolean = false,
    )

    private val LOOKS = mapOf(
        "idle" to Look("idle", "IDLE", "", R.drawable.h_boat, "Idle", R.drawable.chip_mist, R.color.swell),
        "building" to Look("building", "BUILDING", "Spool is stitching", R.drawable.h_spool, "Building", R.drawable.chip_sand, R.color.swell),
        "testing" to Look("testing", "TESTING", "Argo is diving", R.drawable.h_argo, "Testing", R.drawable.chip_teal, R.color.white),
        "reviewing" to Look("reviewing", "REVIEWING", "Yossele is reading", R.drawable.h_yossele, "Reviewing", R.drawable.chip_mist, R.color.swell),
        "art" to Look("art", "DRAWING", "Uloe is lighting the canvas", R.drawable.h_uloe, "Drawing", R.drawable.chip_mist, R.color.swell),
        "waiting" to Look("waiting", "YOUR TURN", "The crew needs you", R.drawable.h_sign, "Your turn", R.drawable.chip_coral, R.color.white, alarm = true),
        "shipped" to Look("shipped", "SHIPPED", "Shipped", R.drawable.h_boat, "Shipped", R.drawable.chip_teal, R.color.white),
        "failed" to Look("failed", "FAILED", "Spool got tangled", R.drawable.h_spool, "Failed", R.drawable.chip_coral, R.color.white, alarm = true),
    )

    private val ROWS = listOf(
        intArrayOf(R.id.row1, R.id.row1_head, R.id.row1_title, R.id.row1_helpers, R.id.row1_chip),
        intArrayOf(R.id.row2, R.id.row2_head, R.id.row2_title, R.id.row2_helpers, R.id.row2_chip),
        intArrayOf(R.id.row3, R.id.row3_head, R.id.row3_title, R.id.row3_helpers, R.id.row3_chip),
    )

    fun updateAll(context: Context) {
        val mgr = AppWidgetManager.getInstance(context)
        val ids = mgr.getAppWidgetIds(ComponentName(context, StatusWidgetProvider::class.java))
        if (ids.isEmpty()) return
        mgr.updateAppWidget(ids, build(context))
    }

    fun build(context: Context): RemoteViews {
        val store = Store(context)
        val now = System.currentTimeMillis()
        val entries = Feed.parse(store.files)
        val board = Selection.board(entries, now)
        val v = RemoteViews(context.packageName, R.layout.widget)
        val main = board.main
        val look = LOOKS[main?.state ?: "idle"] ?: LOOKS.getValue("idle")

        // the scene: one ViewFlipper child per frame
        v.removeAllViews(R.id.flipper)
        for (i in 0 until FRAMES) {
            val frame = RemoteViews(context.packageName, R.layout.frame)
            frame.setImageViewResource(R.id.frame, frameId(context, look.scene, i))
            v.addView(R.id.flipper, frame)
        }

        val title: String
        val line: String
        val step: String
        var since = ""
        when {
            main == null -> {
                title = look.title
                line = "The crew is asleep on deck"
                val last = entries.mapNotNull { it.updatedMs }.maxOrNull()
                step = if (last == null) "No thread has reported yet" else "Nothing busy since ${clock(last, now)}"
            }
            else -> {
                title = look.title
                line = "${look.verb}: ${main.thread}"
                step = main.step
                main.startedMs?.let { since = "since " + clock(it, now) }
            }
        }
        v.setImageViewBitmap(R.id.title, titleBitmap(context, title, look.alarm && main != null))
        v.setContentDescription(R.id.title, title)
        v.setTextViewText(R.id.line, line)
        v.setTextViewText(R.id.step, step)
        v.setViewVisibility(R.id.step, if (step.isEmpty()) View.GONE else View.VISIBLE)
        v.setTextViewText(R.id.since, since)

        // the big scene's own sub-agents, two lines at most
        val helpers = main?.helpers.orEmpty()
        listOf(R.id.helper1, R.id.helper2).forEachIndexed { i, id ->
            val h = helpers.getOrNull(i)
            if (h == null) {
                v.setViewVisibility(id, View.GONE)
            } else {
                var text = h.task + if (h.model.isNotEmpty()) "  ·  " + h.model.uppercase(Locale.ROOT) else ""
                if (i == 1 && helpers.size > 2) text += "  (+${helpers.size - 2} more)"
                v.setTextViewText(id, text)
                v.setViewVisibility(id, View.VISIBLE)
            }
        }

        // the other busy threads
        v.setViewVisibility(R.id.deck_label, if (board.others.isEmpty()) View.GONE else View.VISIBLE)
        ROWS.forEachIndexed { i, ids ->
            val e = board.others.getOrNull(i)
            if (e == null) {
                v.setViewVisibility(ids[0], View.GONE)
            } else {
                val l = LOOKS[e.state] ?: LOOKS.getValue("idle")
                v.setViewVisibility(ids[0], View.VISIBLE)
                v.setImageViewResource(ids[1], l.head)
                v.setTextViewText(ids[2], e.thread)
                v.setTextViewText(ids[3], if (e.helpers.isEmpty()) "" else "+${e.helpers.size}")
                v.setTextViewText(ids[4], l.chip)
                v.setInt(ids[4], "setBackgroundResource", l.chipBg)
                v.setTextColor(ids[4], context.getColor(l.chipFg))
            }
        }

        v.setTextViewText(R.id.updated, footer(store, now))

        val tap = Intent(context, StatusWidgetProvider::class.java).setAction(StatusWidgetProvider.ACTION_REFRESH)
        val pi = PendingIntent.getBroadcast(context, 0, tap, PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT)
        v.setOnClickPendingIntent(android.R.id.background, pi)
        return v
    }

    /** The footer after a tap, before the read comes back. */
    fun refreshing(context: Context) {
        val mgr = AppWidgetManager.getInstance(context)
        val ids = mgr.getAppWidgetIds(ComponentName(context, StatusWidgetProvider::class.java))
        val v = RemoteViews(context.packageName, R.layout.widget)
        v.setTextViewText(R.id.updated, "Refreshing…")
        for (id in ids) mgr.partiallyUpdateAppWidget(id, v)
    }

    private fun footer(store: Store, now: Long): String {
        val good = store.fetchedMs
        return when {
            store.error.isNotEmpty() && store.errorMs >= good ->
                if (good == 0L) "Couldn't read GitHub, tap the app" else "Offline, last read ${clock(good, now)}"
            good == 0L -> "Not read yet"
            else -> "Updated ${clock(good, now)}"
        }
    }

    private val frameCache = HashMap<String, Int>()

    private fun frameId(context: Context, scene: String, i: Int): Int {
        val name = "f_%s_%02d".format(Locale.ROOT, scene, i)
        return frameCache.getOrPut(name) {
            context.resources.getIdentifier(name, "drawable", context.packageName).takeIf { it != 0 } ?: R.drawable.f_idle_00
        }
    }

    /** "19:20" today, "Tue 16:34" another day. */
    private fun clock(ms: Long, now: Long): String {
        val a = Calendar.getInstance().apply { timeInMillis = ms }
        val b = Calendar.getInstance().apply { timeInMillis = now }
        val sameDay = a.get(Calendar.YEAR) == b.get(Calendar.YEAR) && a.get(Calendar.DAY_OF_YEAR) == b.get(Calendar.DAY_OF_YEAR)
        return SimpleDateFormat(if (sameDay) "HH:mm" else "EEE HH:mm", Locale.getDefault()).format(Date(ms))
    }

    /** The big state word in Exo 2 Light: drawn here, since a home screen may not load the app's fonts. */
    private fun titleBitmap(context: Context, text: String, alarm: Boolean): Bitmap {
        val dm = context.resources.displayMetrics
        val paint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            typeface = context.resources.getFont(R.font.exo2_light)
            textSize = TypedValue.applyDimension(TypedValue.COMPLEX_UNIT_SP, 36f, dm)
            letterSpacing = 0.08f
            color = context.getColor(if (alarm) R.color.coral else R.color.foam)
        }
        val h = TypedValue.applyDimension(TypedValue.COMPLEX_UNIT_DIP, 44f, dm).toInt()
        val w = (paint.measureText(text) + 4).toInt().coerceAtLeast(1)
        val bmp = Bitmap.createBitmap(w, h, Bitmap.Config.ARGB_8888)
        val fm = paint.fontMetrics
        val baseline = (h - (fm.descent - fm.ascent)) / 2f - fm.ascent
        Canvas(bmp).drawText(text, 0f, baseline, paint)
        return bmp
    }
}
