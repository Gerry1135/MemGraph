using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace MemGraph
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class Graph : MonoBehaviour
    {
        private const int GraphWidth = 500;
        private const int GraphHeight = 100;

        private Rect windowPos = new Rect(80, 80, 400, 50);
        private bool showUI = false;

        public long[] values = new long[GraphWidth];
        public Texture2D texGraph;

        int valIndex = 0;
        int lastRendered = 0;

        public long lastValue;
        //public String lastValueStr;

        int lastColCount = 0;
        long lastAlloc = 0;

        long totalAlloc = 0;

        double vscale = 1e-6;

        long startTime;
        long ticksPerSec;

        bool fullUpdate = false;

        //const String lastValuePattern = "Last: {0}%";

        Color[] blackLine;
        Color[] redLine;
        Color[] greenLine;
        Color[] blueLine;

        private GUIStyle labelStyle;
        private GUILayoutOption wndWidth;
        private GUILayoutOption wndHeight;
        private GUILayoutOption graphHeight;

        internal void Awake()
        {
            DontDestroyOnLoad(gameObject);

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

            lastValue = 0;
            //lastValueStr = String.Format(lastValuePattern, lastValue.ToString("N2"));

            lastColCount = GC.CollectionCount(GC.MaxGeneration);
            lastAlloc = GC.GetTotalMemory(false);

            startTime = Stopwatch.GetTimestamp();
            ticksPerSec = Stopwatch.Frequency;
        }

        internal void OnDestroy()
        {
        }

        public void AddMemoryIncrement()
        {
            int colCount = GC.CollectionCount(GC.MaxGeneration);
            if (lastColCount == colCount)
            {
                long currentMem = GC.GetTotalMemory(false);

                totalAlloc += (currentMem - lastAlloc);

                lastAlloc = currentMem;
            }
        }

        public void FixedUpdate()
        {
            AddMemoryIncrement();
            
            // If at least 1 second has passed then record and reset
            long endTime = Stopwatch.GetTimestamp();
            long timeDelta = endTime - startTime;
            //print("timeDelta = " + timeDelta);
            if (timeDelta > ticksPerSec)
            {
                values[valIndex] = totalAlloc;

                if (totalAlloc != lastValue)
                {
                    lastValue = totalAlloc;
                    //lastValueStr = String.Format(lastValuePattern, lastValue.ToString("N4"));
                }

                startTime = endTime;
                valIndex = (valIndex + 1) % GraphWidth;
            }
        }

        public void Update()
        {
            //print("Update Start");
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
                }
                if (Input.GetKeyDown(KeyCode.KeypadMinus))
                {
                    // Decrease scale
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
                // We're going to wrap this back round to the start so copy the value so 
                int startlastRend = lastRendered;

                // Update the columns from lastRendered to frameIndex
                if (startlastRend >= valIndex)
                {
                    for (int x = startlastRend; x < GraphWidth; x++)
                    {
                        DrawColumn(texGraph, x, (int)((double)values[x] * vscale), redLine);
                    }

                    startlastRend = 0;
                }

                for (int x = startlastRend; x < valIndex; x++)
                {
                    DrawColumn(texGraph, x, (int)((double)values[x] * vscale), redLine);
                }

                if (valIndex < GraphWidth)
                    texGraph.SetPixels(valIndex, 0, 1, GraphHeight, blackLine);
                if (valIndex != GraphWidth - 2)
                    texGraph.SetPixels((valIndex + 1) % GraphWidth, 0, 1, GraphHeight, blackLine);
                texGraph.Apply();

                lastRendered = valIndex;
            }
            //print("Update End");
        }

        private void DrawColumn(Texture2D tex, int x, int y, Color[] col)
        {
            //print("drawcol(" + x + ", " + y + ")");
            if (y > GraphHeight - 1)
                y = GraphHeight - 1;
            tex.SetPixels(x, 0, 1, y + 1, col);
            if (y < GraphHeight - 1)
                tex.SetPixels(x, y + 1, 1, GraphHeight - 1 - y, blackLine);
        }

        public void OnGUI()
        {
            if (labelStyle == null)
                labelStyle = new GUIStyle(GUI.skin.label);

            if (wndWidth == null)
                wndWidth = GUILayout.Width(GraphWidth);
            if (wndHeight == null)
                wndHeight = GUILayout.Height(GraphHeight);
            if (graphHeight == null)
                graphHeight = GUILayout.Height(GraphHeight);

            if (showUI)
                windowPos = GUILayout.Window(3651275, windowPos, WindowGUI, "Profile Graph", wndWidth, wndHeight);
        }

        public void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Box(texGraph, wndWidth, graphHeight);

            //GUILayout.BeginHorizontal();
            //GUILayout.Label(lastValueStr, labelStyle);
            //GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }
    }
}
