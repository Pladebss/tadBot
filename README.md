# 📊 TadBot – Discord Booster + PC Stats Bot

A production-ready Discord bot built with **discord.js** that:

* Monitors boost messages
* Routes automated follow commands
* Supports restart/shutdown control
* Tracks PC performance in real time
* Generates interactive CPU / RAM / GPU graphs
* Persists stats history to disk
* Displays downtime gaps visually
* Automatically down-samples long-range graphs

---

# 🚀 Features

## 🔁 Booster Monitor

* Watches a single `monitorChannelId`
* Detects boost messages in:

  * Plain text
  * Embed titles
  * Embed descriptions
  * Embed fields
* Maps boost targets using a configurable mapping table
* Sends corresponding `FollowTo <Field>` messages to configured channels
* Updates bot presence:

  * Active when gathering
  * DND + “Standing By…” after 15 minutes

---

## 🖥 `/stats` – Interactive PC Monitoring

Slash command:

```
/stats
/stats range:10m
/stats range:1h
/stats range:1d
/stats range:1w
/stats range:30s
```

### What it shows:

* CPU %
* Memory %
* GPU %
* Uptime
* OS + hardware info

### Graph Behavior:

* Smart X-axis formatting:

  * Weeks → show days
  * Days → show hours
  * Hours → show every 5 minutes
  * Minutes → show every 5 seconds
* Gaps are visible (bot restarts show broken lines)
* Max 2000 visible points (performance cap)
* Full data stored internally

---

## 💾 Persistent Stats Storage

Stats are stored in:

```
/data/stats.json
```

Behavior:

* Samples every 5 seconds
* Keeps up to 1 week of history
* Saves to disk every 30 seconds
* Atomic writes (safe against corruption)
* Automatically loads on startup

---

## ⚙️ Configuration

Create:

```
config.json
```

Example:

```json
{
  "monitorChannelId": "1234567890",
  "boostOutputChannelIds": [
    "111111111111",
    "222222222222"
  ],
  "superUsers": [
    "YOUR_USER_ID"
  ],
  "guildOnly": true,
  "guildIds": [
    "1465119276020011104"
  ]
}
```

---

## 🧠 Stats Collector Configuration

In `src/index.js`:

```js
config._statsCollector = new StatsCollector({
  sampleIntervalMs: 5000,      // sample every 5 seconds
  maxPoints: 120960,           // 1 week of data
  persistEverySamples: 6       // write to disk every 30s
});
config._statsCollector.start();
```

### What these mean:

| Option              | Description                   |
| ------------------- | ----------------------------- |
| sampleIntervalMs    | How often stats are collected |
| maxPoints           | Max rolling data buffer size  |
| persistEverySamples | How often stats.json is saved |

---

## 📈 Graph Engine

Graphs are generated using:

* QuickChart (Chart.js backend)
* No native canvas dependencies
* No local build tools required

### Downsampling Logic

For long ranges:

* Data is capped to 2000 points
* Null gap markers preserved
* Full history remains stored
* Only the chart output is reduced

---

## 🗂 Project Structure

```
tadBot/
│
├── src/
│   ├── commands/
│   │   ├── stats.js
│   │   ├── restart.js
│   │   └── shutdown.js
│   │
│   ├── util/
│   │   ├── statsCollector.js
│   │   └── permissions.js
│   │
│   ├── handlers/
│   │   └── commandRouter.js
│   │
│   └── index.js
│
├── data/
│   └── stats.json
│
├── config.json
├── run_bot.bat
├── package.json
└── README.md
```

---

## ▶️ Running the Bot

1. Install Node.js (LTS)
2. Install dependencies:

```bash
npm install
```

3. Edit `run_bot.bat` and paste your token:

```bat
set "DISCORD_BOT_TOKEN=YOUR_TOKEN"
```

4. Run:

```
run_bot.bat
```

---

## 🔁 Auto-Restart Behavior

The batch file runs the bot inside a supervisor loop:

* Normal crash → auto restart
* Exit code 99 → full shutdown
* Restart command works cleanly

---

## 🛡 Command Permissions

Each command file defines:

```js
name
allowedChannels
requiresSuperUser
```

The router enforces:

* Channel restrictions
* SuperUser restrictions
* No bypass for superUsers unless explicitly coded

---

## 🧩 Customization Ideas

Future enhancements possible:

* Disk usage graphs
* Network throughput graphs
* Temperature graphs
* LTTB smart decimation
* Web dashboard
* Prometheus export
* Historical CSV export
* Alert thresholds

---

## 📌 Notes

* GPU usage depends on driver support
* QuickChart requires internet access
* stats.json may grow to several MB for week-long history (normal)

---

## 📄 License

Private internal project.

