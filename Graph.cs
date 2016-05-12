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
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace MemGraph
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class Graph : MonoBehaviour
    {
        const int GraphX = 10;
        const int GraphY = 36;
        const int GraphWidth = 512;
        const int GraphHeight = 256;
        const int LabelX = 10;
        const int LabelY = 18;
        const int LabelWidth = GraphWidth;
        const int LabelHeight = 20;
        const int WndWidth = GraphWidth + 8;
        const int WndHeight = GraphHeight + 42;

        const int numScales = 13;   // Number of entries in the scale array
        const int kb = 1024;
        const int mb = kb * kb;

        Rect windowPos;
        int windowId = 0;
        string windowTitle;
        bool showUI = true;
        Rect labelRect;
        Rect graphRect;

        long[] values;
        bool[] flags;
        Texture2D texGraph;

        int valIndex = 0;           // The current index into the values array
        int lastRendered = 0;       // The last index of the values array that has been rendered into the texture

        int lastColCount = 0;       // The most recent count of GC runs
        long lastAlloc = 0;         // The most recent value of total memory
        long lastAllocMB = 0;       // The most recent value of total memory in MB displayed in window

        long totalAlloc = 0;        // The sum of all the memory deltas (this is the value stored in the array every ~1s)
        bool doneGC = false;        // Has a GC run (this is also stored)

        long lastValue = 0;         // The last value stored in the array
        int lastFixedCount = 0;     // The last value of fixedCount used to build guiStr
        int lastUpdateCount = 0;    // The last value of updateCount used to build guiStr
        int fixedCount = 0;         // The count of FixedUpdate calls in the current interval
        int updateCount = 0;        // The count of Update calls in the current interval

        string guiStr;              // The string at the top of the window (only updated when required)

        long startTime;             // The timestamp totalAlloc was last stored
        long ticksPerSec;           // The number of timestamp ticks in a second

        bool fullUpdate = true;     // Flag to force re-render of entire texture (e.g. when changing scale)

        int scaleIndex = 4;         // Index of the current vertical scale
        static double[] valCycle;
        static string[] valCycleStr;

        Color[] blackLine;
        Color[] redLine;
        Color[] greenLine;
        Color[] blueLine;

        static StringBuilder strBuild;

        GUIStyle labelStyle;

        GUI.WindowFunction wndFunction = null;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);

            windowId = Guid.NewGuid().GetHashCode();
            windowTitle = "MemGraph 1.0.0.3";

            strBuild = new StringBuilder(128);

            valCycle = new double[] { 64 * kb, 128 * kb, 256 * kb, 512 * kb, 1 * mb, 2 * mb, 4 * mb, 8 * mb, 16 * mb, 32 * mb, 64 * mb, 128 * mb, 256 * mb };
            valCycleStr = new string[] { "64 KB", "128 KB", "256 KB", "512 KB", "1 MB", "2 MB", "4 MB", "8 MB", "16 MB", "32 MB", "64 MB", "128 MB", "256 MB" };

            windowPos.Set(80, 80, WndWidth, WndHeight);
            labelRect.Set(LabelX, LabelY, LabelWidth, LabelHeight);
            graphRect.Set(GraphX, GraphY, GraphWidth, GraphHeight);

            values = new long[GraphWidth];
            flags = new bool[GraphWidth];
            texGraph = new Texture2D(GraphWidth, GraphHeight, TextureFormat.ARGB32, false);

            redLine = new Color[GraphHeight];
            greenLine = new Color[GraphHeight];
            blueLine = new Color[GraphHeight];
            blackLine = new Color[GraphHeight];
            for (int i = 0; i < blackLine.Length; i++)
            {
                blackLine[i] = Color.black;
                redLine[i] = Color.red;
                greenLine[i] = Color.green;
                blueLine[i] = Color.blue;
            }

            for (int j = 0; j < GraphWidth; j++)
                values[j] = 0;

            UpdateGuiStr();

            lastColCount = GC.CollectionCount(GC.MaxGeneration);
            lastAlloc = GC.GetTotalMemory(false);
            lastAllocMB = lastAlloc >> 20;

            startTime = Stopwatch.GetTimestamp();
            ticksPerSec = Stopwatch.Frequency;

            // Force a full update of the graph texture
            fullUpdate = true;
        }

        void AddMemoryIncrement()
        {
            long currentMem = GC.GetTotalMemory(false);
            int colCount = GC.CollectionCount(GC.MaxGeneration);
            if (lastColCount != colCount)
            {
                doneGC = true;
                lastColCount = colCount;
            }

            // If the GC has run then the total memory may have shrunk so only add if it increases
            long diff = currentMem - lastAlloc;
            if (diff > 0)
                totalAlloc += diff;

            // Remember the current memory for next time
            lastAlloc = currentMem;

            long endTime = Stopwatch.GetTimestamp();
            long timeDelta = endTime - startTime;
            if (timeDelta > ticksPerSec)
            {
                // At least 1 second has passed so record the values
                values[valIndex] = totalAlloc;
                flags[valIndex] = doneGC;

                // Calculate the new lastAllocMB value
                long newMB = lastAlloc >> 20;

                // If the gui string needs to change then update it and store the last used value
                if (totalAlloc != lastValue || fixedCount != lastFixedCount || updateCount != lastUpdateCount || newMB != lastAllocMB)
                {
                    lastAllocMB = newMB;
                    lastValue = totalAlloc;
                    lastFixedCount = fixedCount;
                    lastUpdateCount = updateCount;
                    UpdateGuiStr();
                }

                // Reset the values for the next accumulation
                startTime = endTime;
                totalAlloc = 0;
                fixedCount = 0;
                updateCount = 0;
                doneGC = false;

                // Increament the current value index and force full update if we have caught up with the rendering
                valIndex = (valIndex + 1) % GraphWidth;
                fullUpdate = (valIndex == lastRendered);
            }
        }

        void UpdateGuiStr()
        {
            // We use a static StringBuilder to do this to avoid as much garbage as possible
            strBuild.Length = 0;
            strBuild.Append("Scale:");
            strBuild.Append(valCycleStr[scaleIndex]);
            strBuild.Append("   HWM:");
            strBuild.Append(lastAllocMB);
            strBuild.Append(" MB   Last:");
            strBuild.Append(lastValue / 1024);
            strBuild.Append(" KB   U:");
            strBuild.Append(lastUpdateCount);
            strBuild.Append("   FU:");
            strBuild.Append(lastFixedCount);
            guiStr = strBuild.ToString();
        }

        void FixedUpdate()
        {
            fixedCount += 1;
            AddMemoryIncrement();
        }

        void Update()
        {
            //print("Update Start");
            updateCount += 1;
            AddMemoryIncrement();

            if (GameSettings.MODIFIER_KEY.GetKey())
            {
                if (Input.GetKeyDown(KeyCode.KeypadMultiply))
                {
                    showUI = !showUI;
                }
                if (Input.GetKeyDown(KeyCode.KeypadPlus))
                {
                    // Increase scale
                    scaleIndex = (scaleIndex + 1) % numScales;
                    UpdateGuiStr();
                    fullUpdate = true;
                }
                if (Input.GetKeyDown(KeyCode.KeypadMinus))
                {
                    // Decrease scale
                    scaleIndex = (scaleIndex + numScales - 1) % numScales;
                    UpdateGuiStr();
                    fullUpdate = true;
                }
            }

            if (!showUI)
                return;

            if (fullUpdate)
            {
                fullUpdate = false;
                lastRendered = (valIndex + 1) % GraphWidth;
            }

            // If we want to update this time
            if (lastRendered != valIndex)
            {
                double scale = valCycle[scaleIndex];

                // We're going to wrap this back round to the start so copy the value so 
                int startlastRend = lastRendered;

                // Update the columns from lastRendered to valIndex wrapping round at the end
                if (startlastRend >= valIndex)
                {
                    for (int x = startlastRend; x < GraphWidth; x++)
                    {
                        DrawColumn(texGraph, x, (int)((double)values[x] * GraphHeight / scale), greenLine, flags[x] ? redLine : blackLine);
                    }

                    startlastRend = 0;
                }

                for (int x = startlastRend; x < valIndex; x++)
                {
                    DrawColumn(texGraph, x, (int)((double)values[x] * GraphHeight / scale), greenLine, flags[x] ? redLine : blackLine);
                }

                if (valIndex < GraphWidth)
                    texGraph.SetPixels(valIndex, 0, 1, GraphHeight, blueLine);
                if (valIndex != GraphWidth - 2)
                    texGraph.SetPixels((valIndex + 1) % GraphWidth, 0, 1, GraphHeight, blackLine);
                texGraph.Apply();

                lastRendered = valIndex;
            }
            //print("Update End");
        }

        void DrawColumn(Texture2D tex, int x, int y, Color[] fgcol, Color[] bgcol)
        {
            //print("drawcol(" + x + ", " + y + ")");
            if (y > GraphHeight - 1)
                y = GraphHeight - 1;
            tex.SetPixels(x, 0, 1, y + 1, fgcol);
            if (y < GraphHeight - 1)
                tex.SetPixels(x, y + 1, 1, GraphHeight - 1 - y, bgcol);
        }

        void OnGUI()
        {
            if (wndFunction == null)
                wndFunction = new GUI.WindowFunction(WindowGUI);

            if (labelStyle == null)
                labelStyle = new GUIStyle(GUI.skin.label);

            if (showUI)
                windowPos = GUI.Window(windowId, windowPos, wndFunction, windowTitle);
        }

        void WindowGUI(int windowID)
        {
            GUI.Label(labelRect, guiStr, labelStyle);
            GUI.Box(graphRect, texGraph, labelStyle);
            GUI.DragWindow(windowPos);
        }
    }
}
