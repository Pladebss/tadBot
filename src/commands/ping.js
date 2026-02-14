
const { SlashCommandBuilder } = require("discord.js");

module.exports = {
  name: "ping",
  allowedChannels: ["1472263993174523976"],
  requiresSuperUser: false,
  data: new SlashCommandBuilder()
    .setName("ping")
    .setDescription("Health check."),
  async execute(interaction) {
    await interaction.reply({ content: "Pong!", ephemeral: false });
  },
};
