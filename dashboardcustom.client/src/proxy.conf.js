const { env } = require('process');

const PROXY_CONFIG = [
  {
    context: [
      "/weatherforecast",
      "/api"
    ],
    target : "http://localhost:5206",
    secure: false,
    changeOrigin: true
  }
]

module.exports = PROXY_CONFIG;
