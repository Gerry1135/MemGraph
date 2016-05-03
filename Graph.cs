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

        public double[] values = new double[GraphWidth];
        public Texture2D texGraph;

        int valIndex = 0;
        int lastRendered = 0;

        public double lastValue;
        public String lastValueStr;

        long lastAlloc = 0;

        long totalAlloc = 0;

        double vscale = 1e-6;

        long startTime;
        long ticksPerSec;

        bool fullUpdate = false;

        const String lastValuePattern = "Last: {0}%";

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
                values[j] = 0.0;

            lastValue = 0.0;
            lastValueStr = String.Format(lastValuePattern, lastValue.ToString("N2"));

            startTime = Stopwatch.GetTimestamp();
            ticksPerSec = Stopwatch.Frequency;
        }

        internal void OnDestroy()
        {
        }

        public void AddMemoryIncrement()
        {



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
                double frac = ((double)ticksDelta * 100.0 / (double)timeDelta);
                //print("value = " + frac);
                values[valIndex] = frac;

                if (frac != lastValue)
                {
                    lastValue = frac;
                    lastValueStr = String.Format(lastValuePattern, frac.ToString("N4"));
                }

                startTime = endTime;
                valIndex = (valIndex + 1) % ChannelValues.width;
            }
        }

        public void Update()
        {
            //print("Update Start");

            if (GameSettings.MODIFIER_KEY.GetKey() && Input.GetKeyDown(KeyCode.Minus))
            {
                showUI = !showUI;
            }

            if (!showUI)
                return;

            if (fullUpdate)
            {
                fullUpdate = false;
                lastRendered = (valIndex + 1) % ChannelValues.width;
            }

            // If we want to update this time
            if (lastRendered != valIndex)
            {
                for (int i = 0; i < NumChannels + 1; i++)
                {
                    // We're going to wrap this back round to the start so copy the value so 
                    int startlastRend = lastRendered;

                    ChannelValues data = dataarray[i];

                    // Update the columns from lastRendered to frameIndex
                    if (startlastRend >= valIndex)
                    {
                        for (int x = startlastRend; x < ChannelValues.width; x++)
                        {
                            DrawColumn(data.texGraph, x, (int)data.values[x], redLine);
                        }

                        startlastRend = 0;
                    }

                    for (int x = startlastRend; x < valIndex; x++)
                    {
                        DrawColumn(data.texGraph, x, (int)data.values[x], redLine);
                    }

                    if (valIndex < ChannelValues.width)
                        data.texGraph.SetPixels(valIndex, 0, 1, ChannelValues.height, blackLine);
                    if (valIndex != ChannelValues.width - 2)
                        data.texGraph.SetPixels((valIndex + 1) % ChannelValues.width, 0, 1, ChannelValues.height, blackLine);
                    data.texGraph.Apply();
                }

                lastRendered = valIndex;
            }
            //print("Update End");
        }

        private void DrawColumn(Texture2D tex, int x, int y, Color[] col)
        {
            //print("drawcol(" + x + ", " + y + ")");
            if (y > ChannelValues.height - 1)
                y = ChannelValues.height - 1;
            tex.SetPixels(x, 0, 1, y + 1, col);
            if (y < ChannelValues.height - 1)
                tex.SetPixels(x, y + 1, 1, ChannelValues.height - 1 - y, blackLine);
        }

        public void OnGUI()
        {
            if (labelStyle == null)
                labelStyle = new GUIStyle(GUI.skin.label);

            if (wndWidth == null)
                wndWidth = GUILayout.Width(ChannelValues.width);
            if (wndHeight == null)
                wndHeight = GUILayout.Height(ChannelValues.height);
            if (graphHeight == null)
                graphHeight = GUILayout.Height(ChannelValues.height);

            if (showUI)
                windowPos = GUILayout.Window(3651275, windowPos, WindowGUI, "Profile Graph", wndWidth, wndHeight);
        }

        public void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();
            for (int i = 0; i < NumChannels + 1; i++)
            {
                ChannelValues data = dataarray[i];

                GUILayout.BeginVertical();
                GUILayout.Box(data.texGraph, wndWidth, graphHeight);

                GUILayout.BeginHorizontal();
                GUILayout.Label(data.lastValueStr, labelStyle);
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();

            GUI.DragWindow();
        }
    }
}
