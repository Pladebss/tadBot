const {
  SlashCommandBuilder,
  ActionRowBuilder,
  StringSelectMenuBuilder,
  AttachmentBuilder,
} = require("discord.js");
const https = require("https");

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
  allowedChannels: [], // leave empty to allow anywhere (router should treat empty as "no restriction")
  requiresSuperUser: false,
  data: new SlashCommandBuilder()
    .setName("stats")
    .setDescription("Shows nerdy PC stats + graphs (CPU/RAM/GPU/Uptime)."),

  async execute(interaction, ctx) {
    // ctx should include statsCollector (see note below)
    const statsCollector = ctx?.statsCollector;
    if (!statsCollector) {
      return interaction.reply({ content: "Stats collector not initialized.", ephemeral: false });
    }

    await interaction.deferReply({ ephemeral: false });

    const snapshot = await statsCollector.getSnapshot();

    const menu = new StringSelectMenuBuilder()
      .setCustomId("stats_view")
      .setPlaceholder("Choose a stats view…")
      .addOptions(
        { label: "Overview", value: "overview" },
        { label: "CPU (10m)", value: "cpu_10" },
        { label: "Memory (10m)", value: "mem_10" },
        { label: "GPU (10m)", value: "gpu_10" },
        { label: "All Graphs (10m)", value: "all_10" },
        { label: "CPU+MEM+GPU (30m)", value: "all_30" },
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
      (snapshot.gpuPct != null ? `**GPU Util:** ${snapshot.gpuPct}%\n` : `**GPU Util:** (not available)\n`);

    await interaction.editReply({
      content: overviewText,
      components: [row],
    });

    const msg = await interaction.fetchReply();

    const collector = msg.createMessageComponentCollector({
      time: 5 * 60_000, // 5 min
    });

    collector.on("collect", async (i) => {
      if (i.user.id !== interaction.user.id) {
        return i.reply({ content: "Only the command invoker can use this menu.", ephemeral: true });
      }

      await i.deferUpdate();

      const value = i.values[0];

      const makeLabels = (ts) =>
        ts.map((t) => {
          const d = new Date(t);
          return d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", second: "2-digit" });
        });

      async function renderMetric(minutes, metric) {
        const h = statsCollector.getHistory({ minutes });
        const labels = makeLabels(h.ts);

        const map = {
          cpu: { label: "CPU %", data: h.cpu },
          mem: { label: "Memory %", data: h.mem },
          gpu: { label: "GPU %", data: h.gpu },
        };

        const config = buildChart({
          labels,
          title: `${map[metric].label} (last ${minutes} minutes)`,
          series: [{ label: map[metric].label, data: map[metric].data }],
        });

        const png = await fetchQuickChartPng(config);
        return new AttachmentBuilder(png, { name: `${metric}_${minutes}m.png` });
      }

      async function renderAll(minutes) {
        const h = statsCollector.getHistory({ minutes });
        const labels = makeLabels(h.ts);

        const config = buildChart({
          labels,
          title: `CPU / Memory / GPU (last ${minutes} minutes)`,
          series: [
            { label: "CPU %", data: h.cpu },
            { label: "Memory %", data: h.mem },
            { label: "GPU %", data: h.gpu },
          ],
        });

        const png = await fetchQuickChartPng(config);
        return new AttachmentBuilder(png, { name: `all_${minutes}m.png` });
      }

      try {
        const snap = await statsCollector.getSnapshot();
        const base =
          `**Uptime:** ${fmtUptime(snap.uptimeSeconds)} | **CPU:** ${snap.cpuLoadPct}% | ` +
          `**MEM:** ${snap.memPct}%` +
          (snap.gpuPct != null ? ` | **GPU:** ${snap.gpuPct}%` : "");

        if (value === "overview") {
          await interaction.editReply({ content: overviewText, files: [], components: [row] });
          return;
        }

        if (value === "cpu_10") {
          const att = await renderMetric(10, "cpu");
          await interaction.editReply({ content: base, files: [att], components: [row] });
          return;
        }

        if (value === "mem_10") {
          const att = await renderMetric(10, "mem");
          await interaction.editReply({ content: base, files: [att], components: [row] });
          return;
        }

        if (value === "gpu_10") {
          const att = await renderMetric(10, "gpu");
          await interaction.editReply({ content: base, files: [att], components: [row] });
          return;
        }

        if (value === "all_10") {
          const att = await renderAll(10);
          await interaction.editReply({ content: base, files: [att], components: [row] });
          return;
        }

        if (value === "all_30") {
          const att = await renderAll(30);
          await interaction.editReply({ content: base, files: [att], components: [row] });
          return;
        }
      } catch (e) {
        await interaction.editReply({
          content:
            `Couldn’t generate chart. If this PC has no internet access, charts won’t work.\n` +
            `Error: ${String(e.message || e).slice(0, 250)}`,
          files: [],
          components: [row],
        });
      }
    });

    collector.on("end", async () => {
      // disable the menu after timeout
      const disabledRow = new ActionRowBuilder().addComponents(menu.setDisabled(true));
      try {
        await interaction.editReply({ components: [disabledRow] });
      } catch {}
    });
  },
};
