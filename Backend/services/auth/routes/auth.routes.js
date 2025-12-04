const router = require("express").Router();
const controller = require("../controllers/auth.controller");

router.post("/user", controller.registerOrLogin);
router.post("/refresh", controller.refresh);
router.post("/revalidate", controller.revalidate);

module.exports = router;
