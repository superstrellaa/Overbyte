const app = require("./app");
const logger = require("@overbyte-backend/shared-logger");

const PORT = process.env.PORT || 3006;

app.listen(PORT, () => {
  logger.info("Network Balancer Service listening", { port: PORT });
});
