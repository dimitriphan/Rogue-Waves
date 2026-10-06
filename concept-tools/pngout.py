"""Minimal RGBA PNG writer (stdlib only)."""
import struct, zlib

def hex_rgba(c):
    if c is None:
        return (0, 0, 0, 0)
    c = c.lstrip('#')
    return (int(c[0:2], 16), int(c[2:4], 16), int(c[4:6], 16), 255)

def write_png(path, pixels, scale=1):
    """pixels: rows of (r, g, b, a) tuples."""
    h, w = len(pixels), len(pixels[0])
    raw = bytearray()
    for row in pixels:
        line = bytearray()
        for px in row:
            line += bytes(px) * scale
        for _ in range(scale):
            raw += b'\x00' + line
    def chunk(tag, data):
        return struct.pack('>I', len(data)) + tag + data + struct.pack('>I', zlib.crc32(tag + data) & 0xffffffff)
    png = b'\x89PNG\r\n\x1a\n'
    png += chunk(b'IHDR', struct.pack('>IIBBBBB', w * scale, h * scale, 8, 6, 0, 0, 0))
    png += chunk(b'IDAT', zlib.compress(bytes(raw), 9))
    png += chunk(b'IEND', b'')
    with open(path, 'wb') as f:
        f.write(png)
