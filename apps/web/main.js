// Browser host: owns the page, the canvas size, and the frame loop, and forwards them to the engine.
import init, { WebClient, version } from "./pkg/grootscape_web.js";

const canvas = document.getElementById("game");
const status = document.getElementById("status");

function deviceSize(entry) {
  const box = entry?.devicePixelContentBoxSize?.[0];
  if (box) {
    return [box.inlineSize, box.blockSize];
  }
  const ratio = window.devicePixelRatio || 1;
  return [Math.round(canvas.clientWidth * ratio), Math.round(canvas.clientHeight * ratio)];
}

async function start() {
  await init();
  [canvas.width, canvas.height] = deviceSize();
  const client = await WebClient.start(canvas);
  status.textContent = `Grootscape ${version()} · ${client.backend}`;

  new ResizeObserver(([entry]) => {
    const [width, height] = deviceSize(entry);
    if (width > 0 && height > 0 && (width !== canvas.width || height !== canvas.height)) {
      canvas.width = width;
      canvas.height = height;
      client.resize(width, height);
    }
  }).observe(canvas, { box: "device-pixel-content-box" });

  const frame = () => {
    try {
      client.render();
      requestAnimationFrame(frame);
    } catch (error) {
      fail(error);
    }
  };
  requestAnimationFrame(frame);
}

function fail(error) {
  status.textContent = `Grootscape stopped: ${error.message ?? error}`;
  console.error(error);
}

start().catch(fail);
