Imports System.Text
Imports System.Security.Cryptography
Imports System.Runtime.InteropServices

Public Class Chat
    Dim Timer1 As New Windows.Forms.Timer
    Dim chatlines1 As New List(Of String)

    Private Sub Button_PreviewMouseDown(sender As Object, e As MouseButtonEventArgs)
        MainWindow.lib1.SendMessage(TextBox1.Text)
        TextBox1.Text = ""
        TextBox1.Focus()
    End Sub

    Private Sub TextBox1_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox1.KeyDown
        If e.Key = Key.Enter Then
            e.Handled = True
            Button_PreviewMouseDown(Nothing, Nothing)
        End If
    End Sub

    Private Sub MetroWindow_Loaded(sender As Object, e As RoutedEventArgs)
        Timer1.Interval = 100
        AddHandler Timer1.Tick, AddressOf Timer1_Tick

        Timer1.Start()
        TextBox1.Focus()
    End Sub
	Private Sub Timer1_Tick(sender As Object, e As EventArgs)
		Try
			If Not MainWindow.lib1.chatlines.Count = chatlines1.Count Then
				For i As Integer = chatlines1.Count To MainWindow.lib1.chatlines.Count - 1
					Dim mes As String() = Split(MainWindow.lib1.chatlines(i), {":"c}, 2)
					For j As Integer = 0 To MainWindow.lib1.users.Count - 1
						If MainWindow.lib1.users(j).Contains(mes(0)) Then
							mes(0) = MainWindow.lib1.users(j).Split(":")(0)
							Exit For
						End If
					Next
					RichTextBox1.AppendText(vbNewLine & mes(0) & ": " & mes(1))
				Next
				chatlines1 = New List(Of String)(MainWindow.lib1.chatlines)
				Format()
				ScrollToBottom(RichTextBox1)

			End If
		Catch
		End Try
	End Sub

	''' <summary>
	''' Color is random, but same for each clients as it's 4 first numbers of MD5 hash of the clientname
	''' </summary>
	Sub Format()
        Dim lines() As String = RichTextBox1.Text.Split(vbLf)
        Dim startIndex As Integer = 0
        For i As Integer = 0 To lines.Length - 1
            RichTextBox1.Select(startIndex, lines(i).Length)
            If lines(i).StartsWith("You:") Then
                RichTextBox1.SelectionColor = System.Drawing.Color.Black
            Else
                Dim color1 As String = "#" & GetMd5Hash(System.Security.Cryptography.MD5.Create(), lines(i).Split(":")(0)).Substring(0, 4) & "00"
                RichTextBox1.SelectionColor = System.Drawing.ColorTranslator.FromHtml(color1)
            End If
            startIndex += lines(i).Length + vbLf.Length
        Next
    End Sub
    Shared Function GetMd5Hash(ByVal md5Hash As MD5, ByVal input As String) As String
        Dim data As Byte() = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input))
        Dim sBuilder As New StringBuilder()
        Dim i As Integer
        For i = 0 To data.Length - 1
            sBuilder.Append(data(i).ToString("x2"))
        Next i
        Return sBuilder.ToString()

    End Function


	<DllImport("user32.dll", CharSet:=CharSet.Auto)> _
    Private Shared Function SendMessage(hWnd As IntPtr, wMsg As Integer, wParam As IntPtr, lParam As IntPtr) As Integer
    End Function
    Private Const WM_VSCROLL As Integer = 277
    Private Const SB_PAGEBOTTOM As Integer = 7

	''' <summary>
	''' Autoscrolling
	''' </summary>
	Public Shared Sub ScrollToBottom(MyRichTextBox As Windows.Forms.RichTextBox)
        SendMessage(MyRichTextBox.Handle, WM_VSCROLL, SB_PAGEBOTTOM, IntPtr.Zero)
    End Sub

	''' <summary>
	''' Link handler
	''' </summary>
	Private Sub RichTextBox1_LinkClicked(sender As Object, e As Forms.LinkClickedEventArgs) Handles RichTextBox1.LinkClicked
		System.Diagnostics.Process.Start(e.LinkText) 'just pass the link
	End Sub
End Class
