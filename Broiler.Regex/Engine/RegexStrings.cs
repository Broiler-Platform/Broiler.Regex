// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   6
// Annotated:        6/6
// Exempt:           9
// Human-reviewed:   0/6
// IP risk:          None
// Security risk:    High
// Criteria:         2/1
// Resource impact:  2/10 max
// Unverified:       6
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.Regex.Engine;

/// <summary>
/// The strings of a <c>v</c>-mode class, as <see cref="RegexOp.StringClass"/> reads them: each string of more than one
/// character as its code points, kept by the character a match of it begins with, its first, or, read right to left in a
/// look-behind's body, its last. It is built once by the program compiler and never changed, and every occurrence of the
/// same strings in one program, read in the same direction and under the same folding, shares it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why by one character.</b> ECMA-262 compiles such a class to an alternation of its strings longest first, then its
/// single characters, then the empty string (§22.2.2.7, Atom :: CharacterClass, steps 3 to 8). A string can match at an
/// index only if its first character is the one there, so of the strings that can match at an index, all begin with the
/// same character: the strings kept under that character, in their order, are the alternatives that can match, in the
/// alternation's order. The rest would each fail at their first character.
/// </para>
/// <para>
/// <b>The layout.</b> The keys, the characters strings begin with, are sorted and distinct, and the strings kept under
/// key <c>k</c> are entries <c>GroupStart(k)</c> to <c>GroupStart(k + 1) - 1</c> of the members, each a string's number,
/// in the order the class gives them: longest first, as the parser sorted them. Each member also records where its group
/// ends, so that a backtrack frame need keep only the member it goes on from. A string's code points are a run of the
/// text, in the order they are written; under a fold they are each one's Canonicalize, as the instructions of the
/// alternation would compare them.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7; IP=None; Security=High; Resources=2; Fingerprint=40E33A
// Broiler-Falsified-If: two strings that begin with the same character are kept in an order other than the class's, a string is kept under a character other than its first, or right to left other than its last, or a program's two occurrences of the same strings read in the same direction get two tables
// Broiler-Human:        PENDING
internal sealed class RegexStrings
{
    /// <summary>The characters a kept string begins with (its last, right to left), sorted and distinct.</summary>
    private readonly int[] _keys;

    /// <summary>For each key, the index of its first member; one more entry, the members' count, closes the last group.</summary>
    private readonly int[] _groupStarts;

    /// <summary>The strings' numbers, grouped by key, each group in the class's order.</summary>
    private readonly int[] _members;

    /// <summary>For each member, the index after the last member of its group.</summary>
    private readonly int[] _groupEnds;

    /// <summary>For each string, the index of its first code point in the text; one more entry closes the last.</summary>
    private readonly int[] _textStarts;

    /// <summary>Every string's code points, one after another, as written.</summary>
    private readonly int[] _text;

    /// <summary>A table over the arrays given, which the caller hands over and does not keep.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=DEEA33
    // Broiler-Human:        PENDING
    internal RegexStrings(int[] keys, int[] groupStarts, int[] members, int[] groupEnds, int[] textStarts, int[] text, bool rightToLeft, bool folded)
    {
        _keys = keys;
        _groupStarts = groupStarts;
        _members = members;
        _groupEnds = groupEnds;
        _textStarts = textStarts;
        _text = text;
        RightToLeft = rightToLeft;
        Folded = folded;
    }

    /// <summary>
    /// Whether the strings are read right to left, in a look-behind's body: a string is then kept by its last character,
    /// and compared from it to its first.
    /// </summary>
    internal bool RightToLeft { get; }

    /// <summary>
    /// Whether the strings were folded when parsed (a <c>v</c>-mode class under <c>i</c>): their code points are canonical,
    /// and a character of the subject is compared, and looked up, by its Canonicalize.
    /// </summary>
    internal bool Folded { get; }

    /// <summary>The number of members: the candidate index past every string, where the class's single characters are tried.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=78ADB6
    // Broiler-Human:        PENDING
    internal int Count => _members.Length;

    /// <summary>How many distinct characters strings begin with: the size of the search a lookup makes.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=272B61
    // Broiler-Human:        PENDING
    internal int KeyCount => _keys.Length;

    /// <summary>
    /// The first member kept under <paramref name="key"/>, or <see cref="Count"/> when no string begins with it: a binary
    /// search over the keys.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=25B128
    // Broiler-Falsified-If: a key equal to the first or the last of the sorted keys is reported absent, or a character no string begins with is given a member
    // Broiler-Human:        PENDING
    internal int FirstMember(int key)
    {
        var low = 0;
        var high = _keys.Length - 1;
        while (low <= high)
        {
            var middle = (low + high) >> 1;
            if (key < _keys[middle])
                high = middle - 1;
            else if (key > _keys[middle])
                low = middle + 1;
            else
                return _groupStarts[middle];
        }

        return _members.Length;
    }

    /// <summary>The member after <paramref name="member"/> in its group, or <see cref="Count"/> after the group's last.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=1235EC
    // Broiler-Human:        PENDING
    internal int NextMember(int member) => member + 1 < _groupEnds[member] ? member + 1 : _members.Length;

    /// <summary>The code points of the string at <paramref name="member"/>, as written.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=0BF70C
    // Broiler-Human:        PENDING
    internal ReadOnlySpan<int> Text(int member)
    {
        var number = _members[member];
        return _text.AsSpan(_textStarts[number], _textStarts[number + 1] - _textStarts[number]);
    }
}
