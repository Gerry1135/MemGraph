/*
 Copyright (c) 2016 Gerry Iles (Padishar)

 Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:

 The above copyright notice and this permission notice shall be included in
 all copies or substantial portions of the Software.

 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 THE SOFTWARE.
*/

using System;

namespace MemGraph
{

    class Item8
    {
        public Item8 next = null;
    }

    class Item16
    {
        public Item16 next = null;
        double d2;
    }

    class Item24
    {
        public Item24 next = null;
        double d2;
        double d3;
    }

    class PadHeap
    {
        int megsPerSize = 32;

        Item8 head8 = null;
        Item16 head16 = null;
        Item24 head24 = null;

        LogMsg Log = new LogMsg();

        int[] lengths = new int[] { 8, 16, 24, 40, 56, 72, 88, 120, 152, 184, 216, 272, 328, 408 /*, 640, 776, 984, 1320, 2008*/ };
        int[] chunks = new int[] { 4, 3, 3, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
        object[][] heads = new object[][] { null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null };

        public void Pad()
        {
            try
            {
                long curMem = GC.GetTotalMemory(false);
                Log.buf.Append("Pad(");
                Log.buf.Append(megsPerSize);
                Log.buf.Append(") started, memory = ");
                Log.buf.Append((curMem / 1024));
                Log.buf.AppendLine(" KB");

                head8 = null;
                head16 = null;
                head24 = null;
                for (int i = 0; i < heads.Length; i++)
                    heads[i] = null;

                GC.Collect();
                curMem = GC.GetTotalMemory(false);
                Log.buf.Append("After disard and collect, memory = ");
                Log.buf.Append((curMem / 1024));
                Log.buf.AppendLine(" KB");

                // Do the small sizes with custom classes
                Pad8();
                Pad16();
                Pad24();

                // Do the rest of the sizes with arrays of object
                for (int i = 0; i < lengths.Length; i++)
                    PadArray(i);

                curMem = GC.GetTotalMemory(false);
                Log.buf.Append("After padding, memory = ");
                Log.buf.Append((curMem / 1024));
                Log.buf.AppendLine(" KB");
            }
            catch (Exception e)
            {
                Log.buf.AppendLine(e.ToString());
            }
            Log.Flush();

            //megsPerSize = megsPerSize * 2;
        }

        void Pad8()
        {
            Log.buf.AppendLine("Pad8");

            long lastMem = GC.GetTotalMemory(false);
            long totalAlloc = 0;    // The amount of memory we have locked against GC
            long maxAlloc = megsPerSize * 10 * 1024 * 1024;
            Item8 tempHead = null;
            Item8 test;
            while (totalAlloc < maxAlloc)
            {
                // Allocate a block
                test = new Item8();

                long curMem = GC.GetTotalMemory(false);
                if (curMem == lastMem + 4096)
                {
                    totalAlloc += 4096;

                    // Add the block to the keep list
                    test.next = head8;
                    head8 = test;
                }
                else
                {
                    // Add the block to the temp list
                    test.next = tempHead;
                    tempHead = test;
                }

                lastMem = curMem;
            }
        }

        void Pad16()
        {
            Log.buf.AppendLine("Pad16");

            long lastMem = GC.GetTotalMemory(false);
            long totalAlloc = 0;    // The amount of memory we have locked against GC
            long maxAlloc = megsPerSize * 7 * 1024 * 1024;
            Item16 tempHead = null;
            Item16 test;
            while (totalAlloc < maxAlloc)
            {
                // Allocate a block
                test = new Item16();

                long curMem = GC.GetTotalMemory(false);
                if (curMem == lastMem + 4096)
                {
                    totalAlloc += 4096;

                    // Add the block to the keep list
                    test.next = head16;
                    head16 = test;
                }
                else
                {
                    // Add the block to the temp list
                    test.next = tempHead;
                    tempHead = test;
                }

                lastMem = curMem;
            }
        }

        void Pad24()
        {
            Log.buf.AppendLine("Pad24");

            long lastMem = GC.GetTotalMemory(false);
            long totalAlloc = 0;    // The amount of memory we have locked against GC
            long maxAlloc = megsPerSize * 5 * 1024 * 1024;
            Item24 tempHead = null;
            Item24 test;
            while (totalAlloc < maxAlloc)
            {
                // Allocate a block
                test = new Item24();

                long curMem = GC.GetTotalMemory(false);
                if (curMem == lastMem + 4096)
                {
                    totalAlloc += 4096;

                    // Add the block to the keep list
                    test.next = head24;
                    head24 = test;
                }
                else
                {
                    // Add the block to the temp list
                    test.next = tempHead;
                    tempHead = test;
                }

                lastMem = curMem;
            }
        }

        void PadArray(int index)
        {
            int bytes = lengths[index];
            int refCount = bytes / 8;

            Log.buf.Append("PadArray(");
            Log.buf.Append(bytes);
            Log.buf.AppendLine(")");

            long lastMem = GC.GetTotalMemory(false);
            long totalAlloc = 0;    // The amount of memory we have locked against GC
            long maxAlloc = megsPerSize * chunks[index] * 1024 * 1024;
            object[] tempHead = null;
            object[] test;
            while (totalAlloc < maxAlloc)
            {
                // Allocate a block
                test = new object[refCount];

                long curMem = GC.GetTotalMemory(false);
                if (curMem == lastMem + 4096)
                {
                    totalAlloc += 4096;

                    // Add the block to the keep list
                    test[0] = heads[index];
                    heads[index] = test;
                }
                else
                {
                    // Add the block to the temp list
                    test[0] = tempHead;
                    tempHead = test;
                }

                lastMem = curMem;
            }
        }
    }
}
