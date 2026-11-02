# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""SplitMix64, and how bundle sjl-11-001's population generator reads a draw.

The generator is Broiler.Unicode's scripts/reference-arms/splitmix64.py (git blob
90c9bbe57a2e54046b5de02f0830d516c8e5e05a at Broiler.Unicode 9d636c1), itself Broiler.JS's `splitmix64`
(scripts/conformance/generate_number_vectors.py), copied with this docstring and the licence header changed and
nothing else: Sebastiano Vigna's splitmix64.c, a 64-bit state advanced by 0x9E3779B97F4A7C15 and mixed by two
multiply-xorshift rounds. tests/test_splitmix64.py checks it against the published reference outputs.

**A draw** is one output of the stream started from a class's seed, consumed in the order the generator states
(README.md, "P4"). It is read in one of four ways, and only these:

- a choice among n things takes the draw modulo n (`choice`);
- an integer from [low, high] is low plus the draw modulo (high - low + 1) (`between`);
- a choice that needs a second value from the same draw takes it from the draw shifted right by 16 bits,
  modulo its own range (`second`);
- a choice that needs a third value from the same draw takes it from the draw shifted right by 32 bits,
  modulo its own range (`third`).
"""

from __future__ import annotations

from collections.abc import Iterator

MASK = 0xFFFFFFFFFFFFFFFF


def splitmix64(seed: int) -> Iterator[int]:
    state = seed & MASK
    while True:
        state = (state + 0x9E3779B97F4A7C15) & MASK
        z = state
        z = ((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9) & MASK
        z = ((z ^ (z >> 27)) * 0x94D049BB133111EB) & MASK
        yield z ^ (z >> 31)


def choice(draw: int, n: int) -> int:
    """A choice among n things: the draw modulo n."""
    return draw % n


def between(draw: int, low: int, high: int) -> int:
    """An integer from [low, high]: low plus the draw modulo the range's size."""
    return low + draw % (high - low + 1)


def second(draw: int, n: int) -> int:
    """A second value from the same draw: the draw shifted right by 16 bits, modulo n."""
    return (draw >> 16) % n


def third(draw: int, n: int) -> int:
    """A third value from the same draw: the draw shifted right by 32 bits, modulo n."""
    return (draw >> 32) % n
