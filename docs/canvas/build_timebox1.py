# -*- coding: utf-8 -*-
"""Publish the Timebox 1 requirements as Canvas-ready HTML.

    python docs/canvas/build_timebox1.py

Writes docs/TIMEBOX_1_REQUIREMENTS.html (the whole document on one page, for
printing or a single Canvas page) and one page per '##' section into
docs/canvas/timebox1/. Each file names its suggested Canvas page title in a comment at
the top. Edit the Markdown, never the HTML.
"""
import os

from canvas_render import build

HERE = os.path.dirname(os.path.abspath(__file__))

build(
    label="Timebox 1",
    source=os.path.join(HERE, os.pardir, "TIMEBOX_1_REQUIREMENTS.md"),
    out_dir=os.path.join(HERE, "timebox1"),
)
