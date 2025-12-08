module.exports = function validateInternalApiKey(req, res) {
  const authHeader = req.headers.authorization;

  if (!authHeader || !authHeader.startsWith("Bearer ")) {
    res.json({ code: "21", error: "Missing authorization" });
    return false;
  }

  const token = authHeader.split(" ")[1];

  if (token !== process.env.INTERNAL_API_KEY) {
    res.json({ code: "24", error: "Invalid internal API key" });
    return false;
  }

  return true;
};
