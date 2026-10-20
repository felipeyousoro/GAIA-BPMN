const { app, BrowserWindow, session } = require("electron");
const path = require("node:path");

function createWindow() {
  const window = new BrowserWindow({
    width: 1280,
    height: 820,
    webPreferences: { contextIsolation: true, nodeIntegration: false, sandbox: true }
  });
  session.defaultSession.webRequest.onBeforeSendHeaders((details, callback) => callback({ requestHeaders: details.requestHeaders }));
  window.loadFile(path.join(__dirname, "dist", "index.html"));
}

app.whenReady().then(createWindow);
app.on("window-all-closed", () => { if (process.platform !== "darwin") app.quit(); });
