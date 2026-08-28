const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { PNG } = require('pngjs');

const rigRoot = path.resolve(__dirname, '../../Assets/Characters/Arca/Rig');
const input = path.join(rigRoot, 'References/Arca_RigMasterV2_AI_Chroma.png');
const output = path.join(rigRoot, 'Source/Arca_RigMaster_V2_Transparent.png');
const png = PNG.sync.read(fs.readFileSync(input));

if (png.width !== 1254 || png.height !== 1254) {
  throw new Error(`Rig Master must be 1254x1254, got ${png.width}x${png.height}.`);
}

let transparent = 0;
let visible = 0;
for (let i = 0; i < png.data.length; i += 4) {
  const red = png.data[i];
  const green = png.data[i + 1];
  const blue = png.data[i + 2];
  const dominance = green - Math.max(red, blue);

  if (green > 130 && dominance > 35) {
    const alpha = Math.max(0, Math.min(255, 255 - (dominance - 35) * 3.2));
    png.data[i + 3] = alpha;
    if (alpha === 0) {
      png.data[i] = 0;
      png.data[i + 1] = 0;
      png.data[i + 2] = 0;
      transparent++;
      continue;
    }
  }

  // Remove chroma spill from antialiased character edges.
  png.data[i + 1] = Math.min(green, Math.round(Math.max(red, blue) * 1.04));
  if (png.data[i + 3] > 0) visible++;
}

const buffer = PNG.sync.write(png);
fs.writeFileSync(output, buffer);
const hash = crypto.createHash('sha256').update(buffer).digest('hex').toUpperCase();
fs.writeFileSync(path.join(rigRoot, 'Data/Arca_RigMaster_V2.sha256'), `${hash}\n`);
console.log(`${output}\nSIZE=${png.width}x${png.height}\nVISIBLE=${visible}\nTRANSPARENT=${transparent}\nSHA256=${hash}`);
