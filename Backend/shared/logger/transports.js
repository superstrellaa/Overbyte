const winston = require("winston");
const DailyRotateFile = require("winston-daily-rotate-file");
const path = require("path");
const fs = require("fs");

const logDir = path.join(process.cwd(), "logs");

if (!fs.existsSync(logDir)) {
  fs.mkdirSync(logDir, { recursive: true });
}

const jsonFormat = winston.format.combine(
  winston.format.timestamp({ format: "YYYY-MM-DD HH:mm:ss" }),
  winston.format.printf(({ timestamp, level, message, ...meta }) => {
    return JSON.stringify({
      timestamp,
      level,
      message,
      ...meta,
    });
  })
);

const consoleTransport = new winston.transports.Console({
  level: process.env.NODE_ENV === "production" ? "info" : "debug",
  format: jsonFormat,
});

const fileTransport = new DailyRotateFile({
  dirname: logDir,
  filename: "server-%DATE%.log",
  datePattern: "YYYY-MM-DD",
  zippedArchive: false,
  maxFiles: "14d",
  level: "info",
  format: jsonFormat,
});

module.exports = {
  consoleTransport,
  fileTransport,
};
