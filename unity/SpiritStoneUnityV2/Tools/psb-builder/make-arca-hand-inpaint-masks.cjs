const fs = require('fs');
const path = require('path');
const { createCanvas } = require('canvas');

const root = path.resolve(__dirname, '../../Assets/Characters/Arca/Rig/Previews');

function createMask(name, width, height, polygons) {
  const canvas = createCanvas(width, height);
  const context = canvas.getContext('2d');
  context.fillStyle = '#000000';
  context.fillRect(0, 0, width, height);
  context.fillStyle = '#ffffff';
  for (const polygon of polygons) {
    context.beginPath();
    context.moveTo(polygon[0], polygon[1]);
    for (let i = 2; i < polygon.length; i += 2) context.lineTo(polygon[i], polygon[i + 1]);
    context.closePath();
    context.fill();
  }
  fs.writeFileSync(path.join(root, name), canvas.toBuffer('image/png'));
}

// White = replaceable exposed fingers. Black = locked glove, gem and wrist.
createMask('Arca_Hand_R_Fingers_InpaintMask.png', 480, 420, [[
  35, 145, 100, 110, 190, 120, 265, 155, 390, 170,
  430, 230, 405, 350, 310, 370, 205, 350, 105, 325, 35, 265,
]]);

createMask('Arca_Hand_L_Fingers_InpaintMask.png', 460, 420, [[
  0, 145, 70, 115, 160, 125, 245, 150, 355, 165, 445, 210,
  455, 295, 405, 350, 305, 365, 200, 350, 100, 330, 10, 285,
]]);

console.log('Arca hand inpaint masks created.');
