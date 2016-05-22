# MemGraph
Copyright (c) 2016 Gerry Iles (Padishar)

This is a simple plugin to display of graph of memory allocation and garbage collection.  It is intended as a troubleshooting 
and development aid rather than for general use.

## Installation
Copy the DLL from the zip file into the GameData folder of your KSP installation.

## Usage
Mod-KeypadMultiply toggles the display of the window.  
Mod-KeypadPlus increases the vertical scale of the graph.  
Mod-KeypadMinus decreases the vertical scale of the graph.  
Mod-KeypadDivide runs a bit of test code controlled by MemGraph\PluginData\test.cfg.  
Mod-End pads the Mono heap with approx 1.5 GB of headroom to reduce frequency of garbage collections.

Every second the plugin totals up all the memory allocated on the heap and whether any garbage collections have run.  It also 
displays the current total heap allocation and counts the number of times the Update and FixedUpdate functions are called. 
The current total is shown as HWM and the values for the last interval are shown in the window as "Last", "U" and "FU".  The 
graph is 512 pixels wide and shows the last 8 minutes 32 seconds of the memory allocation in green.  If a garbage collection 
happens during the interval, that column of the graph will have a red background.

The test code basically allocates a number of blocks of the specified size and displays which allocations actually cause the 
allocated heap to change.  This allows us to deduce various characteristics of the memory allocator and garbage collection 
mechanisms.

The code is released under the MIT license (see https://github.com/Gerry1135/MemGraph/blob/master/Graph.cs).
