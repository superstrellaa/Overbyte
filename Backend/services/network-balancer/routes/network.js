const express = require("express");
const router = express.Router();
const verifyInternalKey = require("../middleware/verifyInternalKey");
const {
  registerServer,
  updateHeartbeat,
  assignServer,
} = require("../services/gameServers");
const logger = require("@overbyte-backend/shared-logger");

router.post("/register", verifyInternalKey, (req, res) => {
  const { id, host, port, status } = req.body;

  if (!id || !host || !port)
    return res.json({ code: "41", error: "Missing fields" });

  const ok = registerServer(id, host, port, status);
  if (!ok)
    return res.json({ code: "45", error: "Server ID already registered" });

  logger.info("Game server registered", { id, host, port });

  res.json({ ok: true });
});

router.post("/heartbeat", verifyInternalKey, (req, res) => {
  const { id, players, status } = req.body;

  const ok = updateHeartbeat(id, players, status);
  if (!ok) return res.json({ code: "47", error: "Server not found" });

  res.json({ ok: true });
});

router.post("/assign-server", verifyInternalKey, (req, res) => {
  const server = assignServer();

  if (!server) return res.json({ code: "45", error: "No servers available" });

  logger.info("Assigned game server", { serverId: server.id });

  res.json({
    serverId: server.id,
    host: server.host,
    port: server.port,
  });
});

module.exports = router;
