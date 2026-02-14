
const { SlashCommandBuilder } = require("discord.js");
const { exec } = require("child_process");

module.exports = {
  name: "restart",
  allowedChannels: ["1472263993174523976"],
  requiresSuperUser: true,
  data: new SlashCommandBuilder()
    .setName("restart")
    .setDescription("Restart actions.")
    .addSubcommand((sc) => sc.setName("bot").setDescription("Restart the bot process (launcher relaunches)."))
    .addSubcommand((sc) => sc.setName("pc").setDescription("Restart the PC (Windows reboot)."))
    .addSubcommand((sc) => sc.setName("rdp").setDescription("Close mstsc.exe and run configured RDP restart batch.")),
  async execute(interaction, ctx) {
    const sub = interaction.options.getSubcommand();

    if (sub === "bot") {
      await interaction.reply({ content: "Restarting bot...", ephemeral: true });
      setTimeout(() => process.exit(0), 500);
      return;
    }

    if (sub === "pc") {
      if (!ctx.config.restart?.allowPcRestart) {
        await interaction.reply({ content: "PC restart is disabled in config.", ephemeral: true });
        return;
      }
      await interaction.reply({ content: "Restarting PC now...", ephemeral: true });
      exec("shutdown /r /t 0", (err) => {
        if (err) ctx.logger.error("Failed to execute shutdown", err);
      });
      return;
    }

    if (sub === "rdp") {
      const batchPath = ctx.config.restart?.rdpRestartBatchPath;
      if (!batchPath) {
        await interaction.reply({ content: "restart.rdpRestartBatchPath is missing in config.", ephemeral: true });
        return;
      }
      await interaction.reply({ content: "Restarting RDP flow...", ephemeral: true });
      exec("taskkill /IM mstsc.exe /F", (err) => {
        if (err) ctx.logger.warn("taskkill returned error (mstsc may not be running).");
        exec(`start "" "${batchPath}"`, (err2) => {
          if (err2) ctx.logger.error("Failed to start RDP batch", err2);
        });
      });
      return;
    }

    await interaction.reply({ content: "Unknown subcommand.", ephemeral: true });
  },
};
