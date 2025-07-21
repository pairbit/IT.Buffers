using IT.Buffers.Extensions;
using System.Buffers;

namespace IT.Buffers.Tests;

public class ReadOnlySequence_PositionOfTest
{
    [Test]
    public void SeqTest()
    {
        var span = "--Sep--body--Sep--"u8;//18
        var memory = span.ToArray().AsMemory();
        var sep = "--Sep--"u8;
        var length = span.Length;

        for (int i = 1; i <= length; i++)
        {
            var seq = memory.SplitBySegments(i);
            Assert.That(seq.SequenceEqual(span), Is.True);

            Assert.That(seq.PositionOf("[]"u8).IsNegative(), Is.True);
            Assert.That(seq.PositionOf(sep), Is.EqualTo(seq.Start));

            var end = seq.PositionOfEnd(sep);
            var sliced = seq.Slice(end);
            Assert.That(sliced.SequenceEqual(seq.Slice(seq.GetPosition(sep.Length))), Is.True);
            Assert.That(sliced.SequenceEqual("body--Sep--"u8), Is.True);

            var pos = seq.PositionOf("y--Sep--"u8);
            Assert.That(seq.Slice(seq.Start, pos).SequenceEqual("--Sep--bod"u8), Is.True);

            end = seq.GetPosition(sep.Length + 1, pos);
            Assert.That(seq.PositionOfEnd("y--Sep--"u8), Is.EqualTo(end));
        }
    }

    [Test]
    public void Seq2Test()
    {
        var span = "--Sep--body--Sep--"u8;//18
        var memory = span.ToArray().AsMemory();
        var sep = "--Sep--"u8;
        var length = span.Length;

        for (int i = 1; i <= length; i++)
        {
            var seq = memory.SplitBySegments(i);
            Assert.That(seq.SequenceEqual(span), Is.True);

            var start = seq.PositionOfEnd(sep);
            Assert.That(start.IsNegative(), Is.False);

            var sliced = seq.Slice(start);
            Assert.That(sliced.SequenceEqual("body--Sep--"u8), Is.True);

            var end = sliced.PositionOf(sep);
            Assert.That(end.IsNegative(), Is.False);
            Assert.That(seq.PositionOf(sep, start), Is.EqualTo(end));

            var body = sliced.Slice(0, end);
            Assert.That(body.SequenceEqual("body"u8), Is.True);
            Assert.That(seq.Slice(start, end).SequenceEqual(body), Is.True);

            Assert.That(seq.PositionOf("[]"u8).IsNegative(), Is.True);
        }
    }
}