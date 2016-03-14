Chromaria Alpha v.0.1
by L Soros
http://eplex.cs.ucf.edu/chromaria/home
lsoros@eecs.ucf.edu

:: PRELIMINARY NOTES ::
This version of Chromaria allows you to reproduce the experiments described in the ALIFE'14 submission "Identifying Necessary Conditions for Open-Ended Evolution Through the Artificial Life World of Chromaria".
This initial version of the software is an alpha release. As such, it does not contain the full functionality intended for future releases and may contain bugs (particularly if the instructions in this README are not followed). Additionally, it was originally compiled and tested on Windows 7 as a Windows application. Thus it is not guaranteed to work seamlessly on all platforms. Future releases will be geared towards cross-platform compatibility. In the meantime, please report any problems to lsoros@eecs.ucf.edu. 

:: HOW TO COMPILE ::
This codebase is built using C#/.NET 4.0, and XNA/MonoGame. Please verify that these software components are installed before attempting to build or run Chromaria.

The easiest way to compile this software is to load Chromaria.sln into Visual Studio. 

To compile via the command line, navigate to the folder containing the source code and type "csc /r:Lidgren.Network.dll /r:MonoGame.Framework.dll /r:OpenTK.dll /r:Tao.Sdl.dll *.cs". 

:: HOW TO RUN ::
Run "chromaria.exe" to run the simulation. There is no GUI control; all settings must be preconfigured by setting the parameters in the chromaria-params.txt file. 
Select one of the three run mode options: new novelty search, new evolution run, or replay existing run. Additional mode-specific options are found elsewhere in the parameters file. 

