const axios = require("axios");
const logger = require("@overbyte-backend/shared-logger");

function startNetworkRegistration() {
  const id = process.env.SERVER_ID;
  const host = process.env.GAME_WS_HOST;
  const port = Number(process.env.GAME_WS_PORT);
  const key = process.env.NETWORK_BALANCER_KEY;

  const gatewayHost = process.env.GATEWAY_HOST;
  const gatewayPort = process.env.GATEWAY_PORT;

  if (!id || !host || !port) {
    throw new Error("Missing SERVER_ID, GAME_WS_HOST or GAME_WS_PORT");
  }

  const baseUrl = `http://${gatewayHost}:${gatewayPort}`;

  async function register() {
    try {
      const res = await axios.post(
        `${baseUrl}/network/register`,
        { id, host, port, status: "online" },
        {
          headers: {
            Authorization: `Bearer ${key}`,
            "Content-Type": "application/json",
          },
        }
      );

      if (res.data?.ok) {
        logger.info("Registered in Network Balancer", { id, host, port });
      } else {
        logger.error("Failed to register in Network Balancer", res.data);
      }
    } catch (err) {
      logger.error("Error registering in Network Balancer", {
        error: err.message,
      });
    }
  }

  async function heartbeat() {
    try {
      const players = global.__CURRENT_PLAYERS__ || 0;

      const res = await axios.post(
        `${baseUrl}/network/heartbeat`,
        { id, players, status: "online" },
        {
          headers: {
            Authorization: `Bearer ${key}`,
            "Content-Type": "application/json",
          },
        }
      );

      if (!res.data?.ok) {
        logger.warn("Heartbeat rejected", res.data);
      }
    } catch (err) {
      logger.error("Error sending heartbeat", { error: err.message });
    }
  }

  register();

  setInterval(heartbeat, 15000);
}

module.exports = { startNetworkRegistration };
