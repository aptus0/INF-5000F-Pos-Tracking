#!/usr/bin/env python3

from __future__ import annotations

from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parent.parent
RESOURCES = ROOT / "Resources"
MEDIA = ROOT / "docs" / "media"
MEDIA.mkdir(parents=True, exist_ok=True)


def font(size: int, bold: bool = False):
    candidates = []
    if bold:
        candidates.extend([
            "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
            "/System/Library/Fonts/Supplemental/Helvetica.ttc",
        ])
    else:
        candidates.extend([
            "/System/Library/Fonts/Supplemental/Arial.ttf",
            "/System/Library/Fonts/Supplemental/Helvetica.ttc",
        ])
    for candidate in candidates:
        path = Path(candidate)
        if path.exists():
            return ImageFont.truetype(str(path), size=size)
    return ImageFont.load_default()


def make_pos_device():
    width, height = 1200, 900
    img = Image.new("RGBA", (width, height), (19, 25, 32, 255))
    draw = ImageDraw.Draw(img)

    orange = (247, 136, 44, 255)
    orange_dark = (191, 95, 22, 255)
    panel = (240, 246, 250, 255)
    blue = (44, 88, 133, 255)
    shadow = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow)
    shadow_draw.rounded_rectangle((280, 150, 920, 760), radius=56, fill=(0, 0, 0, 180))
    shadow = shadow.filter(ImageFilter.GaussianBlur(36))
    img.alpha_composite(shadow, (0, 22))

    draw.rounded_rectangle((290, 130, 910, 740), radius=56, fill=(39, 48, 60, 255), outline=orange, width=10)
    draw.rounded_rectangle((360, 190, 840, 470), radius=28, fill=panel)
    draw.rounded_rectangle((390, 220, 810, 440), radius=20, fill=blue)
    draw.text((455, 255), "SAMER POS", fill="white", font=font(54, bold=True))
    draw.text((485, 330), "Masa • Kasa • Odeme", fill=(225, 233, 242, 255), font=font(30))

    keys_top = 525
    for row in range(3):
        for col in range(3):
            x1 = 400 + col * 118
            y1 = keys_top + row * 64
            x2 = x1 + 92
            y2 = y1 + 44
            draw.rounded_rectangle((x1, y1, x2, y2), radius=10, fill=(230, 234, 239, 255))

    draw.rounded_rectangle((700, 525, 812, 697), radius=16, fill=orange)
    draw.text((728, 589), "OK", fill="white", font=font(34, bold=True))
    draw.rounded_rectangle((300, 760, 890, 812), radius=24, fill=orange_dark)
    draw.text((412, 770), "Ingenico / YNOKC temsil gorseli", fill="white", font=font(28, bold=True))

    img.save(MEDIA / "pos-terminal.png")


def fit_banner():
    src = RESOURCES / "samerhub-banner-reference.png"
    if not src.exists():
        return None
    image = Image.open(src).convert("RGBA")
    image = image.resize((1600, 394))
    image.save(MEDIA / "samerhub-banner.png")
    return image


def make_slide(title: str, subtitle: str, footer: str, accent: str, include_banner: bool = True):
    width, height = 1280, 720
    img = Image.new("RGBA", (width, height), (17, 24, 32, 255))
    draw = ImageDraw.Draw(img)
    accent_rgb = {
        "orange": (247, 136, 44, 255),
        "blue": (56, 103, 153, 255),
        "white": (244, 246, 248, 255),
    }[accent]

    draw.rounded_rectangle((48, 48, width - 48, height - 48), radius=36, fill=(27, 35, 46, 255), outline=(57, 69, 82, 255), width=2)
    logo = RESOURCES / "logo.png"
    if logo.exists():
        logo_img = Image.open(logo).convert("RGBA").resize((132, 132))
        img.alpha_composite(logo_img, (84, 82))

    if include_banner:
        banner = MEDIA / "samerhub-banner.png"
        if banner.exists():
            banner_img = Image.open(banner).convert("RGBA").resize((520, 128))
            img.alpha_composite(banner_img, (686, 86))

    draw.text((84, 250), title, font=font(70, bold=True), fill=(248, 249, 251, 255))
    draw.text((88, 350), subtitle, font=font(34), fill=(205, 214, 223, 255))
    draw.rounded_rectangle((84, 510, 1196, 586), radius=22, fill=accent_rgb)
    draw.text((116, 527), footer, font=font(30, bold=True), fill=(18, 23, 29, 255))
    return img


def make_install_gif():
    fit_banner()
    make_pos_device()

    frames = [
        make_slide(
            "SAMER Hub Windows Kurulumu",
            "Public repo + indirilebilir kurulum paketi + self-contained desktop/service yapisi.",
            "1. GitHub repo ac | 2. Windows package indir | 3. Setup.exe calistir",
            "orange",
        ),
        make_slide(
            "Masaustu ve Servis Hazir",
            "Kurulum servis kaydini yapar, SQLite veritabanini hazirlar ve desktop kisayolunu olusturur.",
            "Kurulumdan sonra SAMER Hub kisayolundan uygulamayi ac",
            "blue",
        ),
        make_slide(
            "POS Entegrasyonu Hazir",
            "Ingenico / YNOKC temsil gorselleri ve ayar ekranlari ile terminal baglantisi yapilabilir.",
            "POS ayarlari icin desktop uygulamasi icindeki Ayarlar sekmesini kullan",
            "white",
            include_banner=False,
        ),
    ]

    pos_image = Image.open(MEDIA / "pos-terminal.png").convert("RGBA").resize((380, 285))
    frames[2].alpha_composite(pos_image, (820, 255))

    frames[0].save(
        MEDIA / "install-flow.gif",
        save_all=True,
        append_images=frames[1:],
        duration=[1800, 1800, 1800],
        loop=0,
        disposal=2,
    )


if __name__ == "__main__":
    make_install_gif()
