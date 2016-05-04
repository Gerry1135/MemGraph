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
        public Texture2D texGraph = new Texture2D(GraphWidth, GraphHeight);

        int valIndex = 0;
        int lastRendered = 0;

        public long lastValue;

        int lastColCount = 0;
        long lastAlloc = 0;

        long totalAlloc = 0;

        double vscale;
        public String guiStr;

        long startTime;
        long ticksPerSec;

        bool fullUpdate = false;

        int scaleIndex = 4;

        const int numScales = 6;
        static double[] valCycle = { 1024, 10240, 102400, 1024 * 1024, 1024 * 1024 * 10, 1024 * 1024 * 100 };
        static String[] valCycleStr = { "1 KB", "10 KB", "100 KB", "1 MB", "10 MB", "100 MB" };

        //const String lastValuePattern = "Last: {0}%";

        Color[] blackLine;
        Color[] redLine;
        Color[] greenLine;
        Color[] blueLine;

        static StringBuilder strBuild = new StringBuilder(128);

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

            //lastValue = 0;
            //lastValueStr = String.Format(lastValuePattern, lastValue.ToString("N2"));

            vscale = valCycle[scaleIndex];
            UpdateGuiStr();

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

                long diff = currentMem - lastAlloc;
                print("diff = " + diff);
                if (diff > 0)
                    totalAlloc += diff;

                lastAlloc = currentMem;
            }
            else
            {
                //print("GC has run");
                lastColCount = colCount;
            }
        }

        void UpdateGuiStr()
        {
            strBuild.Length = 0;
            strBuild.Append("Scale: ");
            strBuild.Append(valCycleStr[scaleIndex]);
            strBuild.Append("   Last: ");
            strBuild.Append(lastValue / 1024);
            strBuild.Append(" KB");
            guiStr = strBuild.ToString();
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
                //print("totalAlloc = " + totalAlloc);

                if (totalAlloc != lastValue)
                {
                    lastValue = totalAlloc;
                    UpdateGuiStr();
                }

                startTime = endTime;
                totalAlloc = 0;
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
                    scaleIndex = (scaleIndex + 1) % numScales;
                    vscale = valCycle[scaleIndex];
                    UpdateGuiStr();
                    fullUpdate = true;
                }
                if (Input.GetKeyDown(KeyCode.KeypadMinus))
                {
                    // Decrease scale
                    scaleIndex = (scaleIndex + numScales - 1) % numScales;
                    vscale = valCycle[scaleIndex];
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
                // We're going to wrap this back round to the start so copy the value so 
                int startlastRend = lastRendered;

                // Update the columns from lastRendered to frameIndex
                if (startlastRend >= valIndex)
                {
                    for (int x = startlastRend; x < GraphWidth; x++)
                    {
                        DrawColumn(texGraph, x, (int)((double)values[x] * GraphHeight / vscale), redLine);
                    }

                    startlastRend = 0;
                }

                for (int x = startlastRend; x < valIndex; x++)
                {
                    DrawColumn(texGraph, x, (int)((double)values[x] * GraphHeight / vscale), redLine);
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

            GUILayout.BeginHorizontal();
            GUILayout.Label(guiStr, labelStyle);
            GUILayout.EndHorizontal();

            GUILayout.Box(texGraph, wndWidth, graphHeight);
            GUILayout.EndVertical();

            GUI.DragWindow();
        }
    }
}
