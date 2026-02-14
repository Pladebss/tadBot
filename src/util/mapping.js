
function normalizeKey(s) {
  return String(s ?? "").trim().replace(/\s+/g, " ");
}

function resolveMappedToken(fieldName, fieldMapping) {
  if (!fieldName) return null;

  if (Object.prototype.hasOwnProperty.call(fieldMapping, fieldName)) {
    return fieldMapping[fieldName];
  }

  const norm = normalizeKey(fieldName);
  if (Object.prototype.hasOwnProperty.call(fieldMapping, norm)) {
    return fieldMapping[norm];
  }

  return null;
}

module.exports = { resolveMappedToken, normalizeKey };
