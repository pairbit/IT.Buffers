using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IT.Buffers.Tests;

internal class SequenceTest
{
    [Test]
    public void Test_GetSpanGetSpan()
    {
        var sequence = new Sequence<byte>();

        var span = sequence.GetSpan();
        var span2 = sequence.GetSpan();
        var span3 = sequence.GetSpan(span.Length + 1);
    }
}