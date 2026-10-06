-- Export the open TDS vessel .aseprite file back to the game's sprite sheets (Aseprite 1.3 or newer).
-- File > Scripts > Open Scripts Folder, copy this file there, then File > Scripts > export_sheets.
--
-- Every tag's user data (right-click the tag > Tag Properties) says where its frames go:
--   sheet=Vessel/Argo-Sheet.png;row=0;col=0;cols=6;rows=8
-- Each frame is drawn flattened (visible layers only) into that cell; cells are the canvas size.
-- Sheets are written to a "sheets" folder next to the .aseprite file, in the same Vessel/UI/Unit folders as
-- Assets/<Name>Mod/Art in the Unity project.

local spr = app.activeSprite
if not spr then return app.alert("Open a vessel .aseprite file first.") end
if spr.colorMode ~= ColorMode.RGB then return app.alert("Set Sprite > Color Mode > RGB Color first.") end
if spr.filename == "" or not app.fs.isFile(spr.filename) then return app.alert("Save the file first.") end

local w, h = spr.width, spr.height
local outDir = app.fs.joinPath(app.fs.filePath(spr.filename), "sheets")
local sheets, order, warnings = {}, {}, {}

for _, tag in ipairs(spr.tags) do
  local d = tag.data or ""
  local sheet = d:match("sheet=([^;]+)")
  if sheet then
    local row = tonumber(d:match("row=(%d+)"))
    local col = tonumber(d:match("col=(%d+)"))
    local cols = tonumber(d:match("cols=(%d+)"))
    local rows = tonumber(d:match("rows=(%d+)"))
    local s = sheets[sheet]
    if not s then
      s = Image(cols * w, rows * h, ColorMode.RGB)
      sheets[sheet] = s
      table.insert(order, sheet)
    end
    local n = tag.frames
    if col + n > cols then
      table.insert(warnings, tag.name .. " has " .. n .. " frames; its row fits " .. (cols - col))
      n = cols - col
    end
    for i = 0, n - 1 do
      s:drawSprite(spr, tag.fromFrame.frameNumber + i, Point((col + i) * w, row * h))
    end
  end
end

if #order == 0 then return app.alert("No tags with sheet=... user data in this file.") end
for _, sheet in ipairs(order) do
  local path = app.fs.joinPath(outDir, sheet)
  app.fs.makeAllDirectories(app.fs.filePath(path))
  sheets[sheet]:saveAs(path)
end

local msg = { "Wrote " .. #order .. " sheet(s) to " .. outDir }
for _, s in ipairs(order) do table.insert(msg, "  " .. s) end
for _, m in ipairs(warnings) do table.insert(msg, "Warning: " .. m) end
app.alert{ title = "TDS sheets", text = msg }
