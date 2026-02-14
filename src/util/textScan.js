
const BOOST_RE = /Boosted:\s*([^\r\n]+)/i;

function extractSegments(message) {
  const segments = [];

  // Plain text
  if (message.content) segments.push(message.content);

  // Embed surfaces
  for (const e of message.embeds ?? []) {
    if (e.title) segments.push(e.title);
    if (e.author?.name) segments.push(e.author.name);
    if (e.description) segments.push(e.description);
    for (const f of e.fields ?? []) {
      if (f.name) segments.push(f.name);
      if (f.value) segments.push(f.value);
    }
  }

  return segments;
}

function findBoostField(segments) {
  for (const s of segments) {
    const m = String(s ?? "").match(BOOST_RE);
    if (m) return m[1].trim();
  }
  return null;
}

module.exports = { extractSegments, findBoostField, BOOST_RE };
