
const path = require("path");
const fs = require("fs");
const { Client, GatewayIntentBits, Partials, Collection } = require("discord.js");

const { readJson, writeJsonAtomic } = require("./util/jsonStore");
const { createLogger } = require("./util/logger");
const { PresenceManager } = require("./util/presence");
const { deployCommands } = require("./deploy-commands");
const { startBoostedWatcher } = require("./handlers/boostedWatcher");
const { startCommandRouter } = require("./handlers/commandRouter");
const { StatsCollector } = require("./util/statsCollector");

const ROOT = path.resolve(__dirname, "..");
const CONFIG_PATH = path.join(ROOT, "config.json");
const STATE_PATH = path.join(ROOT, "data", "state.json");

function loadConfig() {
  if (!fs.existsSync(CONFIG_PATH)) {
    throw new Error(`config.json not found at ${CONFIG_PATH}`);
  }
  return JSON.parse(fs.readFileSync(CONFIG_PATH, "utf8"));
}

function validateConfig(cfg) {
  const required = ["monitorChannelId", "boostDestChannelIds", "fieldMapping", "presence", "slashRegistration", "superUsers"];
  for (const k of required) {
    if (cfg[k] === undefined) throw new Error(`Missing config field: ${k}`);
  }

  if (!cfg.monitorChannelId || String(cfg.monitorChannelId).includes("PUT_")) throw new Error("monitorChannelId not configured.");
  if (!Array.isArray(cfg.boostDestChannelIds) || cfg.boostDestChannelIds.length === 0) throw new Error("boostDestChannelIds must be a non-empty array.");
  if (typeof cfg.fieldMapping !== "object" || cfg.fieldMapping === null) throw new Error("fieldMapping must be an object.");
  if (!cfg.presence || typeof cfg.presence.ttlMinutes !== "number") throw new Error("presence.ttlMinutes must be a number.");
  if (!cfg.slashRegistration || typeof cfg.slashRegistration.guildOnly !== "boolean") throw new Error("slashRegistration.guildOnly must be boolean.");
  if (cfg.slashRegistration.guildOnly) {
    if (!Array.isArray(cfg.slashRegistration.guildIds) || cfg.slashRegistration.guildIds.length === 0) {
      throw new Error("slashRegistration.guildOnly is true but guildIds is empty.");
    }
  }
}

function loadCommands(logger) {
  const commandsPath = path.join(__dirname, "commands");
  const files = fs.readdirSync(commandsPath).filter((f) => f.endsWith(".js"));

  const commands = [];
  const map = new Collection();

  for (const file of files) {
    const mod = require(path.join(commandsPath, file));
    if (!mod?.name || !mod?.data || typeof mod.execute !== "function") {
      throw new Error(`Invalid command module: ${file}`);
    }
    commands.push(mod);
    map.set(mod.name, mod);
  }

  logger.info(`Loaded ${commands.length} commands.`);
  return { commands, commandsMap: map };
}

async function main() {
  const token = process.env.DISCORD_BOT_TOKEN;
  if (!token) throw new Error("DISCORD_BOT_TOKEN env var is missing (set it in run_bot.bat).");

  const config = loadConfig();
  validateConfig(config);

  const logger = createLogger({ verbose: Boolean(config.logging?.verbose) });
  logger.info("Starting Discord Booster Bot V3.1");

  const state = readJson(STATE_PATH, { lastFieldName: null, lastMappedToken: null, lastBoostAt: null });
  if (!fs.existsSync(STATE_PATH)) writeJsonAtomic(STATE_PATH, state);

  const client = new Client({
    intents: [
      GatewayIntentBits.Guilds,
      GatewayIntentBits.GuildMessages,
      GatewayIntentBits.MessageContent,
    ],
    partials: [Partials.Channel],
  });

  config._statsCollector = new StatsCollector({
    sampleIntervalMs: 5000,
    maxPoints: 720,
  });
  
  config._statsCollector.start();
  logger.info("Stats collector initialized.");

  const presenceManager = new PresenceManager({ client, config, logger });
  const { commands, commandsMap } = loadCommands(logger);

  client.once("clientReady", async () => {
    try {
      logger.info(`Logged in as ${client.user.tag}`);

      // Deploy slash commands after login
      const clientId = client.application?.id ?? client.user.id;
      await deployCommands({ token, clientId, commands, config, logger });

      // Start routers/watchers
      startCommandRouter({ client, commandsMap, config, logger });
      startBoostedWatcher({ client, config, statePath: STATE_PATH, logger, presenceManager });

      // Restore presence from persisted state
      await presenceManager.restoreFromState(state);

      logger.info("Bot is fully online.");
    } catch (e) {
      logger.error("Fatal error during ready()", e);
      process.exit(1);
    }
  });

  client.on("error", (e) => logger.error("Client error", e));
  client.on("warn", (m) => logger.warn(m));

  await client.login(token);
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
