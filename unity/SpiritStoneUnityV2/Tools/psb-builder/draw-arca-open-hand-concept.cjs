const fs = require('fs');
const path = require('path');
const { createCanvas } = require('canvas');

const width = 1200;
const height = 720;
const canvas = createCanvas(width, height);
const ctx = canvas.getContext('2d');

const background = ctx.createLinearGradient(0, 0, 0, height);
background.addColorStop(0, '#161125');
background.addColorStop(1, '#080610');
ctx.fillStyle = background;
ctx.fillRect(0, 0, width, height);

ctx.strokeStyle = 'rgba(171,112,255,0.13)';
ctx.lineWidth = 1;
for (let x = 40; x < width; x += 40) { ctx.beginPath(); ctx.moveTo(x, 0); ctx.lineTo(x, height); ctx.stroke(); }
for (let y = 40; y < height; y += 40) { ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(width, y); ctx.stroke(); }

ctx.textAlign = 'center';
ctx.fillStyle = '#f1e9ff';
ctx.font = 'bold 34px sans-serif';
ctx.fillText('ARCA · OPEN HAND MANUAL CONCEPT', width / 2, 55);
ctx.fillStyle = '#aa93c9';
ctx.font = '20px sans-serif';
ctx.fillText('엄지 1개 + 손가락 4개 / 기존 검은 장갑 · 보라 보석 · 금색 테두리 유지', width / 2, 88);

function skinGradient(x, y, length) {
  const gradient = ctx.createLinearGradient(x, y, x, y + length);
  gradient.addColorStop(0, '#ffe0cd');
  gradient.addColorStop(0.52, '#f7b8aa');
  gradient.addColorStop(1, '#df8f8e');
  return gradient;
}

function tracePath(points) {
  ctx.beginPath();
  ctx.moveTo(points[0], points[1]);
  for (let i = 2; i < points.length; i += 6) {
    ctx.bezierCurveTo(points[i], points[i + 1], points[i + 2], points[i + 3], points[i + 4], points[i + 5]);
  }
  ctx.closePath();
}

function drawDigit(points, x, y, length) {
  tracePath(points);
  ctx.fillStyle = skinGradient(x, y, length);
  ctx.fill();
  ctx.strokeStyle = '#542a4a';
  ctx.lineWidth = 5;
  ctx.lineJoin = 'round';
  ctx.stroke();
  ctx.strokeStyle = 'rgba(255,241,232,0.7)';
  ctx.lineWidth = 2;
  ctx.stroke();
}

function drawHand(cx, mirror) {
  ctx.save();
  ctx.translate(cx, 160);
  ctx.scale(mirror, 1);

  // Four fingers, deliberately separated at the tips and joined only at palm.
  drawDigit([ -78,190, -104,208,-142,244,-166,278, -178,296,-159,314,-143,298, -119,271,-91,240,-60,225 ], -120,190,125);
  drawDigit([ -38,198, -58,231,-83,280,-94,321, -100,344,-77,356,-64,337, -46,305,-27,255,-10,222 ], -55,200,150);
  drawDigit([ 2,202, -5,242,-10,301,-5,344, -2,367,24,369,29,346, 34,306,31,252,38,216 ], 8,202,165);
  drawDigit([ 42,205, 50,237,67,281,81,313, 90,334,112,324,108,303, 101,270,87,231,72,215 ], 58,205,125);

  // Thumb: one separate digit angled outward from the palm.
  drawDigit([ 78,215, 109,218,147,236,169,258, 184,273,171,294,151,284, 123,272,97,255,71,248 ], 90,215,90);

  // Palm joins the five digits without changing their count.
  tracePath([ -65,190, -28,171,43,174,78,207, 101,231,91,282,64,304, 34,329,-27,332,-63,304, -94,278,-101,224,-65,190 ]);
  ctx.fillStyle = skinGradient(0,180,170);
  ctx.fill();
  ctx.strokeStyle = '#542a4a';
  ctx.lineWidth = 5;
  ctx.stroke();

  // Finger separation creases.
  ctx.strokeStyle = 'rgba(129,63,91,0.65)';
  ctx.lineWidth = 3;
  for (const x of [-52,-12,29,69]) {
    ctx.beginPath(); ctx.moveTo(x,215); ctx.quadraticCurveTo(x+5,235,x+1,251); ctx.stroke();
  }

  // Original Arca-style fingerless glove.
  tracePath([ -105,112, -61,87,45,89,104,126, 116,160,93,221,63,239, 20,215,-58,216,-88,194, -113,172,-123,132,-105,112 ]);
  const glove = ctx.createLinearGradient(0,90,0,240);
  glove.addColorStop(0,'#2e263c'); glove.addColorStop(0.55,'#15131d'); glove.addColorStop(1,'#09080f');
  ctx.fillStyle = glove; ctx.fill();
  ctx.strokeStyle = '#09070e'; ctx.lineWidth = 7; ctx.stroke();

  // Gold cuff and purple lightning gem.
  ctx.beginPath(); ctx.roundRect(-100,92,190,36,16);
  ctx.fillStyle = '#c8973f'; ctx.fill();
  ctx.strokeStyle = '#5f3a19'; ctx.lineWidth = 5; ctx.stroke();
  ctx.beginPath(); ctx.roundRect(-88,99,166,22,10);
  ctx.fillStyle = '#2b2039'; ctx.fill();
  tracePath([ -24,104, -9,87,17,87,34,104, 38,117,18,137,0,136, -18,134,-37,117,-24,104 ]);
  const gem = ctx.createRadialGradient(1,105,2,1,108,34);
  gem.addColorStop(0,'#f7d8ff'); gem.addColorStop(0.35,'#b856ff'); gem.addColorStop(1,'#5a168f');
  ctx.fillStyle = gem; ctx.fill(); ctx.strokeStyle='#e4b252'; ctx.lineWidth=5; ctx.stroke();

  ctx.restore();
}

drawHand(330, 1);
drawHand(870, -1);

ctx.fillStyle = '#d8c5f4';
ctx.font = 'bold 26px sans-serif';
ctx.fillText('아르카 오른손 · Hand_R_Open', 330, 655);
ctx.fillText('아르카 왼손 · Hand_L_Open', 870, 655);

const output = path.resolve(__dirname, '../../Assets/Characters/Arca/Rig/Previews/Arca_OpenHands_ManualConcept_v1.png');
fs.writeFileSync(output, canvas.toBuffer('image/png'));
console.log(output);
