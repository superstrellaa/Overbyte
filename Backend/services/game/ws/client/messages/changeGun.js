const logger = require("@overbyte-backend/shared-logger");

const allowedGuns = [
  "Nothing",
  "HandGun",
  "Stinger",
  "Claw",
  "HandVulcan",
  "Ironfang",
  "Predator",
  "Executioner",
  "Bombard",
  "Vulcan",
];

module.exports = (room, senderId, msg) => {
  if (!room || !room.players || !room.players.has(senderId)) {
    room.sendTo(senderId, {
      type: "error",
      error: "Not in room. Join a room to change gun.",
    });

    logger.warn("changeGun denied: player not in room", {
      player: senderId,
      roomId: room?.id,
    });

    return;
  }

  if (!allowedGuns.includes(msg.gun)) {
    room.sendTo(senderId, {
      type: "error",
      error: "Invalid gun type.",
    });

    logger.warn("changeGun denied: invalid gun", {
      player: senderId,
      gun: msg.gun,
      roomId: room.id,
    });

    return;
  }

  room.broadcastExcept(senderId, {
    type: "gunChanged",
    uuid: senderId,
    gun: msg.gun,
  });
};
