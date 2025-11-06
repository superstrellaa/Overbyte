const path = require("path");
const Joi = require("joi");
const { MAX_ROTATION_DELTA } = require("../config/game");

const schemas = {
  game: Joi.object({
    TICK_RATE_MS: Joi.number().integer().min(1).required(),
    INACTIVITY_TIMEOUT_MS: Joi.number().integer().min(1000).required(),
    MAX_TELEPORT_DISTANCE: Joi.number().integer().min(1).required(),
    MAX_ROTATION_DELTA: Joi.number().integer().min(1).max(180).required(),
    MAX_ROOMS: Joi.number().integer().min(1).required(),
    roomUnlimited: Joi.boolean().default(false),
    MAX_PLAYERS_PER_MATCH: Joi.object()
      .pattern(
        Joi.string().regex(/^[1-4]$/),
        Joi.number().integer().min(2).max(100)
      )
      .required(),
  }),
  rateLimit: Joi.object({
    RATE_LIMIT_WINDOW_MS: Joi.number().integer().min(1).required(),
    RATE_LIMIT_MAX_MSGS: Joi.number().integer().min(1).required(),
  }),
  server: Joi.object({
    SERVER_PING_INTERVAL_MS: Joi.number().integer().min(1000).required(),
    SERVER_PING_TIMEOUT_MS: Joi.number().integer().min(1000).required(),
    MAX_MISSED_PONGS: Joi.number().integer().min(1).max(10).default(3),
  }),
  maps: Joi.array().items(
    Joi.object({
      name: Joi.string().required(),
      spawns: Joi.object()
        .pattern(
          Joi.string().valid("1v1", "2v2", "3v3", "4v4"),
          Joi.array()
            .items(
              Joi.object({
                x: Joi.number().required(),
                y: Joi.number().required(),
                z: Joi.number().required(),
              })
            )
            .min(1)
            .required()
        )
        .required(),
    })
  ),
};

const configCache = {};

function loadConfig(name) {
  if (configCache[name]) return configCache[name];
  let config;
  try {
    config = require(path.join("../config", name + ".js"));
  } catch (e) {
    throw new Error(`[ConfigManager] Config file not found: ${name}`);
  }
  if (schemas[name]) {
    const { error } = schemas[name].validate(config);
    if (error) {
      throw new Error(
        `[ConfigManager] Invalid config for ${name}: ${error.message}`
      );
    }
  }
  configCache[name] = config;
  return config;
}

module.exports = {
  get game() {
    return loadConfig("game");
  },
  get rateLimit() {
    return loadConfig("rateLimit");
  },
  get server() {
    return loadConfig("server");
  },
  get maps() {
    return loadConfig("maps");
  },
};
