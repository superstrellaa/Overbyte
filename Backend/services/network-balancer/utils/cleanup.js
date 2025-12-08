const { cleanupDeadServers } = require("../services/gameServers");

module.exports = function cleanup() {
  setInterval(() => {
    cleanupDeadServers();
  }, 20000);
};
