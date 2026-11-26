import { defineConfig } from "@playwright/test";
export default defineConfig({
  testDir: ".", timeout: 30000, use: { headless: true, launchOptions: { executablePath: process.env.GROOTSCAPE_BROWSER, args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader"] } },
  webServer: { command: "python3 -m http.server 8099 --bind 127.0.0.1 --directory ../../apps/web", url: "http://127.0.0.1:8099" },
});
