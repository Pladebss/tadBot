
function ts() {
  return new Date().toISOString();
}

function fmt(level, msg) {
  return `[${ts()}] [${level}] ${msg}`;
}

function createLogger({ verbose = false } = {}) {
  return {
    info: (msg) => console.log(fmt("INFO", msg)),
    warn: (msg) => console.warn(fmt("WARN", msg)),
    error: (msg, err) => {
      console.error(fmt("ERROR", msg));
      if (err) console.error(err);
    },
    debug: (msg) => {
      if (verbose) console.log(fmt("DEBUG", msg));
    },
  };
}

module.exports = { createLogger };
