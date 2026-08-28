const fs = require('fs');
const path = require('path');
const { PNG } = require('pngjs');

const rigRoot = path.resolve(__dirname, '../../Assets/Characters/Arca/Rig');
const source = PNG.sync.read(fs.readFileSync(path.join(rigRoot, 'Source/Arca_RigMaster_Locked.png')));
const outDir = path.join(rigRoot, 'Previews');

function crop(name, x0, y0, width, height, scale = 4) {
  const out = new PNG({ width: width * scale, height: height * scale });
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      const sourceOffset = ((y0 + y) * source.width + (x0 + x)) * 4;
      for (let sy = 0; sy < scale; sy++) {
        for (let sx = 0; sx < scale; sx++) {
          const targetOffset = (((y * scale + sy) * out.width) + x * scale + sx) * 4;
          source.data.copy(out.data, targetOffset, sourceOffset, sourceOffset + 4);
        }
      }
    }
  }
  fs.writeFileSync(path.join(outDir, name), PNG.sync.write(out));
}

crop('Arca_Hand_R_Source_4x.png', 350, 675, 120, 105);
crop('Arca_Hand_L_Source_4x.png', 765, 685, 115, 105);
