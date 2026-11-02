# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""The text form of bundle sjl-11-001's population and of the arms' answers, shared by the generator, the comparison
and the classifier (README.md, "The files").

A **string** (a pattern or a subject) is a sequence of UTF-16 code units, lone surrogates included. It is written as
one or more segments joined by `+`; a segment is four upper-case hexadecimal digits per code unit, optionally followed
by `*n`, the segment repeated n times; the empty string is written `.`. So `0062+0061*30` is `b` followed by thirty
`a`. There is one way to write a string that the generator writes (`encode`); every way the format admits is read
(`decode`), so a population written by hand for a test reads as the generator's would.
"""

from __future__ import annotations

import gzip
import io
from collections.abc import Iterable, Iterator
from pathlib import Path

EMPTY = "."
RUN = 8  # the shortest run of one code unit `encode` writes as a repeated segment


def units_of(text: str) -> list[int]:
    """The UTF-16 code units of a Python string whose characters are code points, lone surrogates included."""
    data = text.encode("utf-16-le", "surrogatepass")
    return [data[i] | (data[i + 1] << 8) for i in range(0, len(data), 2)]


def text_of(units: Iterable[int]) -> str:
    """The Python string of a sequence of code units: a lead and a trail surrogate in order make one code point."""
    data = bytearray()
    for unit in units:
        data.append(unit & 0xFF)
        data.append(unit >> 8)
    return bytes(data).decode("utf-16-le", "surrogatepass")


def encode_units(units: list[int]) -> str:
    if not units:
        return EMPTY
    segments: list[str] = []
    plain: list[str] = []
    i = 0
    while i < len(units):
        j = i
        while j < len(units) and units[j] == units[i]:
            j += 1
        if j - i >= RUN:
            if plain:
                segments.append("".join(plain))
                plain = []
            segments.append(f"{units[i]:04X}*{j - i}")
        else:
            plain.extend(f"{u:04X}" for u in units[i:j])
        i = j
    if plain:
        segments.append("".join(plain))
    return "+".join(segments)


def encode(text: str) -> str:
    return encode_units(units_of(text))


def encode_segments(segments: Iterable[tuple[list[int], int]]) -> str:
    """A string given as segments, each code units repeated a count of times, written segment for segment."""
    out = []
    for units, count in segments:
        if not units or count == 0:
            continue
        hexed = "".join(f"{u:04X}" for u in units)
        out.append(hexed if count == 1 else f"{hexed}*{count}")
    return "+".join(out) if out else EMPTY


def decode_units(field: str) -> list[int]:
    if field == EMPTY:
        return []
    units: list[int] = []
    for segment in field.split("+"):
        body, _, count = segment.partition("*")
        if len(body) % 4 != 0 or not body:
            raise ValueError(f"a segment of {len(body)} hexadecimal digits: {segment[:40]}")
        part = [int(body[k:k + 4], 16) for k in range(0, len(body), 4)]
        units.extend(part * (int(count) if count else 1))
    return units


def decode(field: str) -> str:
    return text_of(decode_units(field))


def open_text(path: Path, mode: str = "rt"):
    """A text file, gzip-compressed when its name ends in .gz (written with no name and no time stamp, so that two
    runs write the same bytes)."""
    if str(path).endswith(".gz"):
        if "w" in mode:
            return io.TextIOWrapper(_Gzip(path), encoding="utf-8", newline="\n")
        return io.TextIOWrapper(gzip.open(path, "rb"), encoding="utf-8", newline="\n")
    return open(path, mode, encoding="utf-8", newline="\n")


class _Gzip(gzip.GzipFile):
    """A gzip file written with no name and a zero time stamp, which closes the file it writes to."""

    def __init__(self, path: Path) -> None:
        self._raw = open(path, "wb")
        super().__init__(filename="", mode="wb", fileobj=self._raw, mtime=0, compresslevel=6)

    def close(self) -> None:
        try:
            super().close()
        finally:
            self._raw.close()


def read_lines(path: Path) -> Iterator[str]:
    with open_text(path) as handle:
        for line in handle:
            if line.startswith("#"):
                continue
            yield line.rstrip("\n")
