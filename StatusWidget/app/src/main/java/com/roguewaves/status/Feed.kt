package com.roguewaves.status

import org.json.JSONArray
import org.json.JSONObject
import java.io.IOException
import java.net.HttpURLConnection
import java.net.URL
import java.time.Instant

/** Reads every threads/<id>.json on the repository's status branch (written by StatusWidget/status/set_status.py). */
object Feed {
    const val OWNER = "dimitriphan"
    const val REPO = "Rogue-Waves"
    const val BRANCH = "status"
    const val DIR = "threads"
    private const val API = "https://api.github.com/repos/$OWNER/$REPO/contents/$DIR?ref=$BRANCH"
    private const val MAX_FILES = 40

    class FeedError(message: String) : IOException(message)

    /** The raw files, as one JSON array of objects; an empty array when the branch or folder isn't there yet.
     *  The repository is public, so the token is optional: without one GitHub allows 60 reads an hour. */
    fun fetch(token: String): JSONArray {
        val (code, listing) = get(API, token, raw = false)
        when (code) {
            200 -> {}
            404 -> return JSONArray()
            401 -> throw FeedError("GitHub refused the token (401). Paste a new one, or clear it to read without one.")
            403, 429 -> throw FeedError("GitHub said no ($code): the hourly limit, or a token without Contents: read-only.")
            else -> throw FeedError("GitHub answered $code.")
        }
        val files = JSONArray(listing)
        val out = JSONArray()
        for (i in 0 until minOf(files.length(), MAX_FILES)) {
            val f = files.getJSONObject(i)
            if (f.optString("type") != "file" || !f.optString("name").endsWith(".json")) continue
            // the files themselves come from raw.githubusercontent.com, which doesn't count against the hourly API limit
            val raw = f.optString("download_url")
            val (c, body) = if (raw.startsWith("https://")) get(raw, token, raw = true) else get(f.getString("url"), token, raw = true)
            if (c != 200) continue
            try {
                out.put(JSONObject(body))
            } catch (e: Exception) {
                // a half-written or hand-edited file: skip it, keep the rest
            }
        }
        return out
    }

    fun parse(files: JSONArray): List<Entry> {
        val out = ArrayList<Entry>()
        for (i in 0 until files.length()) {
            val o = files.optJSONObject(i) ?: continue
            val hs = o.optJSONArray("helpers")
            val helpers = ArrayList<Helper>()
            if (hs != null) for (j in 0 until hs.length()) {
                val h = hs.optJSONObject(j) ?: continue
                helpers.add(Helper(h.optString("task"), h.optString("model")))
            }
            out.add(
                Entry(
                    id = o.optString("id"),
                    thread = o.optString("thread", o.optString("id")),
                    state = o.optString("state"),
                    step = o.optString("step"),
                    startedMs = time(o.optString("started")),
                    updatedMs = time(o.optString("updated")),
                    helpers = helpers,
                )
            )
        }
        return out
    }

    private fun time(s: String): Long? = try {
        if (s.isEmpty()) null else Instant.parse(s).toEpochMilli()
    } catch (e: Exception) {
        null
    }

    private fun get(url: String, token: String, raw: Boolean): Pair<Int, String> {
        val c = URL(url).openConnection() as HttpURLConnection
        try {
            c.connectTimeout = 15_000
            c.readTimeout = 15_000
            if (token.isNotEmpty()) c.setRequestProperty("Authorization", "Bearer $token")
            c.setRequestProperty("Accept", if (raw) "application/vnd.github.raw+json" else "application/vnd.github+json")
            c.setRequestProperty("X-GitHub-Api-Version", "2022-11-28")
            c.setRequestProperty("User-Agent", "RogueWavesStatus")
            val code = c.responseCode
            val stream = if (code in 200..299) c.inputStream else c.errorStream
            val body = stream?.bufferedReader()?.use { it.readText() } ?: ""
            return code to body
        } finally {
            c.disconnect()
        }
    }
}
