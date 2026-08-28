const fs = require('fs');
const path = require('path');
const { PNG } = require('pngjs');
const { createCanvas, Image } = require('canvas');

const rigRoot = path.resolve(__dirname, '../../Assets/Characters/Arca/Rig');
const sourcePath = path.join(rigRoot, 'Source/Arca_RigMaster_Locked.png');
const outputRoot = path.join(rigRoot, 'Parts/Back');
const source = PNG.sync.read(fs.readFileSync(sourcePath));

function insidePolygon(x, y, polygon) {
  let inside = false;
  const pointCount = polygon.length / 2;
  for (let i = 0, j = pointCount - 1; i < pointCount; j = i++) {
    const xi = polygon[i * 2];
    const yi = polygon[i * 2 + 1];
    const xj = polygon[j * 2];
    const yj = polygon[j * 2 + 1];
    const crosses = ((yi > y) !== (yj > y))
      && (x < ((xj - xi) * (y - yi)) / (yj - yi) + xi);
    if (crosses) inside = !inside;
  }
  return inside;
}

function extract(name, polygons) {
  const output = new PNG({ width: source.width, height: source.height });
  let count = 0;
  for (let y = 0; y < source.height; y++) {
    for (let x = 0; x < source.width; x++) {
      const keep = polygons.some((polygon) => insidePolygon(x + 0.5, y + 0.5, polygon));
      const offset = (y * source.width + x) * 4;
      if (!keep) continue;
      const red = source.data[offset];
      const green = source.data[offset + 1];
      const blue = source.data[offset + 2];
      // Remove only the neutral light checker remnants baked into the draft.
      // Purple/blue highlights are retained because their channels differ.
      if (Math.max(red, green, blue) - Math.min(red, green, blue) < 8
          && Math.min(red, green, blue) > 215) continue;
      // Remove exposed skin caught at the hand/cape boundary. The thresholds are
      // intentionally narrow so gold trim and violet glow remain untouched.
      if (red > 205 && green > 115 && green < 215 && blue > 105 && blue < 205
          && red > green + 25 && green > blue - 35) continue;
      output.data[offset] = source.data[offset];
      output.data[offset + 1] = source.data[offset + 1];
      output.data[offset + 2] = source.data[offset + 2];
      output.data[offset + 3] = source.data[offset + 3];
      if (source.data[offset + 3] > 0) count++;
    }
  }
  const outputPath = path.join(outputRoot, name);
  fs.writeFileSync(outputPath, PNG.sync.write(output));
  console.log(`${name}: ${count} visible pixels`);
}

function extractPurplePart(name, polygons) {
  const output = new PNG({ width: source.width, height: source.height });
  let count = 0;
  for (let y = 0; y < source.height; y++) {
    for (let x = 0; x < source.width; x++) {
      if (!polygons.some((polygon) => insidePolygon(x + 0.5, y + 0.5, polygon))) continue;
      const offset = (y * source.width + x) * 4;
      const red = source.data[offset];
      const green = source.data[offset + 1];
      const blue = source.data[offset + 2];
      const alpha = source.data[offset + 3];
      if (alpha === 0) continue;
      const neutralChecker = Math.max(red, green, blue) - Math.min(red, green, blue) < 8
        && Math.min(red, green, blue) > 215;
      const purpleMaterial = blue > green * 1.08 && red > green * 0.72;
      const darkOutline = Math.max(red, green, blue) < 72;
      if (neutralChecker || (!purpleMaterial && !darkOutline)) continue;
      output.data[offset] = red;
      output.data[offset + 1] = green;
      output.data[offset + 2] = blue;
      output.data[offset + 3] = alpha;
      count++;
    }
  }
  const outputPath = path.join(outputRoot, name);
  fs.writeFileSync(outputPath, PNG.sync.write(output));
  console.log(`${name}: ${count} visible pixels`);
}

function extractVisible(name, directory, polygons) {
  const output = new PNG({ width: source.width, height: source.height });
  let count = 0;
  for (let y = 0; y < source.height; y++) {
    for (let x = 0; x < source.width; x++) {
      if (!polygons.some((polygon) => insidePolygon(x + 0.5, y + 0.5, polygon))) continue;
      const offset = (y * source.width + x) * 4;
      const red = source.data[offset];
      const green = source.data[offset + 1];
      const blue = source.data[offset + 2];
      const alpha = source.data[offset + 3];
      if (alpha === 0) continue;
      const neutralChecker = Math.max(red, green, blue) - Math.min(red, green, blue) < 8
        && Math.min(red, green, blue) > 215;
      if (neutralChecker) continue;
      output.data[offset] = red;
      output.data[offset + 1] = green;
      output.data[offset + 2] = blue;
      output.data[offset + 3] = alpha;
      count++;
    }
  }
  const directoryPath = path.join(rigRoot, 'Parts', directory);
  fs.mkdirSync(directoryPath, { recursive: true });
  fs.writeFileSync(path.join(directoryPath, name), PNG.sync.write(output));
  console.log(`${name}: ${count} visible pixels`);
}

function extractCostumePart(name, directory, polygons) {
  const output = new PNG({ width: source.width, height: source.height });
  let count = 0;
  for (let y = 0; y < source.height; y++) {
    for (let x = 0; x < source.width; x++) {
      if (!polygons.some((polygon) => insidePolygon(x + 0.5, y + 0.5, polygon))) continue;
      const offset = (y * source.width + x) * 4;
      const red = source.data[offset];
      const green = source.data[offset + 1];
      const blue = source.data[offset + 2];
      const alpha = source.data[offset + 3];
      if (alpha === 0) continue;
      const neutralChecker = Math.max(red, green, blue) - Math.min(red, green, blue) < 30
        && Math.min(red, green, blue) > 225;
      const skin = red > 195 && green > 135 && blue > 120
        && red > blue + 14 && red > green + 8;
      const shadedThigh = y > 785 && red > green * 1.18 && blue > green * 0.65
        && red > blue * 1.08;
      if (neutralChecker || skin || shadedThigh) continue;
      output.data[offset] = red;
      output.data[offset + 1] = green;
      output.data[offset + 2] = blue;
      output.data[offset + 3] = alpha;
      count++;
    }
  }
  const directoryPath = path.join(rigRoot, 'Parts', directory);
  fs.mkdirSync(directoryPath, { recursive: true });
  fs.writeFileSync(path.join(directoryPath, name), PNG.sync.write(output));
  console.log(`${name}: ${count} visible pixels`);
}

// Screen-left and screen-right are used deliberately. Character-relative naming
// is deferred until the final hierarchy is approved, preventing L/R ambiguity.
extract('Cape_Back_ScreenLeft_Visible_v2.png', [[
  481, 729, 457, 754, 424, 785, 389, 819, 362, 851, 346, 882,
  344, 917, 349, 945, 358, 969, 371, 919, 398, 875, 431, 844,
  455, 814, 477, 786, 493, 749,
]]);

extract('Cape_Back_ScreenRight_Visible_v2.png', [[
  824, 737, 866, 756, 918, 777, 966, 794, 983, 807, 979, 862,
  972, 922, 957, 969,
  947, 920, 921, 875, 894, 842, 866, 814, 839, 789, 817, 766,
  806, 751,
]]);

// Only the visible outer/rear locks are selected. Bangs crossing the face and
// the gold lightning ornament belong to Front and are intentionally excluded.
extractPurplePart('Hair_Back_Visible_v1.png', [
  [
    510, 183, 455, 203, 421, 238, 400, 282, 387, 329, 389, 383,
    403, 432, 426, 476, 454, 514, 484, 531, 491, 493, 475, 452,
    465, 407, 468, 354, 478, 299, 496, 246,
  ],
  [
    620, 179, 679, 187, 733, 211, 775, 248, 805, 292, 824, 341,
    830, 393, 818, 442, 794, 481, 761, 508, 719, 520, 706, 481,
    715, 440, 723, 397, 720, 350, 705, 303, 680, 258, 650, 217,
  ],
]);

// Torso uses the neck/chest/abdomen as one rigid base. Shoulder armor, arms,
// belt and skirt remain separate so later bone rotation cannot distort them.
extractVisible('Torso_Visible_v1.png', 'Body', [[
  579, 510, 612, 504, 648, 507, 676, 520,
  680, 548, 674, 580, 663, 611, 662, 643, 675, 677,
  663, 700, 631, 711, 593, 704, 577, 682, 584, 650,
  579, 619, 568, 586, 568, 548,
]]);

// Belt and skirt form the pelvis anchor. Bare thighs are removed by the narrow
// skin filter; the polygon avoids both outer cape tails.
extractCostumePart('Pelvis_Skirt_Visible_v1.png', 'Body', [[
  492, 674, 548, 666, 610, 670, 672, 666, 727, 675, 762, 700,
  782, 735, 797, 773, 787, 807, 758, 835, 720, 851, 681, 829,
  631, 812, 584, 832, 535, 851, 493, 830, 466, 801, 461, 765,
  472, 724,
]]);

// Character-relative R/L naming: Arca faces the camera, therefore her right
// arm appears on screen-left. Each upper arm stops before the elbow guard.
extractVisible('UpperArm_R_Visible_v1.png', 'Body', [[
  523, 557, 548, 570, 558, 594, 551, 621, 532, 650,
  507, 678, 484, 674, 475, 653, 486, 628, 500, 601,
]]);

extractVisible('UpperArm_L_Visible_v1.png', 'Body', [[
  681, 558, 707, 568, 725, 588, 735, 614, 748, 640,
  768, 667, 759, 688, 737, 692, 719, 670, 706, 643,
  690, 614, 674, 585,
]]);

extractVisible('Forearm_R_Visible_v1.png', 'Body', [[
  480, 642, 506, 651, 514, 671, 501, 692, 480, 709,
  457, 724, 434, 724, 422, 707, 430, 689, 449, 680,
  465, 661,
]]);

extractVisible('Forearm_L_Visible_v1.png', 'Body', [[
  742, 646, 765, 644, 778, 661, 782, 681, 797, 698,
  814, 710, 819, 727, 806, 741, 787, 738, 772, 720,
  757, 698, 746, 678,
]]);

extractVisible('Hand_R_Visible_v1.png', 'Body', [[
  425, 687, 445, 694, 453, 710, 446, 727, 431, 742,
  412, 754, 392, 756, 373, 748, 363, 733, 369, 717,
  389, 709, 408, 700,
]]);

extractVisible('Hand_L_Visible_v1.png', 'Body', [[
  767, 698, 790, 696, 816, 701, 829, 711, 840, 720, 852, 731,
  851, 746, 840, 758, 823, 765, 805, 762, 789, 753,
  770, 754, 759, 740, 760, 718,
]]);

function addMissingPinkyToRightHand() {
  const handPath = path.join(rigRoot, 'Parts/Body/Hand_R_Visible_v1.png');
  const originalBuffer = fs.readFileSync(handPath);
  const image = new Image();
  image.src = originalBuffer;
  const canvas = createCanvas(source.width, source.height);
  const context = canvas.getContext('2d');

  // Duplicate the existing ring-finger shading into the empty gap, at a
  // slightly smaller size. It is drawn behind the approved hand pixels so the
  // palm/glove contour remains unchanged.
  context.save();
  context.beginPath();
  context.moveTo(416, 716);
  context.bezierCurveTo(425, 721, 438, 733, 440, 743);
  context.bezierCurveTo(441, 751, 436, 758, 430, 758);
  context.bezierCurveTo(422, 756, 417, 744, 411, 731);
  context.closePath();
  context.clip();
  context.drawImage(image, 397, 711, 39, 49, 408, 713, 35, 47);
  context.restore();
  context.drawImage(image, 0, 0);

  fs.writeFileSync(handPath, canvas.toBuffer('image/png'));
  console.log('Hand_R_Visible_v1.png: missing pinky repaired from existing pixels');
}

// Superseded by rebuildHandsAnatomically below.

function restoreLeftThumbBridge() {
  const handPath = path.join(rigRoot, 'Parts/Body/Hand_L_Visible_v1.png');
  const originalBuffer = fs.readFileSync(handPath);
  const image = new Image();
  image.src = originalBuffer;
  const canvas = createCanvas(source.width, source.height);
  const context = canvas.getContext('2d');

  // The source cape covered the base of this thumb. Reconstruct only the short
  // bridge between glove and the preserved thumb tip. It must cover the cape
  // pixels because those are the occluder that caused the missing area.
  context.drawImage(image, 0, 0);
  context.beginPath();
  context.moveTo(789, 718);
  context.bezierCurveTo(795, 726, 790, 738, 782, 748);
  context.bezierCurveTo(777, 756, 769, 760, 764, 754);
  context.bezierCurveTo(760, 749, 765, 741, 772, 733);
  context.bezierCurveTo(778, 725, 782, 719, 789, 718);
  context.closePath();
  const gradient = context.createLinearGradient(765, 755, 791, 720);
  gradient.addColorStop(0, '#eaa995');
  gradient.addColorStop(0.55, '#ffd5bd');
  gradient.addColorStop(1, '#f4b39f');
  context.fillStyle = gradient;
  context.fill();
  context.lineWidth = 3;
  context.strokeStyle = '#45213b';
  context.stroke();

  fs.writeFileSync(handPath, canvas.toBuffer('image/png'));
  console.log('Hand_L_Visible_v1.png: occluded thumb bridge restored');
}

// Superseded by rebuildHandsAnatomically below.

function makeGloveLayer(polygon) {
  const glove = new PNG({ width: source.width, height: source.height });
  for (let y = 0; y < source.height; y++) {
    for (let x = 0; x < source.width; x++) {
      if (!insidePolygon(x + 0.5, y + 0.5, polygon)) continue;
      const offset = (y * source.width + x) * 4;
      const red = source.data[offset];
      const green = source.data[offset + 1];
      const blue = source.data[offset + 2];
      const alpha = source.data[offset + 3];
      const skin = red > 150 && red > green * 1.12 && red > blue * 1.05
        && green > 65 && blue > 55;
      const checker = Math.max(red, green, blue) - Math.min(red, green, blue) < 30
        && Math.min(red, green, blue) > 215;
      if (alpha === 0 || skin || checker) continue;
      source.data.copy(glove.data, offset, offset, offset + 4);
    }
  }
  const image = new Image();
  image.src = PNG.sync.write(glove);
  return image;
}

function drawFinger(context, points, colors = ['#f0ad9d', '#ffd8c3']) {
  context.beginPath();
  context.moveTo(points[0], points[1]);
  context.bezierCurveTo(...points.slice(2, 8));
  context.bezierCurveTo(...points.slice(8, 14));
  context.bezierCurveTo(...points.slice(14, 20));
  context.closePath();
  const gradient = context.createLinearGradient(points[0], points[1], points[10], points[11]);
  gradient.addColorStop(0, colors[0]);
  gradient.addColorStop(0.55, colors[1]);
  gradient.addColorStop(1, '#e99f91');
  context.fillStyle = gradient;
  context.fill();
  context.lineWidth = 2.4;
  context.strokeStyle = '#4a253d';
  context.stroke();
}

function rebuildHand(pathName, glovePolygon, palmPath, fingers) {
  const canvas = createCanvas(source.width, source.height);
  const context = canvas.getContext('2d');
  const glove = makeGloveLayer(glovePolygon);

  context.beginPath();
  context.moveTo(palmPath[0], palmPath[1]);
  for (let index = 2; index < palmPath.length; index += 2) {
    context.lineTo(palmPath[index], palmPath[index + 1]);
  }
  context.closePath();
  const palmGradient = context.createLinearGradient(
    palmPath[0], palmPath[1], palmPath[palmPath.length - 2], palmPath[palmPath.length - 1]);
  palmGradient.addColorStop(0, '#f0aa99');
  palmGradient.addColorStop(0.55, '#ffd7c1');
  palmGradient.addColorStop(1, '#eca391');
  context.fillStyle = palmGradient;
  context.fill();
  context.lineWidth = 2.4;
  context.strokeStyle = '#4a253d';
  context.stroke();

  // Exactly five paths per hand: four fingers followed by one thumb.
  for (const finger of fingers) drawFinger(context, finger);
  context.drawImage(glove, 0, 0);
  fs.writeFileSync(path.join(rigRoot, 'Parts/Body', pathName), canvas.toBuffer('image/png'));
}

rebuildHand(
  'Hand_R_Visible_v1.png',
  [405, 681, 451, 681, 455, 725, 423, 735, 397, 710],
  [405, 704, 436, 704, 444, 723, 425, 735, 400, 724],
  [
    [410, 708, 397, 707, 379, 710, 367, 716, 362, 721, 367, 727, 375, 727, 389, 724, 404, 718, 416, 716],
    [417, 713, 405, 719, 392, 731, 383, 741, 378, 747, 383, 752, 390, 750, 400, 744, 412, 730, 422, 721],
    [425, 716, 416, 727, 408, 741, 404, 750, 402, 757, 408, 760, 414, 756, 421, 747, 428, 732, 433, 722],
    [432, 718, 429, 730, 427, 742, 428, 749, 430, 755, 437, 756, 441, 751, 445, 744, 444, 730, 440, 721],
    [437, 712, 444, 717, 449, 728, 450, 737, 451, 745, 447, 752, 441, 751, 435, 748, 431, 733, 430, 722],
  ],
);

rebuildHand(
  'Hand_L_Visible_v1.png',
  [776, 684, 824, 690, 833, 728, 807, 742, 776, 718],
  [785, 705, 815, 706, 831, 724, 819, 742, 790, 735],
  [
    [806, 713, 819, 713, 837, 719, 847, 726, 853, 731, 850, 738, 843, 738, 830, 735, 817, 728, 805, 722],
    [804, 719, 817, 724, 833, 734, 840, 742, 844, 748, 839, 754, 832, 752, 821, 748, 810, 736, 801, 728],
    [800, 724, 811, 731, 823, 743, 828, 751, 831, 757, 825, 762, 819, 759, 810, 753, 802, 741, 795, 732],
    [794, 726, 802, 736, 809, 748, 811, 755, 812, 761, 806, 765, 800, 761, 794, 754, 789, 741, 788, 732],
    [790, 716, 785, 725, 778, 738, 773, 747, 769, 754, 763, 753, 762, 747, 762, 739, 774, 722, 782, 715],
  ],
);

console.log('Both hands rebuilt with exactly five anatomical digit paths.');
