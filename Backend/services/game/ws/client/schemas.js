const Joi = require("joi");

const schemas = {
  shoot: Joi.object({
    type: Joi.string().valid("shoot").required(),

    gun: Joi.string().required(),

    origin: Joi.object({
      x: Joi.number().required(),
      y: Joi.number().required(),
      z: Joi.number().required(),
    }).required(),

    hit: Joi.string().valid("player", "wall", "none").required(),

    hitUuid: Joi.string().when("hit", {
      is: "player",
      then: Joi.required(),
      otherwise: Joi.forbidden(),
    }),

    hitPoint: Joi.object({
      x: Joi.number().required(),
      y: Joi.number().required(),
      z: Joi.number().required(),
    }).when("hit", {
      is: Joi.valid("player", "wall"),
      then: Joi.required(),
      otherwise: Joi.forbidden(),
    }),

    hitNormal: Joi.object({
      x: Joi.number().required(),
      y: Joi.number().required(),
      z: Joi.number().required(),
    }).when("hit", {
      is: Joi.valid("player", "wall"),
      then: Joi.required(),
      otherwise: Joi.forbidden(),
    }),
  }),

  move: Joi.object({
    type: Joi.string().valid("move").required(),
    x: Joi.number().required(),
    y: Joi.number().required(),
    z: Joi.number().required(),
    rotationY: Joi.number().required(),
    vx: Joi.number().required(),
    vy: Joi.number().required(),
    vz: Joi.number().required(),
  }),

  aiming: Joi.object({
    type: Joi.string().valid("aiming").required(),
    pitch: Joi.number().min(-45).max(45).required(),
  }),

  changeGun: Joi.object({
    type: Joi.string().valid("changeGun").required(),
    gun: Joi.string()
      .valid(
        "Nothing",
        "HandGun",
        "Stinger",
        "Claw",
        "HandVulcan",
        "Ironfang",
        "Predator",
        "Executioner",
        "Bombard",
        "Vulcan"
      )
      .required(),
  }),

  leaveQueue: Joi.object({
    type: Joi.string().valid("leaveQueue").required(),
  }),

  ping: Joi.object({
    type: Joi.string().valid("ping").required(),
  }),
};

module.exports = schemas;
