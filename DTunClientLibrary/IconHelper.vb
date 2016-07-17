''' <summary>
''' Updates icon in tray
''' Depending on current transmission can display Upload, Download or both
''' </summary>
Class IconHelper
	Dim rec As Integer = 0
	Dim sen As Integer = 0
	Dim i1 As System.Drawing.Icon = My.Resources._1
	Dim i2 As System.Drawing.Icon = My.Resources._2
	Dim i3 As System.Drawing.Icon = My.Resources._3
	Dim i4 As System.Drawing.Icon = My.Resources._4


	Dim thr As New System.Timers.Timer(100)
	Sub New()
		AddHandler thr.Elapsed, AddressOf Update
		thr.Start()
	End Sub
	Public Sub R()
		If rec < 4 Then
			rec += 1
		End If
	End Sub
	Public Sub U()
		If sen < 4 Then
			sen += 1
		End If
	End Sub

	Sub Update()
		Try
			If rec > 0 Then
				If sen > 0 Then
					Library.icon.Icon = i4
					rec -= 1
					sen -= 1
				Else
					Library.icon.Icon = i3
					rec -= 1
				End If
			ElseIf sen > 0 Then
				Library.icon.Icon = i2
				sen -= 1
			Else
				Library.icon.Icon = i1
			End If
		Catch
		End Try
	End Sub
End Class