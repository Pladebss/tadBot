
function fillTemplate(template, fieldName) {
  return String(template ?? "").replace("{field}", fieldName);
}

class PresenceManager {
  constructor({ client, config, logger }) {
    this.client = client;
    this.config = config;
    this.logger = logger;
    this.timer = null;
  }

  async setActive(fieldName, lastBoostAtISO) {
    const presenceCfg = this.config.presence ?? {};
    const text = fillTemplate(presenceCfg.activeTemplate ?? "Gathering: {field}", fieldName);

    await this._setPresenceOnline(text);
    this._armTimer(fieldName);
  }

  async setIdle() {
    const idleText = this.config.presence?.idleText ?? "Standing By...";
    await this._setPresenceDnd(idleText);
  }

  async restoreFromState(state) {
    const ttlMinutes = Number(this.config.presence?.ttlMinutes ?? 15);
    const ttlMs = ttlMinutes * 60 * 1000;

    const lastBoostAt = state?.lastBoostAt ? new Date(state.lastBoostAt).getTime() : null;
    const now = Date.now();

    if (state?.lastFieldName && lastBoostAt && (now - lastBoostAt) < ttlMs) {
      const remaining = ttlMs - (now - lastBoostAt);
      this.logger.info(`Restoring active presence for '${state.lastFieldName}' (remaining ${Math.ceil(remaining / 1000)}s)`);
      const text = fillTemplate(this.config.presence?.activeTemplate ?? "Gathering: {field}", state.lastFieldName);
      await this._setPresenceOnline(text);
      this._armTimer(state.lastFieldName, remaining);
    } else {
      this.logger.info("Restoring idle presence");
      await this.setIdle();
    }
  }

  _armTimer(fieldName, overrideMs = null) {
    const ttlMinutes = Number(this.config.presence?.ttlMinutes ?? 15);
    const ttlMs = overrideMs ?? (ttlMinutes * 60 * 1000);

    if (this.timer) clearTimeout(this.timer);
    this.timer = setTimeout(async () => {
      this.logger.info(`TTL expired for '${fieldName}' -> setting idle`);
      try {
        await this.setIdle();
      } catch (e) {
        this.logger.error("Failed setting idle presence", e);
      }
    }, ttlMs);
  }

  async _setPresenceOnline(activityText) {
    if (!this.client.user) return;
    await this.client.user.setPresence({
      status: "online",
      activities: [{ name: activityText }],
    });
  }

  async _setPresenceDnd(activityText) {
    if (!this.client.user) return;
    await this.client.user.setPresence({
      status: "dnd",
      activities: [{ name: activityText }],
    });
  }
}

module.exports = { PresenceManager };
