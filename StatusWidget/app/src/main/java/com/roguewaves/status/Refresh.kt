package com.roguewaves.status

import android.content.Context
import androidx.work.Constraints
import androidx.work.CoroutineWorker
import androidx.work.ExistingPeriodicWorkPolicy
import androidx.work.ExistingWorkPolicy
import androidx.work.NetworkType
import androidx.work.OneTimeWorkRequestBuilder
import androidx.work.OutOfQuotaPolicy
import androidx.work.PeriodicWorkRequestBuilder
import androidx.work.WorkManager
import androidx.work.WorkerParameters
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.util.concurrent.TimeUnit

/** Reads the feed, keeps it, redraws every widget. */
object Refresh {
    private const val PERIODIC = "status-every-15-minutes"
    private const val NOW = "status-now"

    private val online = Constraints.Builder().setRequiredNetworkType(NetworkType.CONNECTED).build()

    /** The 15-minute read (Android's shortest background period). KEEP, so a widget update never restarts it. */
    fun schedule(context: Context) {
        val req = PeriodicWorkRequestBuilder<RefreshWorker>(15, TimeUnit.MINUTES).setConstraints(online).build()
        WorkManager.getInstance(context).enqueueUniquePeriodicWork(PERIODIC, ExistingPeriodicWorkPolicy.KEEP, req)
    }

    /** A read right away: a tap on the widget, or the app's buttons. */
    fun now(context: Context) {
        val req = OneTimeWorkRequestBuilder<RefreshWorker>()
            .setConstraints(online)
            .setExpedited(OutOfQuotaPolicy.RUN_AS_NON_EXPEDITED_WORK_REQUEST)
            .build()
        WorkManager.getInstance(context).enqueueUniqueWork(NOW, ExistingWorkPolicy.REPLACE, req)
    }

    fun cancel(context: Context) {
        WorkManager.getInstance(context).cancelUniqueWork(PERIODIC)
        WorkManager.getInstance(context).cancelUniqueWork(NOW)
    }

    /** Blocking: call off the main thread. Returns a line for the app's screen. */
    fun readOnce(context: Context): String {
        val store = Store(context)
        val token = store.token
        val now = System.currentTimeMillis()
        val line = try {
            val files = Feed.fetch(token)
            store.saveGood(files, now)
            val entries = Feed.parse(files)
            if (entries.isEmpty()) "Read the status branch: no thread has reported yet."
            else "Read ${entries.size} thread file(s):\n" + entries.joinToString("\n") { "• ${it.thread}: ${it.state}" }
        } catch (e: Exception) {
            val msg = e.message ?: e.javaClass.simpleName
            store.saveError(msg, now)
            "Couldn't read the status: $msg"
        }
        Renderer.updateAll(context)
        return line
    }
}

class RefreshWorker(context: Context, params: WorkerParameters) : CoroutineWorker(context, params) {
    override suspend fun doWork(): Result = withContext(Dispatchers.IO) {
        Refresh.readOnce(applicationContext)
        Result.success()
    }
}
