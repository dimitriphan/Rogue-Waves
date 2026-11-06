package com.roguewaves.status

import android.content.Context
import org.json.JSONArray

/** The token and the last thing read from GitHub, in the app's private preferences. */
class Store(context: Context) {
    private val p = context.getSharedPreferences("status", Context.MODE_PRIVATE)

    var token: String
        get() = p.getString("token", "") ?: ""
        set(v) = p.edit().putString("token", v.trim()).apply()

    /** The files from the last good read. */
    val files: JSONArray
        get() = try {
            JSONArray(p.getString("files", "[]"))
        } catch (e: Exception) {
            JSONArray()
        }

    /** When the last good read happened (epoch ms), 0 for never. */
    val fetchedMs: Long get() = p.getLong("fetched", 0L)

    /** The last failed read's message, empty after a good read. */
    val error: String get() = p.getString("error", "") ?: ""

    val errorMs: Long get() = p.getLong("errorAt", 0L)

    fun saveGood(files: JSONArray, atMs: Long) {
        p.edit().putString("files", files.toString()).putLong("fetched", atMs).putString("error", "").apply()
    }

    fun saveError(message: String, atMs: Long) {
        p.edit().putString("error", message).putLong("errorAt", atMs).apply()
    }
}
