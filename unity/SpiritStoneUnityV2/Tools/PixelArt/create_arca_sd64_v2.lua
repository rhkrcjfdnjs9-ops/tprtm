local output = "C:/Users/rhkrc/orca/projects/tprtm/unity/SpiritStoneUnityV2/Assets/Characters/Arca/Pixel64/Concepts/Arca_SD64_HandPixel_v1.png"

local sprite = Sprite(64, 64, ColorMode.RGB)
sprite.filename = output
local image = sprite.cels[1].image
image:clear()

local function rgba(hex)
  local r = math.floor(hex / 0x10000) % 0x100
  local g = math.floor(hex / 0x100) % 0x100
  local b = hex % 0x100
  return app.pixelColor.rgba(r, g, b, 255)
end

local P = {
  outline = rgba(0x1B102B), deep = rgba(0x291348), shadow = rgba(0x45206F),
  purple = rgba(0x7038A8), violet = rgba(0x9D56D8), light = rgba(0xD49BFF),
  glow = rgba(0xF1D7FF), black = rgba(0x17131E), cloth = rgba(0x282130),
  gold = rgba(0xD79A38), goldLight = rgba(0xFFD47A), skin = rgba(0xF4B79F),
  skinLight = rgba(0xFFD8C6), blush = rgba(0xE9828B), white = rgba(0xFFF5FF),
  eye = rgba(0xB969F0), eyeDark = rgba(0x56277F), boot = rgba(0x21192B)
}

local function px(x, y, color)
  if x >= 0 and x < 64 and y >= 0 and y < 64 then image:drawPixel(x, y, color) end
end

local function rect(x1, y1, x2, y2, color)
  for y = y1, y2 do for x = x1, x2 do px(x, y, color) end end
end

local function ellipse(cx, cy, rx, ry, color)
  for y = cy - ry, cy + ry do
    for x = cx - rx, cx + rx do
      local dx, dy = (x - cx) / rx, (y - cy) / ry
      if dx * dx + dy * dy <= 1 then px(x, y, color) end
    end
  end
end

local function poly(points, color)
  local minY, maxY = 63, 0
  for _, p in ipairs(points) do minY = math.min(minY, p[2]); maxY = math.max(maxY, p[2]) end
  for y = minY, maxY do
    local nodes = {}
    local j = #points
    for i = 1, #points do
      local a, b = points[i], points[j]
      if (a[2] < y and b[2] >= y) or (b[2] < y and a[2] >= y) then
        table.insert(nodes, math.floor(a[1] + (y - a[2]) / (b[2] - a[2]) * (b[1] - a[1]) + 0.5))
      end
      j = i
    end
    table.sort(nodes)
    for i = 1, #nodes - 1, 2 do for x = nodes[i], nodes[i + 1] do px(x, y, color) end end
  end
end

-- Lightning ahoge: oversized and readable at game scale.
poly({{33,2},{38,2},{35,7},{39,7},{32,14},{34,9},{30,9}}, P.outline)
poly({{34,3},{36,3},{33,8},{36,8},{33,11},{34,8},{32,8}}, P.light)

-- Back hair and side locks.
ellipse(32, 22, 19, 17, P.outline)
ellipse(32, 22, 17, 15, P.purple)
poly({{14,22},{17,31},{13,37},{21,34},{24,26}}, P.outline)
poly({{15,23},{18,31},{15,34},{20,32},{22,25}}, P.shadow)
poly({{49,21},{50,31},{54,35},{47,35},{43,26}}, P.outline)
poly({{48,22},{48,30},{51,33},{47,32},{44,25}}, P.violet)

-- Face, ears and clear expression.
rect(16,21,19,27,P.outline); rect(17,22,19,26,P.skin)
rect(45,21,48,27,P.outline); rect(45,22,47,26,P.skin)
ellipse(32, 24, 13, 11, P.outline)
ellipse(32, 24, 12, 10, P.skin)
rect(22,19,42,27,P.skinLight)

-- Hair cap and chunky bangs.
ellipse(32, 17, 16, 10, P.purple)
poly({{16,17},{21,10},{30,8},{27,20},{23,25},{23,16}}, P.shadow)
poly({{26,9},{35,8},{34,22},{30,26},{29,16}}, P.purple)
poly({{34,8},{44,13},{43,23},{38,27},{39,15}}, P.violet)
rect(22,12,25,14,P.violet); rect(26,10,31,12,P.violet)
rect(37,11,41,13,P.light); rect(40,14,43,16,P.light)

-- Large readable eyes, eyebrows, mouth and blush.
rect(23,20,28,21,P.outline); rect(36,20,41,21,P.outline)
rect(23,22,28,27,P.outline); rect(36,22,41,27,P.outline)
rect(24,22,27,26,P.white); rect(37,22,40,26,P.white)
rect(25,23,27,26,P.eye); rect(37,23,39,26,P.eye)
rect(26,24,27,26,P.eyeDark); rect(37,24,38,26,P.eyeDark)
px(25,22,P.glow); px(39,22,P.glow)
rect(20,28,23,29,P.blush); rect(41,28,44,29,P.blush)
rect(30,29,35,30,P.outline); rect(31,29,34,29,P.blush)

-- Lightning hair clip.
poly({{18,14},{22,11},{23,14},{26,15},{22,18},{20,17}}, P.outline)
poly({{20,14},{22,13},{22,15},{24,15},{21,17},{21,15}}, P.goldLight)

-- Cape behind body.
poly({{21,33},{16,48},{22,46},{25,53},{29,43}}, P.outline)
poly({{22,34},{18,46},{22,44},{25,49},{27,40}}, P.shadow)
poly({{43,33},{49,48},{44,46},{40,52},{37,42}}, P.outline)
poly({{42,34},{47,46},{43,44},{40,49},{38,40}}, P.purple)
rect(18,45,20,46,P.violet); rect(44,44,46,45,P.light)

-- Torso with a simple, readable black-purple-gold costume.
poly({{25,32},{39,32},{43,43},{38,47},{26,47},{21,43}}, P.outline)
poly({{26,33},{38,33},{40,42},{36,45},{28,45},{24,42}}, P.cloth)
rect(27,34,37,36,P.black)
poly({{30,33},{32,31},{34,33},{32,36}}, P.gold)
px(32,33,P.light)
rect(26,39,38,40,P.gold); rect(28,39,36,39,P.goldLight)

-- Arms separated from torso for visible animation silhouette.
poly({{23,34},{18,36},{14,42},{17,45},{22,40},{27,38}}, P.outline)
poly({{22,35},{19,37},{16,42},{17,43},{21,39},{25,37}}, P.skin)
rect(15,40,18,44,P.cloth); px(15,44,P.skinLight); px(17,44,P.skinLight)
poly({{40,34},{45,35},{51,39},{50,43},{44,40},{37,38}}, P.outline)
poly({{41,35},{44,36},{49,39},{49,41},{45,39},{39,37}}, P.skinLight)
rect(47,38,51,42,P.cloth); px(51,40,P.skin); px(51,42,P.skin)
rect(19,37,22,38,P.gold); rect(43,36,46,37,P.goldLight)

-- Layered skirt.
poly({{24,43},{40,43},{45,50},{38,52},{32,50},{26,52},{19,50}}, P.outline)
poly({{24,44},{40,44},{42,49},{37,50},{32,48},{27,50},{22,49}}, P.black)
poly({{22,49},{27,50},{32,48},{37,50},{42,49},{40,52},{35,51},{32,53},{28,51},{23,52}}, P.violet)
px(24,49,P.light); px(31,49,P.light); px(39,49,P.light)

-- Legs and boots, both feet share baseline y=61.
poly({{25,50},{31,50},{30,57},{28,57},{28,61},{21,61},{22,57},{24,56}}, P.outline)
poly({{26,51},{29,51},{28,56},{25,56}}, P.skinLight)
poly({{22,56},{29,56},{28,60},{22,60}}, P.boot)
rect(23,57,28,58,P.purple); px(26,57,P.goldLight)
poly({{34,50},{40,50},{41,56},{43,57},{43,61},{35,61},{35,57}}, P.outline)
poly({{35,51},{39,51},{39,56},{36,56}}, P.skin)
poly({{36,56},{42,56},{42,60},{35,60}}, P.boot)
rect(37,57,41,58,P.purple); px(38,57,P.goldLight)

sprite:saveAs(output)
sprite:close()
