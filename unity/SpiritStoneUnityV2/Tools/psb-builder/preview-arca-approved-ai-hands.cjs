const fs = require('fs');
const path = require('path');
const { PNG } = require('pngjs');
const { createCanvas, Image } = require('canvas');

const rigRoot = path.resolve(__dirname, '../../Assets/Characters/Arca/Rig');
const masterPath = path.join(rigRoot, 'Source/Arca_RigMaster_Locked.png');
const rightPath = path.join(rigRoot, 'References/Arca_Hand_R_Accepted_3PlusThumb_v1.png');
const leftPath = path.join(rigRoot, 'References/Arca_Hand_L_ThumbDown_4PlusThumb_v3.png');

function chromaKey(filePath) {
  const png = PNG.sync.read(fs.readFileSync(filePath));
  for (let i = 0; i < png.data.length; i += 4) {
    let r = png.data[i], g = png.data[i + 1], b = png.data[i + 2];
    const greenStrength = g - Math.max(r, b);
    if (g > 145 && greenStrength > 45) {
      const alpha = Math.max(0, Math.min(255, 255 - (greenStrength - 45) * 3));
      png.data[i + 3] = alpha;
      if (alpha === 0) continue;
    }
    // Suppress remaining green spill on antialiased edges.
    png.data[i + 1] = Math.min(g, Math.round(Math.max(r, b) * 1.05));
  }
  const image = new Image();
  image.src = PNG.sync.write(png);
  return image;
}

function clearPolygon(context, points) {
  context.save();
  context.globalCompositeOperation = 'destination-out';
  context.beginPath();
  context.moveTo(points[0], points[1]);
  for (let i = 2; i < points.length; i += 2) context.lineTo(points[i], points[i + 1]);
  context.closePath();
  context.fill();
  context.restore();
}

const master = new Image();
master.src = fs.readFileSync(masterPath);
const canvas = createCanvas(1254, 1254);
const context = canvas.getContext('2d');
context.drawImage(master, 0, 0);

// Remove only the two old hand silhouettes; forearms remain untouched.
clearPolygon(context, [360,700,397,681,448,685,461,714,452,751,417,767,378,758,358,735]);
clearPolygon(context, [770,696,807,686,844,704,861,735,850,764,813,775,778,760,761,730]);

// Generated files correspond to the original 4x inspection crops. Mapping the
// complete image back to those exact crop rectangles guarantees the same Unity
// canvas size and wrist location without guessing a new character scale.
context.drawImage(chromaKey(rightPath), 350, 675, 120, 105);
context.drawImage(chromaKey(leftPath), 765, 685, 115, 105);

const output = path.join(rigRoot, 'Previews/Arca_Hands_UnityPreview_v1.png');
fs.writeFileSync(output, canvas.toBuffer('image/png'));
console.log(output);
