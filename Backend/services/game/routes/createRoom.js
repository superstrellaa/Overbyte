const logger = require("@overbyte-backend/shared-logger");
const { roomManager } = require("../ws/websocket");
const { nanoid } = require("nanoid");

module.exports = function (app) {
  app.post("/create-room", (req, res) => {
    const authHeader = req.headers.authorization;

    if (!authHeader || !authHeader.startsWith("Bearer ")) {
      return res.json({ code: "51", error: "Missing authorization" });
    }

    const token = authHeader.split(" ")[1];
    if (token !== process.env.INTERNAL_API_KEY) {
      return res.json({ code: "54", error: "Invalid internal API key" });
    }

    const roomId = nanoid(10);
    const maxPlayers = req.body.maxPlayers || 10;

    const room = roomManager.createRoom(roomId, { maxPlayers });

    logger.info("Room created", { roomId, maxPlayers });

    const WS_PORT =
      Number(process.env.GAME_WS_PORT) || Number(process.env.PORT) || 3003;
    return res.json({
      ok: true,
      roomId,
      wsHost: process.env.GAME_WS_HOST || "localhost",
      wsPort: WS_PORT,
    });
  });
};
