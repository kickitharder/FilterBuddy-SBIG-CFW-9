'tabs=4
' --------------------------------------------------------------------------------
' TODO fill in this information for your driver, then remove this line!
'
' ASCOM FilterWheel driver for FilterBuddy
'
' Description:	Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam 
'				nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam 
'				erat, sed diam voluptua. At vero eos et accusam et justo duo 
'				dolores et ea rebum. Stet clita kasd gubergren, no sea takimata 
'				sanctus est Lorem ipsum dolor sit amet.
'
' Implements:	ASCOM FilterWheel interface version: 1.0
' Author:		(XXX) Your N. Here <your@email.here>
'
' Edit Log:
'
' Date			Who	Vers	    Description
' -----------	---	-----	    -------------------------------------------------------
' 14-Jul-2020	KRR	0.200714	Initial edit, from FilterWheel template
' ---------------------------------------------------------------------------------
'
'
' Your driver's ID is ASCOM.FilterBuddy.FilterWheel
'
' The Guid attribute sets the CLSID for ASCOM.DeviceName.FilterWheel
' The ClassInterface/None attribute prevents an empty interface called
' _FilterWheel from being created and used as the [default] interface
'

' This definition is used to select code that's only applicable for one device type
#Const Device = "FilterWheel"

Imports ASCOM
Imports ASCOM.Astrometry
Imports ASCOM.Astrometry.AstroUtils
Imports ASCOM.DeviceInterface
Imports ASCOM.Utilities
Imports System.IO.Ports

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Text

<Guid("c2cc9049-96e0-43e1-9b6a-e5f54efe80c7")>
<ClassInterface(ClassInterfaceType.None)>
Public Class FilterWheel

    ' The Guid attribute sets the CLSID for ASCOM.FilterBuddy.FilterWheel
    ' The ClassInterface/None attribute prevents an empty interface called
    ' _FilterBuddy from being created and used as the [default] interface

    ' TODO Replace the not implemented exceptions with code to implement the function or
    ' throw the appropriate ASCOM exception.
    '
    Implements IFilterWheelV2

    '
    ' Driver ID and descriptive string that shows in the Chooser
    '
    Friend Shared driverID As String = "ASCOM.FilterBuddy.FilterWheel"
    Private Shared driverDescription As String = "FilterBuddy"

    Friend Shared comPortProfileName As String = "COM Port" 'Constants used for Profile persistence
    Friend Shared traceStateProfileName As String = "Trace Level"
    Friend Shared comPortDefault As String = "COM1"
    Friend Shared traceStateDefault As String = "False"

    Friend Shared comPort As String ' Variables to hold the current device configuration
    Friend Shared traceState As Boolean

    Private connectedState As Boolean ' Private variable to hold the connected state
    Private utilities As Util ' Private variable to hold an ASCOM Utilities object
    Private astroUtilities As AstroUtils ' Private variable to hold an AstroUtils object to provide the Range method
    Private TL As TraceLogger ' Private variable to hold the trace logger object (creates a diagnostic log file with information that you specify)

    '
    ' Constructor - Must be public for COM registration!
    '
    Public Sub New()

        ReadProfile() ' Read device configuration from the ASCOM Profile store
        TL = New TraceLogger("", "FilterBuddy")
        TL.Enabled = traceState
        TL.LogMessage("FilterWheel", "Starting initialisation")

        connectedState = False ' Initialise connected to false
        utilities = New Util() ' Initialise util object
        astroUtilities = New AstroUtils 'Initialise new astro utilities object

        'TODO: Implement your additional construction here

        TL.LogMessage("FilterWheel", "Completed initialisation")
    End Sub

    '
    ' PUBLIC COM INTERFACE IFilterWheelV2 IMPLEMENTATION
    '

#Region "Common properties and methods"
    ''' <summary>
    ''' Displays the Setup Dialog form.
    ''' If the user clicks the OK button to dismiss the form, then
    ''' the new settings are saved, otherwise the old values are reloaded.
    ''' THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
    ''' </summary>
    Public Sub SetupDialog() Implements IFilterWheelV2.SetupDialog
        ' consider only showing the setup dialog if not connected
        ' or call a different dialog if connected
        If IsConnected Then
            System.Windows.Forms.MessageBox.Show("Already connected, just press OK")
        End If

        Using F As SetupDialogForm = New SetupDialogForm()
            Dim result As System.Windows.Forms.DialogResult = F.ShowDialog()
            If result = DialogResult.OK Then
                WriteProfile() ' Persist device configuration values to the ASCOM Profile store
            End If
        End Using
    End Sub

    Public ReadOnly Property SupportedActions() As ArrayList Implements IFilterWheelV2.SupportedActions
        Get
            TL.LogMessage("SupportedActions Get", "Returning empty arraylist")
            Return New ArrayList()
        End Get
    End Property

    Public Function Action(ByVal ActionName As String, ByVal ActionParameters As String) As String Implements IFilterWheelV2.Action
        Throw New ActionNotImplementedException("Action " & ActionName & " is not supported by this driver")
    End Function

    Public Sub CommandBlind(ByVal Command As String, Optional ByVal Raw As Boolean = False) Implements IFilterWheelV2.CommandBlind
        CheckConnected("CommandBlind")
        ' Call CommandString and return as soon as it finishes
        Me.CommandString(Command, Raw)
        ' or
        Throw New MethodNotImplementedException("CommandBlind")
    End Sub

    Public Function CommandBool(ByVal Command As String, Optional ByVal Raw As Boolean = False) As Boolean _
        Implements IFilterWheelV2.CommandBool
        CheckConnected("CommandBool")
        Dim ret As String = CommandString(Command, Raw)
        ' TODO decode the return string and return true or false
        ' or
        Throw New MethodNotImplementedException("CommandBool")
    End Function

    Public Function CommandString(ByVal Command As String, Optional ByVal Raw As Boolean = False) As String _
        Implements IFilterWheelV2.CommandString
        CheckConnected("CommandString")
        ' it's a good idea to put all the low level communication with the device here,
        ' then all communication calls this function
        ' you need something to ensure that only one command is in progress at a time
        Throw New MethodNotImplementedException("CommandString")
    End Function

    Public Property Connected() As Boolean Implements IFilterWheelV2.Connected
        Get
            TL.LogMessage("Connected Get", IsConnected.ToString())
            Return IsConnected
        End Get
        Set(value As Boolean)
            TL.LogMessage("Connected Set", value.ToString())
            If value = IsConnected Then
                Return
            End If

            If value Then
                connectedState = True
                TL.LogMessage("Connected Set", "Connecting to port " + comPort)
                ' TODO connect to the device
                If serialCommand("A") = "A" Then
                    connectedState = True   'FilterBuddy now in ASCOM mode
                Else
                    connectedState = False
                    TL.LogMessage("Connected Set", "Disconnecting from port " + comPort)
                    ' TODO disconnect from the device
                    serialCommand("a")      'FilterBuddy not in ASCOM mode
                    ' MsgBox("GoodBye!",, "FilterBuddy")
                End If
            End If
        End Set
    End Property

    Public ReadOnly Property Description As String Implements IFilterWheelV2.Description
        Get
            ' this pattern seems to be needed to allow a public property to return a private field
            Dim d As String = driverDescription
            TL.LogMessage("Description Get", d)
            Return d
        End Get
    End Property

    Public ReadOnly Property DriverInfo As String Implements IFilterWheelV2.DriverInfo
        Get
            Dim m_version As Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
            ' TODO customise this driver description
            Dim s_driverInfo As String = "ASCOM driver for the SBIG CFW-9 filter wheel"
            ' Dim s_driverInfo As String = "Information about the driver itself. Version: " + m_version.Major.ToString() + "." + m_version.Minor.ToString()
            TL.LogMessage("DriverInfo Get", s_driverInfo)
            Return s_driverInfo
        End Get
    End Property

    Public ReadOnly Property DriverVersion() As String Implements IFilterWheelV2.DriverVersion
        Get
            ' Get our own assembly and report its version number
            TL.LogMessage("DriverVersion Get", Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString(2))
            Return Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString(2)
        End Get
    End Property

    Public ReadOnly Property InterfaceVersion() As Short Implements IFilterWheelV2.InterfaceVersion
        Get
            TL.LogMessage("InterfaceVersion Get", "2")
            Return 2
        End Get
    End Property

    Public ReadOnly Property Name As String Implements IFilterWheelV2.Name
        Get
            Dim s_name As String = "FilterBuddy"
            TL.LogMessage("Name Get", s_name)
            Return s_name
        End Get
    End Property

    Public Sub Dispose() Implements IFilterWheelV2.Dispose
        ' Clean up the trace logger and util objects
        TL.Enabled = False
        TL.Dispose()
        TL = Nothing
        utilities.Dispose()
        utilities = Nothing
        astroUtilities.Dispose()
        astroUtilities = Nothing
    End Sub

#End Region

#Region "IFilterWheel Implementation"
    Private fwOffsets As Integer() = New Integer(4) {0, 0, 0, 0, 0} 'class level variable to hold focus offsets
    Private fwNames As String() = New String(4) {"Clear", "Red", "Green", "Blue", "Luminance"} 'class level variable to hold the filter names
    Private fwPosition As Short = 0 'class level variable to retain the current filterwheel position

    Public ReadOnly Property FocusOffsets() As Integer() Implements IFilterWheelV2.FocusOffsets
        Get
            For Each fwOffset As Integer In fwOffsets ' Write filter offsets to the log
                TL.LogMessage("FocusOffsets Get", fwOffset.ToString())
            Next

            Return fwOffsets
        End Get
    End Property

    Public ReadOnly Property Names As String() Implements IFilterWheelV2.Names
        Get
            For Each fwName As String In fwNames ' Write filter names to the log
                TL.LogMessage("Names Get", fwName)
            Next

            Return fwNames
        End Get
    End Property

    Public Property Position() As Short Implements IFilterWheelV2.Position
        Get
            TL.LogMessage("Position Get", fwPosition.ToString())
            fwPosition = CShort(serialCommand("G")) - 1 'If "0" is got then "-1" is returned meaning wheel is still moving, though it maybe in trouble
            Return fwPosition
        End Get
        Set(value As Short)
            TL.LogMessage("Position Set", value.ToString())
            If ((value < 0) Or (value > fwNames.Length - 1)) Then
                TL.LogMessage("Position Set", "Throwing InvalidValueException")
                Throw New InvalidValueException("Position", value.ToString(), "0 to " & (fwNames.Length - 1).ToString())
            End If
            fwPosition = CShort(serialCommand(Str(value + 1)))
        End Set
    End Property

#End Region

#Region "Private properties and methods"
    ' here are some useful properties and methods that can be used as required
    ' to help with

    Private Function serialCommand(cmdStr As String) As String
        Dim retStr As String = ""
        Dim objSerial As New SerialPort

        Try
            With objSerial
                .PortName = FilterWheel.comPort
                .BaudRate = 9600
                .ReadTimeout = 5000
                .WriteTimeout = 5000
                .Open()
                .Write(cmdStr)
            End With
            retStr = objSerial.ReadTo("#")
        Catch ex As Exception
            MsgBox("Can't open " + objSerial.PortName,, "FilterBuddy")
            retStr = "0"
        End Try

        Try
            objSerial.Close()
        Catch ex As Exception
        End Try

        Return retStr
    End Function


#Region "ASCOM Registration"

    Private Shared Sub RegUnregASCOM(ByVal bRegister As Boolean)

        Using P As New Profile() With {.DeviceType = "FilterWheel"}
            If bRegister Then
                P.Register(driverID, driverDescription)
            Else
                P.Unregister(driverID)
            End If
        End Using

    End Sub

    <ComRegisterFunction()>
    Public Shared Sub RegisterASCOM(ByVal T As Type)

        RegUnregASCOM(True)

    End Sub

    <ComUnregisterFunction()>
    Public Shared Sub UnregisterASCOM(ByVal T As Type)

        RegUnregASCOM(False)

    End Sub

#End Region

    ''' <summary>
    ''' Returns true if there is a valid connection to the driver hardware
    ''' </summary>
    Private ReadOnly Property IsConnected As Boolean
        Get
            ' TODO check that the driver hardware connection exists and is connected to the hardware
            '            If serialCommand("C") = "C" Then
            '            connectedState = True
            '            Else
            '            connectedState = False
            '            End If
            Return connectedState
        End Get
    End Property

    ''' <summary>
    ''' Use this function to throw an exception if we aren't connected to the hardware
    ''' </summary>
    ''' <param name="message"></param>
    Private Sub CheckConnected(ByVal message As String)
        If Not IsConnected Then
            Throw New NotConnectedException(message)
        End If
    End Sub

    ''' <summary>
    ''' Read the device configuration from the ASCOM Profile store
    ''' </summary>
    Friend Sub ReadProfile()
        Using driverProfile As New Profile()
            driverProfile.DeviceType = "FilterWheel"
            traceState = Convert.ToBoolean(driverProfile.GetValue(driverID, traceStateProfileName, String.Empty, traceStateDefault))
            comPort = driverProfile.GetValue(driverID, comPortProfileName, String.Empty, comPortDefault)
        End Using
    End Sub

    ''' <summary>
    ''' Write the device configuration to the  ASCOM  Profile store
    ''' </summary>
    Friend Sub WriteProfile()
        Using driverProfile As New Profile()
            driverProfile.DeviceType = "FilterWheel"
            driverProfile.WriteValue(driverID, traceStateProfileName, traceState.ToString())
            driverProfile.WriteValue(driverID, comPortProfileName, comPort.ToString())
        End Using
    End Sub

#End Region

End Class
