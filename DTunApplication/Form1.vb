Imports System.Text
Imports System.Net
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Net.Sockets

Public Class Form1
    Public Shared lib1 As Library
    Dim state1 As Integer = -1
    Public Shared status As New Dictionary(Of String, ClientInfo)
    Dim thr As New Threading.Thread(AddressOf connectionTest)
    Dim restart As Boolean = False
    Private Sub Form1_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown

        Dim fvi As FileVersionInfo = FileVersionInfo.GetVersionInfo(My.Application.Info.DirectoryPath & "\DTun4ClientLibrary.dll")
        Label7.Text = fvi.FileMajorPart & "." & fvi.FileMinorPart
        TextBox1.Text = My.Settings.tb1
        TextBox2.Text = My.Settings.tb2
        CheckBox1.Checked = My.Settings.ch1

        If TextBox2.Text = "" Then
            TextBox2.Text = Environment.MachineName
        End If
#If Not Debug Then
        If Not (Command$().ToLower.Contains("-updated")) Then
            MessageBox.Show("Start application using updater")
            Environment.Exit(1)
            Exit Sub
        End If
#End If
        WindowState = FormWindowState.Normal
        Form1.CheckForIllegalCrossThreadCalls = False
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        NotifyIcon1.Visible = False
        My.Settings.tb1 = TextBox1.Text
        My.Settings.tb2 = TextBox2.Text
        My.Settings.ch1 = CheckBox1.Checked
        My.Settings.Save()
        Try
            thr.Abort()
        Catch
        End Try
        If Not restart Then
            Environment.Exit(0)
        Else
            Try
                lib1.SDTun()
            Catch
            End Try
        End If

        'If Not lib1 Is Nothing Then
        'lib1.SDTun()
        'End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If Button1.Text = "Connect" Then


            If TextBox1.Text.Contains("*") Or TextBox2.Text.Contains("*") Or TextBox1.Text.Contains("^") Or TextBox2.Text.Contains("^") Or TextBox1.Text.Contains(":") Or TextBox2.Text.Contains(":") Then
                MsgBox("Names can contain neither *, ^ nor :")
                Exit Sub
            End If
            lib1 = New Library
            Dim thr As New Threading.Thread(AddressOf lib1.Main)
            thr.IsBackground = True
            Dim c(5) As Object
            c(0) = TextBox2.Text
            c(1) = TextBox1.Text
            c(2) = CheckBox1.Checked
            c(3) = False
            c(4) = Me.NotifyIcon1
            c(5) = Me.Button2

            If (Command$().ToLower.Contains("-debug")) Then
                c(3) = True
                Label10.Text = "DEBUG MODE"
            End If

            thr.Start(c)
            'lib1.Main(TextBox2.Text, TextBox1.Text)

            TextBox1.Enabled = False
            TextBox2.Enabled = False
            'Button1.Enabled = False
            CheckBox1.Enabled = False
            Label5.Text = "Communicating..."
            Timer1.Start()
            Button1.Text = "Disconnect"
        ElseIf Button1.Text = "Disconnect" Then
            restart = True
            Application.Restart()
        End If
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If lib1.updateusers Then
            lib1.updateusers = False
            For i As Integer = 0 To lib1.users.Count - 2
                ListBox1.Items.Add(lib1.users(i))
            Next
            Label9.Text = lib1.IP
        End If
        If state1 <> lib1.state Then
            state1 = lib1.state
            If state1 < 7 Then
                ProgressBar1.Value = Int(state1 * 16.5)
            End If
            If state1 = 0 Then
                Label5.Text = "Downloading public RSA key..."
            ElseIf state1 = 1 Then
                Label5.Text = "Received IP and public key"
            ElseIf state1 = 2 Then
                Label5.Text = "Generated AES key"
            ElseIf state1 = 3 Then
                Label5.Text = "Connecting to DTun4 Server"
            ElseIf state1 = 4 Then
                Label5.Text = "Scanning for network devices..."
            ElseIf state1 = 5 Then
                Label5.Text = "Device found. Connecting..."
            ElseIf state1 = 6 Then
                Label5.Text = "Connected"
                thr.IsBackground = True
                thr.Start()
            ElseIf state1 = 7 Then
                Label5.Text = "Error"
                ProgressBar1.Value = 50
                Modify.SetState(ProgressBar1, 2)
            End If
        End If
        If lib1.conn Then
            ProgressBar1.Value = 100
            Label5.Text = "Connected."
            Timer2.Enabled = True
            Timer1.Enabled = False
            Button2.Enabled = True
        End If
    End Sub

    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        If Not lib1.conn Then
            Label5.Text = "Connection lost. Reconnecting..."
            System.IO.File.Create(".\crash.dtun4")
            restart = True
            Application.Restart()
        End If

        If lib1.updateusers Then
            lib1.updateusers = False
            ListBox1.Items.Clear()
            For i As Integer = 0 To lib1.users.Count - 2
                ListBox1.Items.Add(lib1.users(i))
            Next
        Else
            Dim source As IPEndPoint = New IPEndPoint(IPAddress.Any, 4955)
            lib1.listener.Send(Encoding.Default.GetBytes("INFOPLS"), Encoding.Default.GetByteCount("INFOPLS"), lib1.groupEP)
        End If
    End Sub


    Private Sub NotifyIcon1_Click(sender As Object, e As EventArgs) Handles NotifyIcon1.Click
        Show()
        WindowState = FormWindowState.Normal
    End Sub

    Private Sub Form1_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        If FormWindowState.Minimized = WindowState Then
            Hide()
        End If
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Label9.Text = Library.getIP()
        If System.IO.File.Exists(".\crash.dtun4") Then
            System.IO.File.Delete(".\crash.dtun4")
            TextBox1.Enabled = False
            TextBox2.Enabled = False
            Button1.Enabled = False
            CheckBox1.Enabled = False
            Label5.Text = "Network error. Reconnecting in 10 seconds..."
            ProgressBar1.Value = 100
            Modify.SetState(ProgressBar1, 2)
            Timer3.Start()
        End If
    End Sub

    Private Sub Timer3_Tick(sender As Object, e As EventArgs) Handles Timer3.Tick
        If ProgressBar1.Value > 10 Then
            ProgressBar1.Value -= 10
        Else
            Modify.SetState(ProgressBar1, 1)
            ProgressBar1.Value = 0
            Timer3.Enabled = False
            Dim ch As Boolean = False
            If CheckBox1.Checked = False Then
                CheckBox1.Checked = True
                ch = True
            End If
            Button1_Click(Nothing, Nothing)
            If ch Then
                CheckBox1.Checked = False
            End If
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim chatwindow As New Chat
        chatwindow.Show()

    End Sub

    Private Sub ListBox1_MouseDown(sender As Object, e As MouseEventArgs) Handles ListBox1.MouseDown
        Try
            ListBox1.SelectedIndex = ListBox1.IndexFromPoint(e.X, e.Y)
        Catch
        End Try
        If Not ListBox1.SelectedItem Is Nothing Then
            If e.Button = Windows.Forms.MouseButtons.Right Then
                client.Items("ping").Text = "ping " & ListBox1.SelectedItem.ToString.Split(":")(1)
                client.Show(MousePosition)
            End If
        End If
        ListBox1.ClearSelected()
    End Sub

    Private Sub client_ItemClicked(sender As Object, e As ToolStripItemClickedEventArgs) Handles client.ItemClicked
        If Not ListBox1.SelectedItem Is Nothing Then
            If e.ClickedItem.Text.StartsWith("ping") Then
                Shell(e.ClickedItem.Text & " -t", AppWinStyle.NormalFocus, False)
            Else
                Clipboard.SetText(ListBox1.SelectedItem.ToString.Split(":")(1))
                ListBox1.ClearSelected()
            End If
        End If

    End Sub

    Private Sub connectionTest()
        Dim CMlistener As New UdpClient(4957)
        CMlistener.Client.ReceiveTimeout = 1100
        Dim source As IPEndPoint
        Threading.Thread.Sleep(5000)
        While True
            Try
                Dim p As New System.Net.NetworkInformation.Ping


                Dim newstatus As New Dictionary(Of String, ClientInfo)
                For i As Integer = 0 To ListBox1.Items.Count - 1
                    If Not newstatus.ContainsKey(ListBox1.Items(i).ToString) Then
                        Dim pingreply As Net.NetworkInformation.PingReply = p.Send(ListBox1.Items(i).ToString.Split(":")(1), 1100)
                        If pingreply.Status = NetworkInformation.IPStatus.Success Then
                            Dim cl As New ClientInfo(2, pingreply.RoundtripTime)
                            newstatus(ListBox1.Items(i)) = cl
                        Else
                            lib1.SendControlMessageReq(ListBox1.Items(i).ToString.Split(":")(1))
                            Try
                                If Encoding.Default.GetString(CMlistener.Receive(source)) = "DTun4CM-REP" Then
                                    Dim cl1 As New ClientInfo(1, 0)
                                    newstatus(ListBox1.Items(i)) = cl1
                                    Continue For
                                End If
                            Catch e As Exception

                            End Try
                            Dim cl2 As New ClientInfo(0, 0)
                            newstatus(ListBox1.Items(i)) = cl2
                        End If
                    End If
                Next
                status = New Dictionary(Of String, ClientInfo)(newstatus)
                ListBox1.Refresh()
            Catch
            End Try
            Threading.Thread.Sleep(5000)
        End While
    End Sub
    Structure ClientInfo
        Dim status As Integer
        Dim pingtime As Integer
        Sub New(_1 As Integer, _2 As Integer)
            status = _1
            pingtime = _2
        End Sub
    End Structure

    Private Sub Label7_Click(sender As Object, e As EventArgs) Handles Label7.Click

        If MessageBox.Show("Do you want to redownload the newest version?", "Selfrepair", MessageBoxButtons.YesNo) = Windows.Forms.DialogResult.Yes Then
            Dim cl As New WebClient()
            cl.DownloadFile(New Uri("http://dtun4.disahome.me/dl/DTun4Launcher.exe"), "DTun4Launcher.exe")
            Shell(".\DTun4Launcher.exe -sr")
            Environment.Exit(0)
        End If
    End Sub
End Class



Namespace Toolset.Controls
    Public Class CustomDrawListBox
        Inherits ListBox
        Dim _1 As Icon = My.Resources._1
        Dim _0 As Icon = My.Resources._0
        Dim _2 As Icon = My.Resources._2
        Dim _3 As Icon = My.Resources._3
        Public Sub New()
            Me.DrawMode = Windows.Forms.DrawMode.OwnerDrawFixed

            Me.ItemHeight = 16
        End Sub

        Protected Overrides Sub OnDrawItem(e As DrawItemEventArgs)
            e.DrawBackground()
            If e.Index >= Me.Items.Count OrElse e.Index <= -1 Then
                Return
            End If

            Dim item As Object = Me.Items(e.Index)
            If item Is Nothing Then
                Return
            End If



            Dim text As String = item.ToString()
            Dim stringSize As SizeF = e.Graphics.MeasureString(text, Me.Font)
            If DTun4.Form1.status.ContainsKey(text) Then
                If DTun4.Form1.status(text).status = 0 Then
                    e.Graphics.DrawIcon(_0, 0, e.Bounds.Y)
                    e.Graphics.DrawString(text, Me.Font, New SolidBrush(Color.Red), New PointF(20, e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2))
                    e.Graphics.DrawString("999", Me.Font, Brushes.Red, New PointF(e.Bounds.Right - 25, e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2))
                ElseIf DTun4.Form1.status(text).status = 1 Then
                    e.Graphics.DrawIcon(_1, 0, e.Bounds.Y)
                    e.Graphics.DrawString(text, Me.Font, New SolidBrush(Color.YellowGreen), New PointF(20, e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2))
                    e.Graphics.DrawString("N/A", Me.Font, Brushes.YellowGreen, New PointF(e.Bounds.Right - 25, e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2))
                ElseIf DTun4.Form1.status(text).status = 2 Then
                    e.Graphics.DrawIcon(_2, 0, e.Bounds.Y)
                    e.Graphics.DrawString(text, Me.Font, New SolidBrush(Color.Green), New PointF(20, e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2))
                    If DTun4.Form1.status(text).pingtime < 100 Then
                        e.Graphics.DrawString(DTun4.Form1.status(text).pingtime, Me.Font, Brushes.Green, New PointF(e.Bounds.Right - 25, e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2))
                    ElseIf DTun4.Form1.status(text).pingtime < 300 Then
                        e.Graphics.DrawString(DTun4.Form1.status(text).pingtime, Me.Font, Brushes.YellowGreen, New PointF(e.Bounds.Right - 25, e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2))
                    Else
                        e.Graphics.DrawString(DTun4.Form1.status(text).pingtime, Me.Font, Brushes.Red, New PointF(e.Bounds.Right - 25, e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2))
                    End If

                End If
            Else
                e.Graphics.DrawIcon(_3, 0, e.Bounds.Y)
                e.Graphics.DrawString(text, Me.Font, New SolidBrush(Color.Blue), New PointF(20, e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2))
            End If
        End Sub
    End Class
End Namespace






Module Modify
    Private Declare Function SendMessage Lib "User32" Alias "SendMessageA" (ByVal hWnd As Long, ByVal wMsg As Long, ByVal wParam As Long, lParam As Long) As Long

    <DllImport("user32.dll", CharSet:=CharSet.Auto, SetLastError:=False)> _
    Private Function SendMessage(hWnd As IntPtr, Msg As UInteger, w As IntPtr, l As IntPtr) As IntPtr
    End Function

    <System.Runtime.CompilerServices.Extension()> _
    Public Sub SetState(pBar As ProgressBar, state As Integer)

        '-- Convert state as integer to type IntPtr
        Dim s As IntPtr
        Dim y As Integer = state
        s = IntPtr.op_Explicit(y)

        '-- Modify bar color
        SendMessage(pBar.Handle, 1040, s, IntPtr.Zero)

    End Sub
End Module
