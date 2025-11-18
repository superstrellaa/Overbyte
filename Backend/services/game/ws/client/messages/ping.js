module.exports = (room, senderId, msg) => {
  room.sendTo(senderId, {
    type: "pong",
  });
};
