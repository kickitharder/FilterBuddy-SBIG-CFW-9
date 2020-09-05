Imports System.Windows.Forms
Imports System.IO.Ports
Imports System.Runtime.InteropServices
Imports ASCOM.Utilities
Imports ASCOM.FilterBuddy
Imports System.IO
Imports System.Threading

<ComVisible(False)>
Public Class SetupDialogForm
    Private Sub OK_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OK_Button.Click ' OK button event handler
        ' Persist new values of user settings to the ASCOM profile
        FilterWheel.comPort = ComboBoxComPort.SelectedItem ' Update the state variables with results from the dialogue
        FilterWheel.traceState = chkTrace.Checked
        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()
    End Sub

    Private Sub Cancel_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Cancel_Button.Click 'Cancel button event handler
        Me.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub ShowAscomWebPage(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PictureBox1.DoubleClick, PictureBox1.Click
        ' Click on ASCOM logo event handler
        Try
            System.Diagnostics.Process.Start("http://ascom-standards.org/")
        Catch noBrowser As System.ComponentModel.Win32Exception
            If noBrowser.ErrorCode = -2147467259 Then
                MessageBox.Show(noBrowser.Message)
            End If
        Catch other As System.Exception
            MessageBox.Show(other.Message)
        End Try
    End Sub

    Private Sub CMHASDLogo_Click(sender As Object, e As EventArgs) Handles CMHASDLogo.Click
        ' Click on CMHASD logo event handler
        Try
            System.Diagnostics.Process.Start("https://crayfordmanorastro.com/")
        Catch noBrowser As System.ComponentModel.Win32Exception
            If noBrowser.ErrorCode = -2147467259 Then
                MessageBox.Show(noBrowser.Message)
            End If
        Catch other As System.Exception
            MessageBox.Show(other.Message)
        End Try
    End Sub

    Private Sub SBIGLogo_Click(sender As Object, e As EventArgs) Handles SBIGLogo.Click
        ' Click on SBIG logo event handler
        Try
            System.Diagnostics.Process.Start("https://diffractionlimited.com/wp-content/uploads/2016/03/CFW9_manual.pdf")
        Catch noBrowser As System.ComponentModel.Win32Exception
            If noBrowser.ErrorCode = -2147467259 Then
                MessageBox.Show(noBrowser.Message)
            End If
        Catch other As System.Exception
            MessageBox.Show(other.Message)
        End Try
    End Sub

    End Sub

    Private Sub SetupDialogForm_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load ' Form load event handler
        ' Retrieve current values of user settings from the ASCOM Profile
        InitUI()
    End Sub

    Private Sub InitUI()
        chkTrace.Checked = FilterWheel.traceState
        ' set the list of com ports to those that are currently available
        ComboBoxComPort.Items.Clear()
        ComboBoxComPort.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames())       ' use System.IO because it's static
        ' select the current port if possible
        If ComboBoxComPort.Items.Contains(FilterWheel.comPort) Then
            ComboBoxComPort.SelectedItem = FilterWheel.comPort
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles btnCheck.Click
        lstDetails.Items.Clear()
        lstDetails.Visible = True
        If serialCmd("a") = "a" Then
            lstDetails.Items.Add("FilterBuddy controller responding")
            lstDetails.Items.Add(serialCmd("N") + "  " +
                                serialCmd("V") + "  by " +
                                serialCmd("B"))
            lstDetails.Items.Add(serialCmd("E"))
        Else
            lstDetails.Items.Add("Filter wheel not responding - check connection")
        End If
    End Sub

    Private Function serialCmd(cmdStr As String) As String
        Dim retStr As String = ""
        Dim objSerial As New SerialPort

        Try
            With objSerial
                .PortName = ComboBoxComPort.SelectedItem.ToString
                .BaudRate = 9600
                .ReadTimeout = 1000
                .WriteTimeout = 1000
                .Open()
                .Write(cmdStr)
            End With
            retStr = objSerial.ReadTo("#")
        Catch ex As Exception
            'MsgBox("FilterBuddy can't open " + objSerial.PortName,, "FilterBuddy")
            retStr = "0"
        End Try

        Try
            objSerial.Close()
        Catch ex As Exception
        End Try

        Return retStr
    End Function
End Class
