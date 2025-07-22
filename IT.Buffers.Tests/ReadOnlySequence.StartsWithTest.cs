using IT.Buffers.Extensions;

namespace IT.Buffers.Tests;

public class ReadOnlySequence_StartsWithTest
{
    [Test]
    public void StartsWithTest()
    {
        var span = "--bo"u8;
        var memory = span.ToArray().AsMemory();
        var length = span.Length;

        for (int i = 1; i <= length; i++)
        {
            var seq = memory.SplitBySegments(i);
            Assert.That(seq.StartsWith("-"u8), Is.True);
            Assert.That(seq.StartsWith("--"u8), Is.True);
            Assert.That(seq.StartsWith("--b"u8), Is.True);
            Assert.That(seq.StartsWith("--bo"u8), Is.True);

            var current = seq.Start;
            Assert.That(seq.StartsWith("-"u8, ref current), Is.True);
            Assert.That(seq.Slice(current).SequenceEqual("-bo"u8), Is.True);

            current = seq.Start;
            Assert.That(seq.StartsWith("--"u8, ref current), Is.True);
            Assert.That(seq.Slice(current).SequenceEqual("bo"u8), Is.True);

            current = seq.Start;
            Assert.That(seq.StartsWith("--b"u8, ref current), Is.True);
            Assert.That(seq.Slice(current).SequenceEqual("o"u8), Is.True);
            
            current = seq.Start;
            Assert.That(seq.StartsWith("--bo"u8, ref current), Is.True);
            Assert.That(seq.Slice(current).IsEmpty, Is.True);

            Assert.That(seq.StartsWith("--bo-"u8), Is.False);
            Assert.That(seq.StartsWith("---++"u8), Is.False);
            Assert.That(seq.StartsWith("---"u8), Is.False);
            Assert.That(seq.StartsWith("++"u8), Is.False);
        }
    }
}