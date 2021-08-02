FilterBuddy
=======

The SBIG CFW-9 is usually connected to the I2C-AUX port of a USB SBIG ST-7/8/9/10/2000 CCD camera body.  Until now, this could only be controlled by these cameras using the SBIG camera drivers and client software such as MaxIm DL, etc.  This is quite disappointing as the filter wheel cannot be used with other cameras.

Diffraction Limited (owner of SBIG) will not release details of the hardware and interface details as this "information is proprietary and confidential".  It also says it does not "support operation via third party hardware and doing so may void your warranty".  Probably all CFW-9 filter wheels are out of warranty!

So, after much investigation the hardware, interface, and software protocol has been worked out.  Full details will be found in the PDF file "CFW-9 Interface Details.pdf".

This repository contains the Visual Basic code necessary to create an ASCOM driver and Arduino code to control an SBIG CFW-9 filter wheel.

Prequistites to compile this code is as follows:

1. ASCOM Platform
        https://ascom-standards.org/Downloads/Index.htm

2. ASCOM Platform Development Components
        https://ascom-standards.org/Downloads/PlatDevComponents.htm

3. Microsoft .NET Framework 4.8
        https://dotnet.microsoft.com/download/dotnet-framework/net48

4. Visual Studio 2019  (Community is free - ensure Visual Basic componets are installed)
        https://visualstudio.microsoft.com/downloads/

5. Arduino IDE
        https://www.arduino.cc/en/Main/Software


Keith Rickard
2 August 2021
