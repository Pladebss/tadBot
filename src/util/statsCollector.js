const si = require("systeminformation");

class StatsCollector {
  constructor({ sampleIntervalMs = 5000, maxPoints = 360 } = {}) {
    this.sampleIntervalMs = sampleIntervalMs;
    this.maxPoints = maxPoints;

    this.timer = null;
    this.history = {
      ts: [],
      cpu: [],
      mem: [],
      gpu: [],
    };
  }

  start() {
    if (this.timer) return;
    this.timer = setInterval(() => this.sample().catch(() => {}), this.sampleIntervalMs);
    // take one immediately
    this.sample().catch(() => {});
  }

  stop() {
    if (this.timer) clearInterval(this.timer);
    this.timer = null;
  }

  _pushPoint(ts, cpu, mem, gpu) {
    const h = this.history;
    h.ts.push(ts);
    h.cpu.push(cpu);
    h.mem.push(mem);
    h.gpu.push(gpu);

    while (h.ts.length > this.maxPoints) {
      h.ts.shift();
      h.cpu.shift();
      h.mem.shift();
      h.gpu.shift();
    }
  }

  async sample() {
    const [load, mem, graphics] = await Promise.all([
      si.currentLoad(),
      si.mem(),
      si.graphics(),
    ]);

    const ts = Date.now();
    const cpuPct = Math.round(load.currentLoad * 10) / 10;
    const memPct = mem.total > 0 ? Math.round((mem.active / mem.total) * 1000) / 10 : 0;

    let gpuPct = 0;
    // systeminformation tries to expose controller utilization; may be missing depending on driver/hardware
    if (graphics && Array.isArray(graphics.controllers) && graphics.controllers.length > 0) {
      const c = graphics.controllers[0];
      // utilizationGpu is present on some systems; fall back to 0
      if (typeof c.utilizationGpu === "number") gpuPct = Math.round(c.utilizationGpu * 10) / 10;
    }

    this._pushPoint(ts, cpuPct, memPct, gpuPct);
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

    const upSec = time.uptime || 0;

    const gpuName =
      graphics?.controllers?.[0]?.model ||
      graphics?.controllers?.[0]?.name ||
      "Unknown";

    const gpuUtil =
      typeof graphics?.controllers?.[0]?.utilizationGpu === "number"
        ? graphics.controllers[0].utilizationGpu
        : null;

    return {
      hostname: osInfo.hostname,
      platform: `${osInfo.distro} ${osInfo.release}`,
      cpu: `${cpu.manufacturer} ${cpu.brand}`,
      gpu: gpuName,
      uptimeSeconds: upSec,
      cpuLoadPct: Math.round(load.currentLoad * 10) / 10,
      memUsedGB: Math.round((mem.active / 1024 ** 3) * 100) / 100,
      memTotalGB: Math.round((mem.total / 1024 ** 3) * 100) / 100,
      memPct: mem.total > 0 ? Math.round((mem.active / mem.total) * 1000) / 10 : 0,
      gpuPct: gpuUtil != null ? Math.round(gpuUtil * 10) / 10 : null,
    };
  }
}

module.exports = { StatsCollector };
