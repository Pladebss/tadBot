
const { isInAllowedChannel, isSuperUser } = require("../util/permissions");

function startCommandRouter({ client, commandsMap, config, logger }) {
  client.on("interactionCreate", async (interaction) => {
    try {
      if (!interaction.isChatInputCommand()) return;

      const cmd = commandsMap.get(interaction.commandName);
      if (!cmd) {
        return interaction.reply({ content: "Unknown command.", ephemeral: true });
      }

      // Channel restriction always applies
      if (!isInAllowedChannel(interaction, cmd.allowedChannels)) {
        return interaction.reply({ content: "Not allowed in this channel.", ephemeral: true });
      }

      // SuperUser enforcement
      if (cmd.requiresSuperUser) {
        const ok = isSuperUser(interaction.user.id, config.superUsers);
        if (!ok) {
          return interaction.reply({ content: "You are not authorized to run this command.", ephemeral: true });
        }
      }

      await cmd.execute(interaction, { config, logger, client });
    } catch (err) {
      logger.error("Unhandled error in commandRouter", err);
      try {
        if (interaction.deferred || interaction.replied) {
          await interaction.followUp({ content: "Command failed.", ephemeral: true });
        } else {
          await interaction.reply({ content: "Command failed.", ephemeral: true });
        }
      } catch {}
    }
  });
}

module.exports = { startCommandRouter };
