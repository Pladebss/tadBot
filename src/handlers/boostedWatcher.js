
const { extractSegments, findBoostField } = require("../util/textScan");
const { resolveMappedToken } = require("../util/mapping");
const { writeJsonAtomic } = require("../util/jsonStore");

async function sendFollowTo(client, destChannelIds, mappedToken, logger) {
  const payload = `FollowTo ${mappedToken}`;
  for (const id of destChannelIds) {
    try {
      const ch = await client.channels.fetch(id);
      if (!ch || !ch.isTextBased()) {
        logger.warn(`Destination channel ${id} not found or not text-based`);
        continue;
      }
      await ch.send(payload);
      logger.debug(`Sent to ${id}: ${payload}`);
    } catch (err) {
      logger.error(`Failed sending FollowTo to channel ${id}`, err);
    }
  }
}

function startBoostedWatcher({ client, config, statePath, logger, presenceManager }) {
  const monitorId = config.monitorChannelId;
  const destIds = config.boostDestChannelIds ?? [];
  const mapping = config.fieldMapping ?? {};

  client.on("messageCreate", async (message) => {
    try {
      if (!message || message.channelId !== monitorId) return;

      const segments = extractSegments(message);
      const fieldName = findBoostField(segments);
      if (!fieldName) return;

      const mapped = resolveMappedToken(fieldName, mapping);
      if (!mapped) {
        logger.warn(`Boost field '${fieldName}' missing in fieldMapping; no dispatch.`);
        return;
      }

      const nowIso = new Date().toISOString();

      // Persist runtime state
      writeJsonAtomic(statePath, {
        lastFieldName: fieldName,
        lastMappedToken: mapped,
        lastBoostAt: nowIso,
      });

      logger.info(`Boost detected: '${fieldName}' -> FollowTo ${mapped} (dest=${destIds.length})`);

      // Presence update
      await presenceManager.setActive(fieldName, nowIso);

      // Route FollowTo
      await sendFollowTo(client, destIds, mapped, logger);
    } catch (err) {
      logger.error("Unhandled error in boostedWatcher", err);
    }
  });
}

module.exports = { startBoostedWatcher };
