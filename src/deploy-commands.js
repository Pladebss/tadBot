
const { REST, Routes } = require("discord.js");

async function deployCommands({ token, clientId, commands, config, logger }) {
  const rest = new REST({ version: "10" }).setToken(token);
  const body = commands.map((c) => c.data.toJSON());

  const guildOnly = Boolean(config.slashRegistration?.guildOnly);
  const guildIds = config.slashRegistration?.guildIds ?? [];

  if (guildOnly) {
    if (!Array.isArray(guildIds) || guildIds.length === 0) {
      throw new Error("slashRegistration.guildOnly is true but guildIds is empty.");
    }
    for (const guildId of guildIds) {
      logger.info(`Deploying guild commands to ${guildId} (${body.length} commands)`);
      await rest.put(Routes.applicationGuildCommands(clientId, guildId), { body });
    }
    return;
  }

  logger.info(`Deploying global commands (${body.length} commands)`);
  await rest.put(Routes.applicationCommands(clientId), { body });
}

module.exports = { deployCommands };
