using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SegmentsAndSlices.Sample
{
    public class Program
    {
        private const int segmentSize = 100;


        public static void Main(string[] args)
        {
            Usage();


            Console.WriteLine($"Name;Runtime in ms;GcTotalMem;");
            for (int i = 0; i < 100; i++)
            {
                Test("List        ", (arr, offset, elements) => 
                                     new List<int>(arr.Skip(offset).Take(elements)));

                Test("Array       ", (arr, offset, elements) => 
                                     new List<int>(arr.Skip(offset).Take(elements)));

                Test("ArraySegment", (arr, offset, elements) => 
                                     new ArraySegment<int>(arr, offset, elements));

                Test("ArrayCopy   ", (arr, offset, elements) =>
                {
                    var array = new int[elements];
                    Array.Copy(arr, offset, array, 0, elements);
                    return array;
                });
            }

            FillSpans();
        }

        private static void Usage()
        {
            var buffer = new byte[10];
            var segment1 = new ArraySegment<byte>(buffer);
            var segment2 = new ArraySegment<byte>(buffer, 5, 3);

            Console.WriteLine("Origin:");
            PrintOut(buffer, segment1, segment2);
            Console.WriteLine();

            Console.WriteLine("Update Buffer[5]:");
            buffer[5] = 0x01;
            PrintOut(buffer, segment1, segment2);
            Console.WriteLine();

            Console.WriteLine("Update Segment1.Array[5]:");
            segment1.Array[5] = 0x02;
            PrintOut(buffer, segment1, segment2);
            Console.WriteLine();

            Console.WriteLine("Update Segment2.Array[Segment2.Offset]:");
            segment2.Array[segment2.Offset] = 0x03;
            PrintOut(buffer, segment1, segment2);
            Console.WriteLine();

            Subslicing();
        }

        private static void PrintOut(IList<byte> data1, IList<byte> data2, IList<byte> data3)
        {
            WriteData("Buffer  ", data1);
            WriteData("Segment1", data2);
            WriteData("Segment2", data3);
        }

        private static void WriteData(string name, IList<byte> data)
        {
            Console.Write($"{name}: ");
            foreach (var item in data)
                Console.Write($"{item} ");
            Console.WriteLine();
        }




        private static void Test(string prefix, Func<int[],int,int, IEnumerable<int>> getSegment, bool logElements = false)
        {
            var sw = new Stopwatch();

            var fsMem = GC.GetTotalMemory(true);
            sw.Start();

            List<Task> tasks = new List<Task>();

            // Create array.
            int[] arr = new int[5000];
            for (int ctr = 0; ctr <= arr.GetUpperBound(0); ctr++)
                arr[ctr] = ctr + 1;

            // Handle array in segments of 10.
            for (int ctr = 1; ctr <= Math.Ceiling(((double)arr.Length) / segmentSize); ctr++)
            {
                int multiplier = ctr;
                int elements = (multiplier - 1) * 10 + segmentSize > arr.Length ?
                                arr.Length - (multiplier - 1) * 10 : segmentSize;
                var segment = getSegment(arr, (ctr - 1) * 10, elements);
                tasks.Add(Task.Run(() => {
                    var list = (IList<int>)segment;
                    for (int index = 0; index < list.Count; index++)
                        list[index] = list[index] * multiplier;
                }));
            }
            try
            {
                Task.WaitAll(tasks.ToArray());

                if (logElements)
                {
                    int elementsShown = 0;
                    foreach (var value in arr)
                    {
                        Console.Write("{0,3} ", value);
                        elementsShown++;
                        if (elementsShown % 18 == 0)
                            Console.WriteLine();
                    }
                }
            }
            catch (AggregateException e)
            {
                Console.WriteLine("Errors occurred when working with the array:");
                foreach (var inner in e.InnerExceptions)
                    Console.WriteLine("{0}: {1}", inner.GetType().Name, inner.Message);
            }

            sw.Stop();
            fsMem = GC.GetTotalMemory(true) - fsMem;
            Console.WriteLine($"{prefix};{sw.Elapsed.TotalMilliseconds}; {fsMem};");
        }


        private unsafe static void FillSpans()
        {
            // Over an array:
            Span<int> ints = new Span<int>(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            // Over a string (of chars):
            Span<char> chars = new Span<char>("Hello, Slice!".ToArray());

            // Over an unmanaged memory buffer:
            byte* bb = stackalloc byte[256];
            Span<byte> bytes = new Span<byte>(bb, 256);


            PrintSpan1(ints);
            PrintSpan1(chars);
            PrintSpan1(bytes);

            PrintSpan2(ints);
            PrintSpan2(chars);
            PrintSpan2(bytes);
        }

        private static void PrintSpan1<T>(Span<T> slice)
        {
            for (int i = 0; i < slice.Length; i++)
                Console.Write("{0} ", slice[i]);
            Console.WriteLine();
        }


        private static void PrintSpan2<T>(Span<T> slice)
        {
            foreach (T t in slice)
                Console.Write("{0} ", t);
            Console.WriteLine();
        }

        private static void Subslicing()
        {
            //Extract sub span
            Span<int> ints = new Span<int>(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            Span<int> subints = ints.Slice(5, 3);

            //No reallocation of the string
            ReadOnlySpan<char> testSpan = "Span Test".Slice();
            int space = testSpan.IndexOf(' ');
            ReadOnlySpan<char> firstName = testSpan.Slice(0, space);
            ReadOnlySpan<char> lastName = testSpan.Slice(space + 1);
        }


        unsafe void Unsafe(byte* payload, int length)
        {
            Safe(new Span<byte>(payload, length));
        }

        void Safe(Span<byte> payload)
        {
            //now the payload could be handled in a safe way because it is wrapped
        }

    }
}
