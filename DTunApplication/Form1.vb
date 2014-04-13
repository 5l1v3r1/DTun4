Imports System.Text
Imports System.Net
Imports System.Runtime.InteropServices

Public Class Form1
    Public Shared lib1 As Library
    Dim state1 As Integer = -1
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
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        NotifyIcon1.Visible = False
        NotifyIcon1.Dispose()
        My.Settings.tb1 = TextBox1.Text
        My.Settings.tb2 = TextBox2.Text
        My.Settings.ch1 = CheckBox1.Checked
        My.Settings.Save()
        If Not lib1 Is Nothing Then
            lib1.SDTun()
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If TextBox1.Text.Contains("*") Or TextBox2.Text.Contains("*") Or TextBox1.Text.Contains("^") Or TextBox2.Text.Contains("^") Or TextBox1.Text.Contains(":") Or TextBox2.Text.Contains(":") Then
            MsgBox("Names can contain neither *, ^ nor :")
            Exit Sub
        End If
        lib1 = New Library
        Dim thr As New Threading.Thread(AddressOf lib1.Main)
        thr.IsBackground = True
        Dim c(2) As String
        c(0) = TextBox2.Text
        c(1) = TextBox1.Text
        c(2) = CheckBox1.Checked
        thr.Start(c)
        'lib1.Main(TextBox2.Text, TextBox1.Text)

        TextBox1.Enabled = False
        TextBox2.Enabled = False
        Button1.Enabled = False
        CheckBox1.Enabled = False
        Label5.Text = "Communicating..."
        Timer1.Start()

    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If lib1.updateusers Then
            'Label5.Text = "Establishing connection..."
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
            ElseIf state1 = 7 Then
                Label5.Text = "Error"
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
            Application.Restart()
            'lib1.SDTun()
            'Button1_Click(Nothing, Nothing)
            'Timer2.Enabled = False
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

    Private Sub ListBox1_Click(sender As Object, e As EventArgs) Handles ListBox1.Click
        If Not ListBox1.SelectedItem Is Nothing Then
            'ToolTip1.SetToolTip(Me.ListBox1, "Click to copy IP to clipboard")
            Clipboard.SetText(ListBox1.SelectedItem.ToString.Split(":")(1))
            ListBox1.ClearSelected()
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
            Label5.Text = "Network error. Reconnecting in 45 seconds..."
            ProgressBar1.Value = 100
            Modify.SetState(ProgressBar1, 2)
            Timer3.Start()
        End If
    End Sub

    Private Sub Timer3_Tick(sender As Object, e As EventArgs) Handles Timer3.Tick
        If ProgressBar1.Value > 4 Then
            ProgressBar1.Value -= 2
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
        ListBox1.SelectedIndex = ListBox1.IndexFromPoint(e.X, e.Y)
        If Not ListBox1.SelectedItem Is Nothing Then
            If e.Button = Windows.Forms.MouseButtons.Right Then
                client.Items("ping").Text = "ping " & ListBox1.SelectedItem.ToString.Split(":")(1)
                client.Show(MousePosition)
            End If
        End If
    End Sub

    Private Sub client_ItemClicked(sender As Object, e As ToolStripItemClickedEventArgs) Handles client.ItemClicked
        If e.ClickedItem.Text.StartsWith("ping") Then
            Shell(e.ClickedItem.Text & " -t", AppWinStyle.NormalFocus, False)
        Else
            If Not ListBox1.SelectedItem Is Nothing Then
                'ToolTip1.SetToolTip(Me.ListBox1, "Click to copy IP to clipboard")
                Clipboard.SetText(ListBox1.SelectedItem.ToString.Split(":")(1))
                ListBox1.ClearSelected()
            End If
        End If

    End Sub
End Class






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
