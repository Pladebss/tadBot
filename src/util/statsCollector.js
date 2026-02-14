const si = require("systeminformation");
const fs = require("fs");
const path = require("path");

class StatsCollector {
  constructor({
    sampleIntervalMs = 5000,
    maxPoints = 120960, // 7 days @ 5s
    persistEverySamples = 6, // write to disk every N samples (6*5s=30s)
    dataDir = path.join(process.cwd(), "data"),
    dataFileName = "stats.json",
  } = {}) {
    this.sampleIntervalMs = sampleIntervalMs;
    this.maxPoints = maxPoints;
    this.persistEverySamples = persistEverySamples;

    this.dataDir = dataDir;
    this.dataPath = path.join(this.dataDir, dataFileName);

    this.timer = null;
    this.persistCounter = 0;

    this.history = { ts: [], cpu: [], mem: [], gpu: [] };

    this._loadFromDisk();
  }

  start() {
    if (this.timer) return;
    this.timer = setInterval(() => this.sample().catch(() => {}), this.sampleIntervalMs);
    this.sample().catch(() => {});
  }

  stop() {
    if (this.timer) clearInterval(this.timer);
    this.timer = null;
    // best-effort persist
    try {
      this._persistToDisk();
    } catch {}
  }

  _ensureDataDir() {
    if (!fs.existsSync(this.dataDir)) {
      fs.mkdirSync(this.dataDir, { recursive: true });
    }
  }

  _loadFromDisk() {
    try {
      this._ensureDataDir();
      if (!fs.existsSync(this.dataPath)) return;

      const raw = fs.readFileSync(this.dataPath, "utf8");
      const parsed = JSON.parse(raw);

      // basic shape validation
      if (
        parsed &&
        Array.isArray(parsed.ts) &&
        Array.isArray(parsed.cpu) &&
        Array.isArray(parsed.mem) &&
        Array.isArray(parsed.gpu)
      ) {
        this.history = parsed;
        // trim just in case old file is huge
        this._trim();
      }
    } catch (err) {
      // if file is corrupt, start fresh (but don't crash bot)
      this.history = { ts: [], cpu: [], mem: [], gpu: [] };
    }
  }

  _persistToDisk() {
    this._ensureDataDir();

    // atomic write: write temp then rename
    const tmp = `${this.dataPath}.tmp`;
    fs.writeFileSync(tmp, JSON.stringify(this.history));
    fs.renameSync(tmp, this.dataPath);
  }

  _trim() {
    const h = this.history;
    while (h.ts.length > this.maxPoints) {
      h.ts.shift();
      h.cpu.shift();
      h.mem.shift();
      h.gpu.shift();
    }
  }

  _push(ts, cpu, mem, gpu) {
    const h = this.history;
    h.ts.push(ts);
    h.cpu.push(cpu);
    h.mem.push(mem);
    h.gpu.push(gpu);
    this._trim();
  }

  async sample() {
    const [load, mem, graphics] = await Promise.all([
      si.currentLoad(),
      si.mem(),
      si.graphics(),
    ]);

    const ts = Date.now();
    const cpuPct = Math.round(load.currentLoad * 10) / 10;
    const memPct = mem.total ? Math.round((mem.active / mem.total) * 1000) / 10 : 0;

    let gpuPct = 0;
    const c = graphics?.controllers?.[0];
    if (c && typeof c.utilizationGpu === "number") {
      gpuPct = Math.round(c.utilizationGpu * 10) / 10;
    }

    this._push(ts, cpuPct, memPct, gpuPct);

    // persist every N samples (default 30s)
    this.persistCounter++;
    if (this.persistCounter >= this.persistEverySamples) {
      this.persistCounter = 0;
      try {
        this._persistToDisk();
      } catch {}
    }
  }

  getHistory({ minutes = 10 } = {}) {
    const cutoff = Date.now() - minutes * 60_000;
    const h = this.history;

    const idx = h.ts.findIndex((t) => t >= cutoff);
    const start = idx === -1 ? h.ts.length : idx;

    return {
      ts: h.ts.slice(start),
      cpu: h.cpu.slice(start),
      mem: h.mem.slice(start),
      gpu: h.gpu.slice(start),
    };
  }

  async getSnapshot() {
    const [osInfo, time, cpu, mem, load, graphics] = await Promise.all([
      si.osInfo(),
      si.time(),
      si.cpu(),
      si.mem(),
      si.currentLoad(),
      si.graphics(),
    ]);

    const c = graphics?.controllers?.[0];
    const gpuName = c?.model || c?.name || "Unknown GPU";
    const gpuUtil = typeof c?.utilizationGpu === "number" ? c.utilizationGpu : null;

    return {
      hostname: osInfo.hostname,
      platform: `${osInfo.distro} ${osInfo.release}`,
      cpu: `${cpu.manufacturer} ${cpu.brand}`,
      gpu: gpuName,
      uptimeSeconds: time.uptime || 0,
      cpuLoadPct: Math.round(load.currentLoad * 10) / 10,
      memUsedGB: Math.round((mem.active / 1024 ** 3) * 100) / 100,
      memTotalGB: Math.round((mem.total / 1024 ** 3) * 100) / 100,
      memPct: mem.total ? Math.round((mem.active / mem.total) * 1000) / 10 : 0,
      gpuPct: gpuUtil != null ? Math.round(gpuUtil * 10) / 10 : null,
    };
  }
}

module.exports = { StatsCollector };
