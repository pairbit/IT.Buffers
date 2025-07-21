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

            var end = seq.GetPosition(sep.Length);
            Assert.That(seq.PositionOfEnd(sep), Is.EqualTo(end));
            Assert.That(seq.Slice(end).SequenceEqual("body--Sep--"u8), Is.True);

            var pos = seq.PositionOf("y--Sep--"u8);
            Assert.That(seq.Slice(seq.Start, pos).SequenceEqual("--Sep--bod"u8), Is.True);

            end = seq.GetPosition(sep.Length + 1, pos);
            Assert.That(seq.PositionOfEnd("y--Sep--"u8), Is.EqualTo(end));
        }
    }
}