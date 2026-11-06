package com.roguewaves.status

import android.appwidget.AppWidgetManager
import android.appwidget.AppWidgetProvider
import android.content.Context
import android.content.Intent
import android.os.Bundle

class StatusWidgetProvider : AppWidgetProvider() {
    companion object {
        const val ACTION_REFRESH = "com.roguewaves.status.REFRESH"
    }

    // onUpdate draws from what is stored and keeps the 15-minute read alive (KEEP). It never starts a one-time read:
    // a finished one-time job toggles WorkManager's own receiver, the launcher answers with another update, and the
    // widget would refresh itself in a loop.
    override fun onUpdate(context: Context, manager: AppWidgetManager, ids: IntArray) {
        Refresh.schedule(context)
        manager.updateAppWidget(ids, Renderer.build(context))
    }

    override fun onAppWidgetOptionsChanged(context: Context, manager: AppWidgetManager, id: Int, options: Bundle) {
        manager.updateAppWidget(id, Renderer.build(context))
    }

    override fun onEnabled(context: Context) {
        Refresh.schedule(context)
        Refresh.now(context)
    }

    override fun onDisabled(context: Context) {
        Refresh.cancel(context)
    }

    override fun onReceive(context: Context, intent: Intent) {
        if (intent.action == ACTION_REFRESH) {
            Renderer.refreshing(context)
            Refresh.now(context)
            return
        }
        super.onReceive(context, intent)
    }
}
