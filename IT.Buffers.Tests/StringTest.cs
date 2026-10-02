using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IT.Buffers.Tests;

internal class StringTest
{
    //[Test]
    public void Test()
    {
        var length = BufferSize.Max_String;

        var str = new string(' ', length);

        var str2 = string.Create(length, ' ', (span, state) =>
        {
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = state;
            }
        });

        Assert.That(str, Is.EqualTo(str2));
    }
}