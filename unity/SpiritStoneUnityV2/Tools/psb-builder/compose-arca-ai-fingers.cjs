const fs = require('fs');
const path = require('path');
const { PNG } = require('pngjs');
const { createCanvas, Image } = require('canvas');

const rigRoot = path.resolve(__dirname, '../../Assets/Characters/Arca/Rig');
const master = PNG.sync.read(fs.readFileSync(path.join(rigRoot, 'Source/Arca_RigMaster_Locked.png')));
const ai = PNG.sync.read(fs.readFileSync(path.join(rigRoot, 'References/Arca_Hands_AI_Source_v1.png')));

function inside(x, y, flat) {
  let result = false;
  const n = flat.length / 2;
  for (let i = 0, j = n - 1; i < n; j = i++) {
    const xi = flat[i * 2], yi = flat[i * 2 + 1];
    const xj = flat[j * 2], yj = flat[j * 2 + 1];
    if (((yi > y) !== (yj > y)) && x < ((xj - xi) * (y - yi)) / (yj - yi) + xi) result = !result;
  }
  return result;
}

function pngImage(png) {
  const image = new Image();
  image.src = PNG.sync.write(png);
  return image;
}

function extractAiSkin(x0, x1) {
  const out = new PNG({ width: ai.width, height: ai.height });
  const seeds = new Uint8Array(ai.width * ai.height);
  for (let y = 0; y < ai.height; y++) for (let x = x0; x < x1; x++) {
    const i = (y * ai.width + x) * 4;
    const r = ai.data[i], g = ai.data[i + 1], b = ai.data[i + 2];
    const skin = r > 145 && g > 75 && b > 70 && r > g + 8 && r > b + 5;
    if (skin) seeds[y * ai.width + x] = 1;
  }
  const radius = 9;
  for (let y = 0; y < ai.height; y++) for (let x = x0; x < x1; x++) {
    let nearSkin = false;
    for (let dy = -radius; dy <= radius && !nearSkin; dy++) {
      const yy = y + dy;
      if (yy < 0 || yy >= ai.height) continue;
      for (let dx = -radius; dx <= radius; dx++) {
        const xx = x + dx;
        if (xx < x0 || xx >= x1 || dx * dx + dy * dy > radius * radius) continue;
        if (seeds[yy * ai.width + xx]) { nearSkin = true; break; }
      }
    }
    if (!nearSkin) continue;
    const i = (y * ai.width + x) * 4;
    const r = ai.data[i], g = ai.data[i + 1], b = ai.data[i + 2];
    const checker = Math.max(r, g, b) - Math.min(r, g, b) < 24 && Math.min(r, g, b) > 218;
    if (checker) continue;
    out.data[i] = r; out.data[i + 1] = g; out.data[i + 2] = b; out.data[i + 3] = 255;
  }
  return out;
}

function extractOriginalGlove(polygon) {
  const out = new PNG({ width: master.width, height: master.height });
  for (let y = 0; y < master.height; y++) for (let x = 0; x < master.width; x++) {
    if (!inside(x + .5, y + .5, polygon)) continue;
    const i = (y * master.width + x) * 4;
    const r = master.data[i], g = master.data[i + 1], b = master.data[i + 2];
    const skin = r > 155 && r > g * 1.12 && r > b * 1.05 && g > 65 && b > 55;
    const checker = Math.max(r, g, b) - Math.min(r, g, b) < 30 && Math.min(r, g, b) > 215;
    if (skin || checker || master.data[i + 3] === 0) continue;
    master.data.copy(out.data, i, i, i + 4);
  }
  return out;
}

function bounds(png) {
  let minX = png.width, minY = png.height, maxX = -1, maxY = -1;
  for (let y = 0; y < png.height; y++) for (let x = 0; x < png.width; x++) {
    if (png.data[(y * png.width + x) * 4 + 3] === 0) continue;
    minX = Math.min(minX, x); minY = Math.min(minY, y); maxX = Math.max(maxX, x); maxY = Math.max(maxY, y);
  }
  return { x: minX, y: minY, w: maxX - minX + 1, h: maxY - minY + 1 };
}

function rotatedCrop(png, angle) {
  const b = bounds(png);
  const sourceImage = pngImage(png);
  const temp = createCanvas(1200, 1200);
  const ctx = temp.getContext('2d');
  ctx.translate(600, 600);
  ctx.rotate(angle);
  ctx.drawImage(sourceImage, b.x, b.y, b.w, b.h, -b.w / 2, -b.h / 2, b.w, b.h);
  const rendered = PNG.sync.read(temp.toBuffer('image/png'));
  return { image: pngImage(rendered), bounds: bounds(rendered) };
}

function compose(outputName, aiSkin, angle, target, glovePolygon) {
  const canvas = createCanvas(master.width, master.height);
  const ctx = canvas.getContext('2d');
  const rotated = rotatedCrop(aiSkin, angle);
  const scale = Math.min(target.w / rotated.bounds.w, target.h / rotated.bounds.h);
  const width = Math.round(rotated.bounds.w * scale);
  const height = Math.round(rotated.bounds.h * scale);
  const x = Math.round(target.x + (target.w - width) / 2);
  const y = Math.round(target.y + (target.h - height) / 2);
  ctx.drawImage(rotated.image, rotated.bounds.x, rotated.bounds.y, rotated.bounds.w, rotated.bounds.h, x, y, width, height);
  ctx.drawImage(pngImage(extractOriginalGlove(glovePolygon)), 0, 0);
  const output = path.join(rigRoot, 'Parts/Body', outputName);
  fs.writeFileSync(output, canvas.toBuffer('image/png'));
  console.log(`${outputName}: target=${JSON.stringify(target)}, rendered=${width}x${height}@${x},${y}`);
}

compose(
  'Hand_R_AI_Fingers_v1.png', extractAiSkin(0, ai.width / 2), Math.PI / 4,
  { x: 363, y: 687, w: 91, h: 73 }, [397, 677, 455, 681, 456, 728, 420, 737, 394, 708]);
compose(
  'Hand_L_AI_Fingers_v1.png', extractAiSkin(ai.width / 2, ai.width), -Math.PI / 4,
  { x: 759, y: 690, w: 95, h: 75 }, [775, 684, 829, 689, 835, 733, 804, 744, 774, 718]);
