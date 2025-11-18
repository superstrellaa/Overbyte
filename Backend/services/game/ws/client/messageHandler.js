const handlers = require("./messages");
const schemas = require("./schemas");

function handleClientMessage(room, senderId, msg) {
  const type = msg.type;

  const handler = handlers[type];
  if (!handler) return;

  const schema = schemas[type];
  if (schema) {
    const { error } = schema.validate(msg);
    if (error) {
      return room.sendTo(senderId, {
        type: "error",
        error: "invalid_payload",
        details: error.details[0].message,
      });
    }
  }

  handler(room, senderId, msg);
}

module.exports = { handleClientMessage };
