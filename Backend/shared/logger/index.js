const winston = require("winston");
const { consoleTransport, fileTransport } = require("./transports");

const logger = winston.createLogger({
  level: "info",
  transports: [consoleTransport, fileTransport],
});

module.exports = logger;
