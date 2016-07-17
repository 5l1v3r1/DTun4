Imports System.Text
Imports System.Net
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Net.Sockets
Imports MahApps.Metro.Controls
Imports MahApps.Metro.Controls.Dialogs
Imports System.Windows.Threading

Partial Class MainWindow

    Public Shared lib1 As Library
    Dim state1 As Integer = -1
    Public Shared status As New Dictionary(Of String, ClientInfo)
    Dim thr As New System.Threading.Thread(AddressOf connectionTest)
    Dim restart As Boolean = False
    Dim Timer1 As New Windows.Forms.Timer
    Dim Timer2 As New Windows.Forms.Timer
    Dim Timer3 As New Windows.Forms.Timer
    Dim NotifyIcon1 As New Windows.Forms.NotifyIcon
    Dim client As New Forms.ContextMenuStrip
	Dim menufor As String

	''' <summary>
	''' Load event, set up things
	''' </summary>
	Private Sub MetroWindow_Loaded(sender As Object, e As RoutedEventArgs)
		Label9.Content = Library.getIP()
		If System.IO.File.Exists(".\crash.dtun4") Then
			System.IO.File.Delete(".\crash.dtun4")
			TextBox1.IsEnabled = False
			TextBox2.IsEnabled = False
			Button1.IsEnabled = False
			CheckBox1.IsEnabled = False
			Label5.Content = "Network error. Reconnecting in 10 seconds..."
			ProgressBar1.Value = 100

			Dim linear As LinearGradientBrush = New LinearGradientBrush()
			linear.StartPoint = New System.Windows.Point(0, 0)
			linear.EndPoint = New System.Windows.Point(1, 1)
			linear.SpreadMethod = GradientSpreadMethod.Pad
			linear.ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation
			linear.GradientStops.Add(New GradientStop(System.Windows.Media.Color.FromArgb(255, 205, 0, 0), 0))
			linear.GradientStops.Add(New GradientStop(System.Windows.Media.Color.FromArgb(255, 160, 0, 0), 1))

			ProgressBar1.Foreground = linear

			Timer3.Start()
		End If


		Dim fvi As FileVersionInfo = FileVersionInfo.GetVersionInfo(My.Application.Info.DirectoryPath & "\DTun4ClientLibrary.dll")
		Label7.Content = fvi.FileMajorPart & "." & fvi.FileMinorPart
		TextBox1.Text = My.Settings.tb1
		TextBox2.Text = My.Settings.tb2
		CheckBox1.IsChecked = My.Settings.ch1

		If TextBox2.Text = "" Then
			TextBox2.Text = Environment.MachineName
		End If
#If Not Debug Then
		If Not (Command$().ToLower.Contains("-updated")) Then
			'MessageBox.Show("Start application using updater")
			'Await ShowMessageAsync("Error", "Start application using updater")
			Dim thr5 As New System.Threading.Thread(AddressOf error1)
			thr5.IsBackground = True
			thr5.Start(0)
			'Environment.Exit(1)
			Exit Sub
		End If
#End If
		WindowState = WindowState.Normal


		Dim solidbrush As New SolidColorBrush()
		solidbrush.Color = System.Windows.Media.Color.FromArgb(255, 0, 128, 255)

		ProgressBar1.Foreground = solidbrush

		Timer1.Interval = 35
		Timer2.Interval = 2000
		Timer3.Interval = 1000
		AddHandler Timer1.Tick, AddressOf Timer1_Tick
		AddHandler Timer2.Tick, AddressOf Timer2_Tick
		AddHandler Timer3.Tick, AddressOf Timer3_Tick
		NotifyIcon1.Text = "DTun4"
		NotifyIcon1.Icon = My.Resources._11
		NotifyIcon1.Visible = True
		AddHandler NotifyIcon1.MouseClick, AddressOf NotifyIcon1_Click

		Dim menuitem As New Forms.ToolStripMenuItem
		menuitem.Name = "Ping"
		menuitem.Text = "Ping"

		Dim menuitem2 As New Forms.ToolStripMenuItem
		menuitem2.Name = "Copy"
		menuitem2.Text = "Copy IP"

		Dim menuitem3 As New Forms.ToolStripMenuItem
		menuitem3.Name = "CS"
		menuitem3.Text = "Request Client-Server model"

		client.Items.Add(menuitem)
		client.Items.Add(menuitem2)
		client.Items.Add(menuitem3)

		AddHandler client.ItemClicked, AddressOf client_ItemClicked
		AddHandler client.Closed, AddressOf client_Closed

		ListBox1.AddHandler(UIElement.MouseDownEvent, New MouseButtonEventHandler(AddressOf ListBox1_MouseDown), True)


		If (Command$().ToLower.Contains("-conn ")) Then
			Dim network As String = Command$().Remove(0, Command$().IndexOf("-conn ") + 15)
			network = network.Remove(network.Length() - 2)
			TextBox1.Text = network
			Button1_MouseDown(Nothing, Nothing)
		End If
	End Sub

	''' <summary>
	''' Helper for MahApps asynch dialogs
	''' </summary>
	Sub error1(ByVal type As Integer)
		Dim task As task
		Dim t As String
		If type = 0 Then
			t = "Start application using updater"
		ElseIf type = 1 Then
			t = "You cannot request Client-Server model when all clients are active"
		ElseIf type = 2 Then
			t = "You must be the leader to request Client-Server model for the network"
		End If
		Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (Sub() task = ShowMessageAsync("Error", t)))
		'task.Start()
		task.Wait()
		If type = 0 Then
			Environment.Exit(1)
		End If
	End Sub

	''' <summary>
	''' Timer used during the connection
	''' </summary>
	Private Sub Timer1_Tick(sender As Object, e As EventArgs)
		If lib1.updateusers Then
			lib1.updateusers = False
			Dim clc As New ClientCollection
			For i As Integer = 0 To lib1.users.Count - 2
				Dim cl As ClientListBox
				If status.ContainsKey(lib1.users(i)) Then
					cl = New ClientListBox(lib1.users(i), status(lib1.users(i)).pingtime, status(lib1.users(i)).status)
				Else
					cl = New ClientListBox(lib1.users(i), "0", 3)
				End If

				clc.Add(cl)
			Next
			ListBox1.DataContext = clc
			Label9.Content = lib1.IP
		End If
		If state1 <> lib1.state Then
			state1 = lib1.state
			If state1 < 7 Then
				ProgressBar1.Value = Int(state1 * 16.5)
			End If
			If state1 = 0 Then
				Label5.Content = "Downloading public RSA key..."
			ElseIf state1 = 1 Then
				Label5.Content = "Generating AES key..."
			ElseIf state1 = 2 Then
				Label5.Content = "Preparing..."
			ElseIf state1 = 3 Then
				Label5.Content = "Connecting to DTun4 Server"
			ElseIf state1 = 4 Then
				Label5.Content = "Scanning for network devices..."
			ElseIf state1 = 5 Then
				Label5.Content = "Device found. Connecting..."
			ElseIf state1 = 6 Then
				Label5.Content = "Connected"
				thr.IsBackground = True
				thr.Start()
			ElseIf state1 = 7 Then
				Label5.Content = "Error"
				ProgressBar1.Value = 50
				Dim linear As LinearGradientBrush = New LinearGradientBrush()
				linear.StartPoint = New System.Windows.Point(0, 0)
				linear.EndPoint = New System.Windows.Point(1, 1)
				linear.SpreadMethod = GradientSpreadMethod.Pad
				linear.ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation
				linear.GradientStops.Add(New GradientStop(System.Windows.Media.Color.FromArgb(255, 205, 0, 0), 0))
				linear.GradientStops.Add(New GradientStop(System.Windows.Media.Color.FromArgb(255, 160, 0, 0), 1))

				ProgressBar1.Foreground = linear
			End If
		End If
		If lib1.conn Then
			ProgressBar1.Value = 100
			Label5.Content = "Connected."
			Timer2.Enabled = True
			Timer1.Enabled = False
			Button2.IsEnabled = True
			mutebutton.IsEnabled = True
		End If
	End Sub

	''' <summary>
	''' Connected users listbox updater Timer
	''' </summary>
	Private Sub Timer2_Tick(sender As Object, e As EventArgs)
		If Not lib1.conn Then
			Label5.Content = "Connection lost. Reconnecting..."
			System.IO.File.Create(".\crash.dtun4")
			restart = True
			Windows.Forms.Application.Restart()
			Application.Current.Shutdown()
		End If
		If state1 <> lib1.state Then
			state1 = lib1.state
			If state1 = 6 Then
				ProgressBar1.Value = 100
				Label5.Content = "Connected"
			End If
		End If
		If lib1.updateusers Then
			lib1.updateusers = False
			Dim clc As New ClientCollection
			For i As Integer = 0 To lib1.users.Count - 2
				Dim cl As ClientListBox
				If status.ContainsKey(lib1.users(i)) Then
					cl = New ClientListBox(lib1.users(i), status(lib1.users(i)).pingtime, status(lib1.users(i)).status)
				Else
					cl = New ClientListBox(lib1.users(i), "0", 3)
				End If
				clc.Add(cl)
			Next
			If lib1.leading Or lib1.connected Then
				clc(0).Leader = True
			Else
				clc(0).Leader = False
			End If
			ListBox1.DataContext = clc
		End If
		Dim source As IPEndPoint = New IPEndPoint(IPAddress.Any, 4955)
		lib1.listener.Send(Encoding.Default.GetBytes("INFOPLS"), Encoding.Default.GetByteCount("INFOPLS"), lib1.groupEP)
	End Sub

	''' <summary>
	''' Progressbar timer
	''' </summary>
	Private Sub Timer3_Tick(sender As Object, e As EventArgs)
		If ProgressBar1.Value > 10 Then
			ProgressBar1.Value -= 10
		Else
			Dim solidbrush As New SolidColorBrush()
			solidbrush.Color = System.Windows.Media.Color.FromArgb(255, 0, 128, 255)

			ProgressBar1.Foreground = solidbrush

			ProgressBar1.Value = 0
			Timer3.Enabled = False
			Dim ch As Boolean = False
			If CheckBox1.IsChecked = False Then
				CheckBox1.IsChecked = True
				ch = True
			End If
			Button1.IsEnabled = True
			Button1_MouseDown(Nothing, Nothing)
			If ch Then
				CheckBox1.IsChecked = False
			End If
		End If
	End Sub

	Private Sub NotifyIcon1_Click(sender As Object, e As EventArgs)
        Show()
        WindowState = FormWindowState.Normal
    End Sub

	''' <summary>
	''' When ping fails, custom protocol is used
	''' If successful yellow dot appears near client instead of green
	''' It stills means that communication is okay(ping protocol could be blocked)
	''' </summary>
	Private Sub connectionTest()
		Dim CMlistener As New UdpClient(4957)
		CMlistener.Client.ReceiveTimeout = 1100
		Dim source As IPEndPoint
		System.Threading.Thread.Sleep(5000)
		While True
			Try
				Dim p As New System.Net.NetworkInformation.Ping


				Dim newstatus As New Dictionary(Of String, ClientInfo)
				For i As Integer = 0 To ListBox1.Items.Count - 1
					If Not newstatus.ContainsKey(ListBox1.Items(i).ToString) Then
						Dim pingreply As Net.NetworkInformation.PingReply = p.Send(ListBox1.Items(i).ToString.Split(":")(1), 1100)
						If pingreply.Status = NetworkInformation.IPStatus.Success Then
							Dim cl As New ClientInfo(2, pingreply.RoundtripTime)
							newstatus(ListBox1.Items(i).ToString) = cl
						Else
							lib1.SendControlMessageReq(ListBox1.Items(i).ToString.Split(":")(1))
							Try
								If Encoding.Default.GetString(CMlistener.Receive(source)) = "DTun4CM-REP" Then
									Dim cl1 As New ClientInfo(1, 0)
									newstatus(ListBox1.Items(i).ToString) = cl1
									Continue For
								End If
							Catch e As Exception

							End Try
							Dim cl2 As New ClientInfo(0, 0)
							newstatus(ListBox1.Items(i).ToString) = cl2
						End If
					End If
				Next
				status = New Dictionary(Of String, ClientInfo)(newstatus)
				lib1.updateusers = True
				'ListBox1.Items.Refresh()
			Catch
			End Try
			System.Threading.Thread.Sleep(5000)
		End While
	End Sub

	''' <summary>
	''' Information for client that happens to be a server
	''' </summary>
	Structure ClientInfo
        Dim status As Integer
        Dim pingtime As Integer
        Sub New(_1 As Integer, _2 As Integer)
            status = _1
            pingtime = _2
        End Sub
    End Structure

	''' <summary>
	''' Self-repair function after clicking on version number
	''' </summary>
	Private Sub Label7_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles Label7.PreviewMouseDown

        Dim thr5 As New System.Threading.Thread(AddressOf question1)
        thr5.IsBackground = True
        thr5.Start()
        Exit Sub
    End Sub

	''' <summary>
	''' Helper for asynch MahApps Dialog
	''' </summary>
	Sub question1()
		Dim task As Task(Of MessageDialogResult)
		Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (Sub() task = ShowMessageAsync("Selfrepair", "Do you want to redownload the newest version?", MessageDialogStyle.AffirmativeAndNegative)))
		task.Wait()
		If task.Result = MessageDialogResult.Affirmative Then
			Dim cl As New WebClient()
			cl.DownloadFile(New Uri("http://dtun4.disahome.me/dl/DTun4Launcher.exe"), "DTun4Launcher.exe")
			Try
				lib1.SDTun()
				thr.Abort()
			Catch
			End Try
			cl.Dispose()
			System.Threading.Thread.Sleep(200)
			Microsoft.VisualBasic.Shell(".\DTun4Launcher.exe -sr")
			Environment.Exit(0)
		End If

	End Sub

	''' <summary>
	''' Force quit
	''' </summary>
	Private Sub MetroWindow_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs)
        If Not lib1 Is Nothing Then
            Try
                System.IO.File.WriteAllText(lib1.hpath, lib1.hostsold)
            Catch
            End Try
        End If


        NotifyIcon1.Visible = False
        My.Settings.tb1 = TextBox1.Text
        My.Settings.tb2 = TextBox2.Text
        My.Settings.ch1 = CheckBox1.IsChecked
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
    End Sub

	''' <summary>
	''' Hides
	''' </summary>
	Private Sub MetroWindow_SizeChanged(sender As Object, e As SizeChangedEventArgs)
        If FormWindowState.Minimized = WindowState Then
            Hide()
        End If
    End Sub

	''' <summary>
	''' Starts chat window
	''' </summary>
	Private Sub Button2_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles Button2.PreviewMouseDown
		Dim chatwindow As New Chat
		chatwindow.Show()
		Button2.Content = "Chat"
	End Sub

	''' <summary>
	''' Context menu control
	''' </summary>
	Private Sub ListBox1_MouseDown(sender As Object, e As MouseButtonEventArgs)

		If Not ListBox1.SelectedItem Is Nothing Then
			If e.RightButton = MouseButtonState.Pressed Then
				client.Items("ping").Text = "ping " & ListBox1.SelectedItem.ToString.Split(":")(1)
				client.Show(Control.MousePosition)
				menufor = ListBox1.SelectedItem.ToString.Split(":")(1)
			Else
				ListBox1.UnselectAll()
			End If
		End If
	End Sub

	''' <summary>
	''' Context menu actions
	''' </summary>
	Private Sub client_ItemClicked(sender As Object, e As ToolStripItemClickedEventArgs)
		If e.ClickedItem.Text.StartsWith("ping") Then
			Microsoft.VisualBasic.Shell(e.ClickedItem.Text & " -t", AppWinStyle.NormalFocus, False)
		ElseIf e.ClickedItem.Text.StartsWith("Req") Then
			If lib1.leading Then
				Dim y As Boolean = False
				For i As Integer = 0 To status.Count() - 1
					If status.ElementAt(i).Value.status = 0 Then
						y = True
						Exit For
					End If
				Next
				If y Then
					lib1.SendCSReq() 'is a leader, can request Client-Server model
				Else
					Dim thr5 As New System.Threading.Thread(AddressOf error1) 'Everyone is connected, no need
					thr5.IsBackground = True
					thr5.Start(1)
				End If
			Else
				Dim thr5 As New System.Threading.Thread(AddressOf error1) 'Not a leader
				thr5.IsBackground = True
				thr5.Start(2)
			End If
		Else
			Clipboard.SetText(menufor)
		End If
	End Sub

	Private Sub client_Closed(sender As Object, e As ToolStripDropDownClosedEventArgs)
		ListBox1.UnselectAll()
	End Sub

	''' <summary>
	''' Connects to network
	''' </summary>
	Private Sub Button1_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Button1.PreviewMouseDown
		If Button1.Content = "Connect" Then


			If TextBox1.Text.Contains("*") Or TextBox2.Text.Contains("*") Or TextBox1.Text.Contains("^") Or TextBox2.Text.Contains("^") Or TextBox1.Text.Contains(":") Or TextBox2.Text.Contains(":") Or TextBox1.Text.Contains("|") Or TextBox2.Text.Contains("|") Then
				ShowMessageAsync("Error", "Names cannot contain: '*', '^', ':', '|'")
				Exit Sub
			End If
			lib1 = New Library
			Dim thr As New System.Threading.Thread(AddressOf lib1.Main)
			thr.IsBackground = True
			Dim c(6) As Object 'table of params
			c(0) = TextBox2.Text
			c(1) = TextBox1.Text
			c(2) = CheckBox1.IsChecked
			c(3) = False
			c(4) = Me.NotifyIcon1
			Dim chats As Library.Chatget = AddressOf chatset
			c(5) = chats
			Dim reconnba As Library.ReconnBar = AddressOf reconnbar
			c(6) = reconnba

			If (Command$().ToLower.Contains("-debug")) Then
				c(3) = True
				Label10.Content = "DEBUG MODE"
			End If

			thr.Start(c)

			TextBox1.IsEnabled = False
			TextBox2.IsEnabled = False
			CheckBox1.IsEnabled = False
			Label5.Content = "Communicating..."
			Timer1.Start()
			Button1.Content = "Disconnect"
		ElseIf Button1.Content = "Disconnect" Then
			Try
				restart = True
				Windows.Forms.Application.Restart()
				Application.Current.Shutdown() 'reconnecting makes weird bugs, easier to restart application
			Catch
			End Try
		End If
	End Sub

	''' <summary>
	''' Called when there is incoming message on chat
	''' </summary>
	Sub chatset()
		Me.Dispatcher.Invoke(Sub() Me.Button2.Content = "Chat(!)")
	End Sub

	''' <summary>
	''' Shown when connection to the DTun4 server was lost
	''' </summary>
	Sub reconnbar()
		state1 = lib1.state
		If state1 < 7 Then
			Me.Dispatcher.Invoke(Sub() Me.ProgressBar1.Value = 50)
		End If
		Me.Dispatcher.Invoke(Sub() Me.Label5.Content = "Reconnecting to DTun4 Server")
	End Sub

	''' <summary>
	''' Toggles speechsynthesizer
	''' </summary>
	Private Sub mutebutton_PreviewMouseDown(sender As Object, e As MouseButtonEventArgs) Handles mutebutton.PreviewMouseDown
		If mutebutton.Content = "unmute" Then
			mutebutton.Content = "mute"
			lib1.speech.Volume = "100"
		Else
			mutebutton.Content = "unmute"
			lib1.speech.Volume = "0"
		End If
	End Sub
End Class
