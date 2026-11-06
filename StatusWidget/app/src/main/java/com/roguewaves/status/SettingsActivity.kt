package com.roguewaves.status

import android.app.Activity
import android.os.Bundle
import android.widget.Button
import android.widget.EditText
import android.widget.TextView
import kotlin.concurrent.thread

/** The app's one screen: paste the token, read the feed once, see what came back. */
class SettingsActivity : Activity() {
    private lateinit var result: TextView

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_settings)
        val store = Store(this)
        val token = findViewById<EditText>(R.id.token)
        result = findViewById(R.id.result)
        if (store.token.isNotEmpty()) token.hint = "Token saved (paste a new one to replace it)"
        result.text = if (store.error.isNotEmpty()) "Last read failed: ${store.error}" else ""

        findViewById<Button>(R.id.save).setOnClickListener {
            val t = token.text.toString().trim()
            store.token = t                                   // empty clears it: the public repository reads without one
            token.setText("")
            token.hint = if (t.isEmpty()) "github_pat_… (optional)" else "Token saved (paste a new one to replace it)"
            read()
        }
        findViewById<Button>(R.id.refresh).setOnClickListener { read() }
        Refresh.schedule(this)
    }

    private fun read() {
        result.text = "Reading…"
        thread {
            val line = Refresh.readOnce(applicationContext)
            runOnUiThread { result.text = line }
        }
    }
}
