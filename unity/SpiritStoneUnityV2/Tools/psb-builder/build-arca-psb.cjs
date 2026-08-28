const fs = require('fs');
const path = require('path');
const { PNG } = require('pngjs');
require('ag-psd/initialize-canvas');
const { readPsd, writePsdBuffer } = require('ag-psd');

const rigRoot = path.resolve(__dirname, '../../Assets/Characters/Arca/Rig');

function loadImageData(relativePath) {
  const decoded = PNG.sync.read(fs.readFileSync(path.join(rigRoot, relativePath)));
  return {
    width: decoded.width,
    height: decoded.height,
    data: new Uint8ClampedArray(decoded.data),
  };
}

const master = loadImageData('Source/Arca_RigMaster_Locked.png');
const capeLeft = loadImageData('Parts/Back/Cape_Back_ScreenLeft_Visible_v2.png');
const capeRight = loadImageData('Parts/Back/Cape_Back_ScreenRight_Visible_v2.png');
const hairBack = loadImageData('Parts/Back/Hair_Back_Visible_v1.png');
const torso = loadImageData('Parts/Body/Torso_Visible_v1.png');
const pelvisSkirt = loadImageData('Parts/Body/Pelvis_Skirt_Visible_v1.png');
const upperArmRight = loadImageData('Parts/Body/UpperArm_R_Visible_v1.png');
const upperArmLeft = loadImageData('Parts/Body/UpperArm_L_Visible_v1.png');
const forearmRight = loadImageData('Parts/Body/Forearm_R_Visible_v1.png');
const forearmLeft = loadImageData('Parts/Body/Forearm_L_Visible_v1.png');
const handRight = loadImageData('Parts/Body/Hand_R_Visible_v1.png');
const handLeft = loadImageData('Parts/Body/Hand_L_Visible_v1.png');

function composite(images, width, height) {
  const output = new Uint8ClampedArray(width * height * 4);
  for (const image of images) {
    for (let offset = 0; offset < output.length; offset += 4) {
      const sourceAlpha = image.data[offset + 3] / 255;
      const destinationAlpha = output[offset + 3] / 255;
      const combinedAlpha = sourceAlpha + destinationAlpha * (1 - sourceAlpha);
      if (combinedAlpha <= 0) continue;
      for (let channel = 0; channel < 3; channel++) {
        output[offset + channel] = Math.round(
          (image.data[offset + channel] * sourceAlpha
            + output[offset + channel] * destinationAlpha * (1 - sourceAlpha))
          / combinedAlpha);
      }
      output[offset + 3] = Math.round(combinedAlpha * 255);
    }
  }
  return { width, height, data: output };
}

const compositeImage = composite([
  capeLeft, capeRight, hairBack, torso, pelvisSkirt,
  upperArmRight, upperArmLeft, forearmRight, forearmLeft, handRight, handLeft,
], master.width, master.height);

const document = {
  width: master.width,
  height: master.height,
  imageData: compositeImage,
  children: [
    {
      id: 100,
      name: 'Arca',
      opened: true,
      children: [
        { id: 110, name: 'Front', opened: true, children: [] },
        { id: 120, name: 'Face', opened: true, children: [] },
        {
          id: 130,
          name: 'Body',
          opened: true,
          children: [
            { id: 131, name: 'Torso_Visible_v1', imageData: torso },
            { id: 132, name: 'Pelvis_Skirt_Visible_v1', imageData: pelvisSkirt },
            { id: 133, name: 'UpperArm_R_Visible_v1', imageData: upperArmRight },
            { id: 134, name: 'UpperArm_L_Visible_v1', imageData: upperArmLeft },
            { id: 135, name: 'Forearm_R_Visible_v1', imageData: forearmRight },
            { id: 136, name: 'Forearm_L_Visible_v1', imageData: forearmLeft },
            { id: 137, name: 'Hand_R_Visible_v1', imageData: handRight },
            { id: 138, name: 'Hand_L_Visible_v1', imageData: handLeft },
          ],
        },
        {
          id: 140,
          name: 'Back',
          opened: true,
          children: [
            { id: 141, name: 'Hair_Back_Visible_v1', imageData: hairBack },
            { id: 142, name: 'Cape_Back_ScreenRight_Visible_v2', imageData: capeRight },
            { id: 143, name: 'Cape_Back_ScreenLeft_Visible_v2', imageData: capeLeft },
          ],
        },
        {
          id: 190,
          name: '__REFERENCE_MASTER_LOCKED',
          hidden: true,
          imageData: master,
        },
      ],
    },
  ],
};

const output = path.join(rigRoot, 'Arca_Rig_v1.psb');
fs.writeFileSync(output, writePsdBuffer(document, {
  psb: true,
  noBackground: true,
  compress: true,
}));

const header = fs.readFileSync(output).subarray(0, 6);
if (header.toString('ascii', 0, 4) !== '8BPS' || header.readUInt16BE(4) !== 2) {
  throw new Error('Output is not a valid PSB v2 document.');
}

const decoded = readPsd(fs.readFileSync(output), {
  useImageData: true,
  skipThumbnail: true,
});
function collectDrawableLayers(layers, output = []) {
  for (const layer of layers || []) {
    if (layer.name === '__REFERENCE_MASTER_LOCKED') continue;
    if (layer.imageData) output.push(layer);
    collectDrawableLayers(layer.children, output);
  }
  return output;
}

const alphaCounts = collectDrawableLayers(decoded.children).map((layer) => ({
  name: layer.name,
  visiblePixels: layer.imageData.data.reduce(
    (count, value, index) => count + (index % 4 === 3 && value > 0 ? 1 : 0),
    0),
  rgbTotal: layer.imageData.data.reduce(
    (total, value, index) => total + (index % 4 !== 3 ? value : 0),
    0),
}));
if (alphaCounts.some((layer) => layer.visiblePixels === 0 || layer.rgbTotal === 0)) {
  throw new Error(`PSB contains an empty visible layer: ${JSON.stringify(alphaCounts)}`);
}

console.log(`${output}\nPSB_VERSION=2\nSIZE=${fs.statSync(output).size}\nLAYERS=${JSON.stringify(alphaCounts)}`);
