// The page owns layout, animation scheduling, and the canvas lifetime.
import createClient from "./pkg/grootscape_web.js";
const canvas = document.getElementById("game");
const status = document.getElementById("status");
let observer;
let animation;
let stopped = false;
let stopClient;
function fail(error) {
  stopped = true;
  observer?.disconnect();
  cancelAnimationFrame(animation);
  stopClient?.();
  status.textContent = `Grootscape stopped: ${error.message ?? error}`;
  console.error(error);
}
async function start() {
  const module = await createClient({ canvas, onAbort: fail });
  const call = (name, result, args = []) => module.cwrap(`client_${name}`, result, args);
  const open = call("start", "number", ["number", "number"]);
  const resize = call("resize", "number", ["number", "number"]);
  const render = call("render", "number");
  const error = call("error", "string");
  const backend = call("backend", "string");
  const version = call("version", "string");
  stopClient = call("stop", null);
  let started = false;
  function measure() {
    const ratio = window.devicePixelRatio || 1;
    const width = Math.round(canvas.clientWidth * ratio);
    const height = Math.round(canvas.clientHeight * ratio);
    if (width <= 0 || height <= 0) return false;
    if (!started) {
      if (!open(width, height)) throw new Error(error());
      started = true;
      status.textContent = `Grootscape ${version()} · ${backend()} · WebGL 2`;
    } else if (width !== canvas.width || height !== canvas.height) {
      if (!resize(width, height)) throw new Error(error());
    }
    return true;
  }
  observer = new ResizeObserver(() => { try { measure(); } catch (error) { fail(error); } });
  observer.observe(canvas);
  const frame = () => {
    if (stopped) return;
    try {
      if (!document.hidden && measure() && render() < 0) throw new Error(error());
      animation = requestAnimationFrame(frame);
    } catch (error) { fail(error); }
  };
  canvas.addEventListener("webglcontextlost", (event) => {
    event.preventDefault();
    fail(new Error("Graphics context lost. Reload to restart."));
  });
  window.addEventListener("pagehide", (event) => {
    // A cached page resumes its existing animation loop when restored.
    if (event.persisted) return;
    stopped = true;
    observer.disconnect();
    cancelAnimationFrame(animation);
    stopClient();
  }, { once: true });
  animation = requestAnimationFrame(frame);
}
start().catch(fail);
