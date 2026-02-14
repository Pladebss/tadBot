const { SlashCommandBuilder } = require("discord.js");

const EXIT_CODE_SHUTDOWN = 99;

module.exports = {
  name: "shutdown",
  allowedChannels: ["1472263993174523976"],
  requiresSuperUser: true,
  data: new SlashCommandBuilder()
    .setName("shutdown")
    .setDescription("Shut down the bot (does not auto-restart)."),
  async execute(interaction) {
    await interaction.reply({ content: "Shutting down bot...", ephemeral: false });

    // Let Discord receive the reply before quitting
    setTimeout(() => process.exit(EXIT_CODE_SHUTDOWN), 750);
  },
};
