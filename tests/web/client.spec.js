import { test, expect } from "@playwright/test";
import { PNG } from "pngjs";
for (const hidden of [false, true]) {
  test(`WebGL 2 startup, hidden=${hidden}`, async ({ page }) => {
    const errors = [];
    page.on("pageerror", error => errors.push(error.message));
    if (hidden) await page.addInitScript(() => {
      document.addEventListener("DOMContentLoaded", () => { document.getElementById("game").style.display = "none"; });
    });
    await page.goto("http://127.0.0.1:8099");
    if (hidden) {
      await page.waitForTimeout(500);
      await expect(page.locator("#status")).not.toContainText("stopped");
      await page.locator("#game").evaluate(canvas => canvas.style.display = "block");
    }
    await expect(page.locator("#status")).toContainText("WebGL 2");
    await page.setViewportSize({ width: 900, height: 600 });
    await expect.poll(() => page.locator("#game").evaluate(canvas => canvas.width)).toBe(900);
    const pixels = PNG.sync.read(await page.locator("#game").screenshot());
    const color = Array.from(pixels.data.subarray(0, 4));
    expect(color[3]).toBe(255);
    for (const [index, expected] of [13, 28, 20].entries()) {
      expect(Math.abs(color[index] - expected)).toBeLessThanOrEqual(1);
    }
    expect(errors).toEqual([]);
  });
}

test("context loss gives a restart action and reload recovers", async ({ page }) => {
  await page.goto("http://127.0.0.1:8099");
  await expect(page.locator("#status")).toContainText("WebGL 2");
  await page.locator("#game").evaluate(canvas => {
    canvas.getContext("webgl2").getExtension("WEBGL_lose_context").loseContext();
  });
  await expect(page.locator("#status")).toContainText("Reload to restart");
  await page.reload();
  await expect(page.locator("#status")).toContainText("WebGL 2");
});
