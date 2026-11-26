"""Rebuild the portable Inter Bold wordmark; requires fonttools==4.69.1."""
from pathlib import Path
from fontTools.ttLib import TTFont
from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.transformPen import TransformPen

root = Path(__file__).resolve().parents[1]
font = TTFont(root / "backend/EscalaEasy.Infrastructure/ImportExport/Fonts/Inter-Bold.ttf")
glyphs, cmap = font.getGlyphSet(), font.getBestCmap()
scale = 48 / font["head"].unitsPerEm
x = 78.0
parts = []
for word, color in [("Escala", "#164e63"), ("Easy", "#53b58a")]:
    pen = SVGPathPen(glyphs)
    for char in word:
        name = cmap[ord(char)]
        glyphs[name].draw(TransformPen(pen, (scale, 0, 0, -scale, x, 52)))
        x += glyphs[name].width * scale - 1.5
    parts.append(f'<path fill="{color}" d="{pen.getCommands()}"/>')
logo = root / "frontend/public/brand/logo.svg"
source = logo.read_text(encoding="utf-8")
mark = source[:source.index("</g>") + 4]
logo.write_text(mark + "".join(parts) + "</svg>\n", encoding="utf-8")
