
function isInAllowedChannel(interaction, allowedChannels) {
  if (!Array.isArray(allowedChannels) || allowedChannels.length === 0) return true;
  return allowedChannels.includes(interaction.channelId);
}

function isSuperUser(userId, superUsers) {
  return Array.isArray(superUsers) && superUsers.includes(userId);
}

module.exports = { isInAllowedChannel, isSuperUser };
