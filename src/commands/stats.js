const {
  SlashCommandBuilder,
  ActionRowBuilder,
  StringSelectMenuBuilder,
  AttachmentBuilder,
} = require("discord.js");
const https = require("https");

const MAX_POINTS = 2000;

function fmtUptime(seconds) {
  seconds = Math.max(0, Math.floor(seconds));
  const d = Math.floor(seconds / 86400);
  seconds %= 86400;
  const h = Math.floor(seconds / 3600);
  seconds %= 3600;
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${d}d ${h}h ${m}m ${s}s`;
}

// Accepts: 30s, 10m, 1h, 1d, 1w
// Returns minutes (can be fractional for seconds, e.g. 30s -> 0.5)
function parseRangeToMinutes(input) {
  if (!input) return { minutes: 10, label: "10m" };

  const raw = String(input).trim().toLowerCase();
  const m = raw.match(/^(\d+)(s|m|h|d|w)$/);
  if (!m) return null;

  const value = parseInt(m[1], 10);
  const unit = m[2];

  const mult = { s: 1 / 60, m: 1, h: 60, d: 1440, w: 10080 };
  return { minutes: value * mult[unit], label: raw };
}

function clampRangeMinutes(minutes) {
  const MIN = 0.5;    // 30s
  const MAX = 10080;  // 1w
  if (minutes < MIN) return MIN;
  if (minutes > MAX) return MAX;
  return minutes;
}

// QuickChart image generator (no native deps)
function fetchQuickChartPng(chartConfig) {
  const postData = JSON.stringify({
    backgroundColor: "transparent",
    width: 900,
    height: 360,
    format: "png",
    chart: chartConfig,
  });

  const options = {
    hostname: "quickchart.io",
    path: "/chart",
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "Content-Length": Buffer.byteLength(postData),
      "User-Agent": "tadbot-stats",
    },
  };

  return new Promise((resolve, reject) => {
    const req = https.request(options, (res) => {
      const chunks = [];
      res.on("data", (d) => chunks.push(d));
      res.on("end", () => {
        const buf = Buffer.concat(chunks);
        if (res.statusCode && res.statusCode >= 200 && res.statusCode < 300) resolve(buf);
        else reject(new Error(`QuickChart HTTP ${res.statusCode}: ${buf.toString("utf8").slice(0, 200)}`));
      });
    });
    req.on("error", reject);
    req.write(postData);
    req.end();
  });
}

/**
 * Insert gap breaks by injecting null points whenever there’s a large time jump.
 * Chart.js will not draw a line through nulls when spanGaps=false.
 */
function injectNullGaps(ts, seriesList, gapThresholdMs) {
  if (!ts || ts.length === 0) return { ts: [], seriesList: seriesList.map(() => []) };

  const outTs = [];
  const outSeries = seriesList.map(() => []);

  for (let i = 0; i < ts.length; i++) {
    if (i > 0) {
      const dt = ts[i] - ts[i - 1];
      if (dt > gapThresholdMs) {
        const mid = ts[i - 1] + Math.floor(dt / 2);
        outTs.push(mid);
        for (let s = 0; s < outSeries.length; s++) outSeries[s].push(null);
      }
    }

    outTs.push(ts[i]);
    for (let s = 0; s < outSeries.length; s++) outSeries[s].push(seriesList[s][i]);
  }

  return { ts: outTs, seriesList: outSeries };
}

/**
 * Downsample to at most maxPoints while preserving null gap points.
 * We keep:
 * - all null indices (so gaps remain visible)
 * - evenly spaced non-null indices
 * - first + last
 */
function downsamplePreserveNulls(ts, seriesList, maxPoints) {
  const n = ts.length;
  if (n <= maxPoints) return { ts, seriesList };

  // null indices are "breaks" (we assume all series share same null pattern)
  const nullIdx = new Set();
  for (let i = 0; i < n; i++) {
    if (seriesList[0][i] === null) nullIdx.add(i);
  }

  // if nulls alone exceed maxPoints, keep all nulls + a small amount around them
  // (rare unless you have insane gaps)
  const minKeep = 2; // always keep endpoints
  const availableForNonNull = Math.max(0, maxPoints - nullIdx.size - minKeep);

  const keep = new Set();
  keep.add(0);
  keep.add(n - 1);
  for (const i of nullIdx) keep.add(i);

  // Collect non-null indices
  const nonNull = [];
  for (let i = 0; i < n; i++) if (!nullIdx.has(i)) nonNull.push(i);

  if (availableForNonNull <= 0) {
    // only keep endpoints + nulls
    const idxs = Array.from(keep).sort((a, b) => a - b);
    return pickByIndices(ts, seriesList, idxs);
  }

  // Evenly sample from nonNull to fit available slots
  const step = Math.max(1, Math.floor(nonNull.length / availableForNonNull));
  for (let k = 0; k < nonNull.length; k += step) keep.add(nonNull[k]);

  // Make sure we didn't overshoot (can happen with step rounding)
  let idxs = Array.from(keep).sort((a, b) => a - b);
  if (idxs.length > maxPoints) {
    // trim non-null extras while keeping all nulls + endpoints
    const must = new Set([0, n - 1, ...nullIdx]);
    const filtered = [];
    for (const idx of idxs) {
      if (must.has(idx)) filtered.push(idx);
      else if (filtered.length < maxPoints) filtered.push(idx);
    }
    idxs = filtered.slice(0, maxPoints).sort((a, b) => a - b);
  }

  return pickByIndices(ts, seriesList, idxs);
}

function pickByIndices(ts, seriesList, idxs) {
  const outTs = [];
  const outSeries = seriesList.map(() => []);
  for (const i of idxs) {
    outTs.push(ts[i]);
    for (let s = 0; s < seriesList.length; s++) outSeries[s].push(seriesList[s][i]);
  }
  return { ts: outTs, seriesList: outSeries };
}

/**
 * Smart labels: depends on range size.
 * - weeks: weekday
 * - days: hour
 * - hours: every 5 minutes
 * - minutes: every 5 seconds
 * (Blank labels are used to reduce clutter, not data)
 */
function makeSmartLabels(ts, rangeMinutes) {
  if (!ts || ts.length === 0) return [];

  let mode;
  if (rangeMinutes >= 10080) mode = "week";
  else if (rangeMinutes >= 1440) mode = "day";
  else if (rangeMinutes >= 60) mode = "hour";
  else mode = "minute";

  return ts.map((t) => {
    const d = new Date(t);

    // If this is a mid-gap null point, labels can be blank; safe to label normally too.
    if (mode === "week") {
      return d.toLocaleDateString([], { weekday: "short" });
    }

    if (mode === "day") {
      return d.getHours().toString().padStart(2, "0") + ":00";
    }

    if (mode === "hour") {
      if (d.getMinutes() % 5 !== 0) return "";
      return d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
    }

    // minute mode
    if (d.getSeconds() % 5 !== 0) return "";
    return d.toLocaleTimeString([], { minute: "2-digit", second: "2-digit" });
  });
}

function buildChart({ labels, series, title }) {
  return {
    type: "line",
    data: {
      labels,
      datasets: series.map((s) => ({
        label: s.label,
        data: s.data,
        fill: false,
        tension: 0.25,
        pointRadius: 0,
        borderWidth: 2,
        spanGaps: false, // don't connect across nulls
      })),
    },
    options: {
      plugins: {
        title: { display: true, text: title },
        legend: { display: true },
      },
      scales: {
        y: {
          beginAtZero: true,
          suggestedMax: 100,
          ticks: { callback: (v) => `${v}%` },
        },
      },
    },
  };
}

module.exports = {
  name: "stats",
  allowedChannels: ["1472263993174523976"],
  requiresSuperUser: false,

  data: new SlashCommandBuilder()
    .setName("stats")
    .setDescription("Shows nerdy PC stats + graphs (CPU/RAM/GPU/Uptime).")
    .addStringOption((opt) =>
      opt
        .setName("range")
        .setDescription("Time range: 30s, 10m, 1h, 1d, 1w (default 10m)")
        .setRequired(false),
    ),

  async execute(interaction, ctx) {
    const statsCollector = ctx?.statsCollector;
    if (!statsCollector) {
      return interaction.reply({ content: "Stats collector not initialized." });
    }

    const rangeInput = interaction.options.getString("range");
    const parsed = parseRangeToMinutes(rangeInput);
    if (!parsed) {
      return interaction.reply({
        content: "Invalid range. Use formats like `30s`, `10m`, `1h`, `1d`, `1w`.",
      });
    }

    const rangeMinutes = clampRangeMinutes(parsed.minutes);
    const rangeLabel = parsed.label;

    await interaction.deferReply();

    const snapshot = await statsCollector.getSnapshot();

    const menu = new StringSelectMenuBuilder()
      .setCustomId("stats_view")
      .setPlaceholder("Choose a stats view…")
      .addOptions(
        { label: "Overview", value: "overview" },
        { label: `CPU (${rangeLabel})`, value: "cpu" },
        { label: `Memory (${rangeLabel})`, value: "mem" },
        { label: `GPU (${rangeLabel})`, value: "gpu" },
        { label: `All (${rangeLabel})`, value: "all" },
      );

    const row = new ActionRowBuilder().addComponents(menu);

    const overviewText =
      `**Host:** ${snapshot.hostname}\n` +
      `**OS:** ${snapshot.platform}\n` +
      `**Uptime:** ${fmtUptime(snapshot.uptimeSeconds)}\n` +
      `**CPU:** ${snapshot.cpu}\n` +
      `**GPU:** ${snapshot.gpu}\n` +
      `**CPU Load:** ${snapshot.cpuLoadPct}%\n` +
      `**Memory:** ${snapshot.memUsedGB} / ${snapshot.memTotalGB} GB (${snapshot.memPct}%)\n` +
      (snapshot.gpuPct != null ? `**GPU Util:** ${snapshot.gpuPct}%\n` : `**GPU Util:** (not available)\n`) +
      `**Range:** ${rangeLabel}\n` +
      `**Chart cap:** ${MAX_POINTS} points\n`;

    await interaction.editReply({
      content: overviewText,
      components: [row],
      files: [],
    });

    const msg = await interaction.fetchReply();
    const collector = msg.createMessageComponentCollector({ time: 5 * 60_000 });

    function getHistorySafe(minutes) {
      const h = statsCollector.getHistory({ minutes });
      if (!h.ts || h.ts.length < 2) return null;
      return h;
    }

    const gapThresholdMs = Math.max(12_000, (statsCollector.sampleIntervalMs || 5000) * 2.5);

    async function renderMetric(minutes, metric) {
      const h = getHistorySafe(minutes);
      if (!h) throw new Error("Not enough history yet. Wait a bit and try again.");

      const seriesRaw = metric === "cpu" ? h.cpu : metric === "mem" ? h.mem : h.gpu;

      // 1) inject gap nulls
      const injected = injectNullGaps(h.ts, [seriesRaw], gapThresholdMs);
      // 2) downsample to cap
      const down = downsamplePreserveNulls(injected.ts, injected.seriesList, MAX_POINTS);
      // 3) smart labels
      const labels = makeSmartLabels(down.ts, minutes);

      const label = metric === "cpu" ? "CPU %" : metric === "mem" ? "Memory %" : "GPU %";

      const config = buildChart({
        labels,
        title: `${label} (last ${rangeLabel})`,
        series: [{ label, data: down.seriesList[0] }],
      });

      const png = await fetchQuickChartPng(config);
      return new AttachmentBuilder(png, { name: `${metric}_${rangeLabel}.png` });
    }

    async function renderAll(minutes) {
      const h = getHistorySafe(minutes);
      if (!h) throw new Error("Not enough history yet. Wait a bit and try again.");

      // 1) inject gap nulls
      const injected = injectNullGaps(h.ts, [h.cpu, h.mem, h.gpu], gapThresholdMs);
      // 2) downsample to cap
      const down = downsamplePreserveNulls(injected.ts, injected.seriesList, MAX_POINTS);
      // 3) smart labels
      const labels = makeSmartLabels(down.ts, minutes);

      const config = buildChart({
        labels,
        title: `CPU / Memory / GPU (last ${rangeLabel})`,
        series: [
          { label: "CPU %", data: down.seriesList[0] },
          { label: "Memory %", data: down.seriesList[1] },
          { label: "GPU %", data: down.seriesList[2] },
        ],
      });

      const png = await fetchQuickChartPng(config);
      return new AttachmentBuilder(png, { name: `all_${rangeLabel}.png` });
    }

    async function updateWith(files, snap) {
      const base =
        `**Uptime:** ${fmtUptime(snap.uptimeSeconds)} | **CPU:** ${snap.cpuLoadPct}% | ` +
        `**MEM:** ${snap.memPct}%` +
        (snap.gpuPct != null ? ` | **GPU:** ${snap.gpuPct}%` : "") +
        `\n**Range:** ${rangeLabel} | **Chart cap:** ${MAX_POINTS}`;

      await interaction.editReply({ content: base, files, components: [row] });
    }

    collector.on("collect", async (i) => {
      if (i.user.id !== interaction.user.id) {
        return i.reply({ content: "Only the command invoker can use this menu.", ephemeral: true });
      }

      await i.deferUpdate();
      const value = i.values[0];

      try {
        const snap = await statsCollector.getSnapshot();

        if (value === "overview") {
          await interaction.editReply({ content: overviewText, files: [], components: [row] });
          return;
        }

        if (value === "cpu") {
          const att = await renderMetric(rangeMinutes, "cpu");
          await updateWith([att], snap);
          return;
        }

        if (value === "mem") {
          const att = await renderMetric(rangeMinutes, "mem");
          await updateWith([att], snap);
          return;
        }

        if (value === "gpu") {
          const att = await renderMetric(rangeMinutes, "gpu");
          await updateWith([att], snap);
          return;
        }

        if (value === "all") {
          const att = await renderAll(rangeMinutes);
          await updateWith([att], snap);
          return;
        }
      } catch (e) {
        await interaction.editReply({
          content:
            `Couldn’t generate chart.\n` +
            `- If the bot just started, it may not have enough history yet.\n` +
            `- If this PC has no internet access, QuickChart won’t work.\n\n` +
            `Error: ${String(e.message || e).slice(0, 250)}`,
          files: [],
          components: [row],
        });
      }
    });

    collector.on("end", async () => {
      const disabledRow = new ActionRowBuilder().addComponents(menu.setDisabled(true));
      try {
        await interaction.editReply({ components: [disabledRow] });
      } catch {}
    });
  },
};
