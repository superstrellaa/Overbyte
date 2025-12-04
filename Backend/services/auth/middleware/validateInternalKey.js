module.exports = function validateInternalApiKey(req, res) {
  const authHeader = req.headers.authorization;

  if (!authHeader || !authHeader.startsWith("Bearer ")) {
    res.status(401).json({ error: "Missing authorization" });
    return false;
  }

  const token = authHeader.split(" ")[1];

  if (token !== process.env.INTERNAL_API_KEY) {
    res.status(403).json({ error: "Invalid internal API key" });
    return false;
  }

  return true;
};
