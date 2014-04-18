Imports Microsoft.Win32

Module Install

    Sub Main()
        Console.WriteLine("##############################")
        Console.WriteLine("Removing old TAP devices 1 of 5...")
        Console.WriteLine("##############################")
        Console.WriteLine()

        Shell("deltapall.bat", AppWinStyle.Hide, True, -1)

        Console.WriteLine()
        Console.WriteLine("##############################")
        Console.WriteLine("Installing TAP device 2 of 5...")
        Console.WriteLine("##############################")
        Console.WriteLine()


        Dim dir As String = System.AppDomain.CurrentDomain.BaseDirectory()
        Shell("newtap.bat " & dir, AppWinStyle.Hide, True, -1)


        Console.WriteLine()
        Console.WriteLine("##############################")
        Console.WriteLine("Configuring DTun4 adapter 3 of 5...")
        Console.WriteLine("##############################")
        Console.WriteLine()


        'Shell("rename.bat", AppWinStyle.Hide, True, -1)
        System.Threading.Thread.Sleep(5000)

        Dim regpath As String = "SYSTEM\ControlSet001\Control\Class\{4D36E972-E325-11CE-BFC1-08002BE10318}"
        Dim keys As String() = Registry.LocalMachine.OpenSubKey(regpath, False).GetSubKeyNames

        Dim pairs As New Dictionary(Of String, String)
        For i As Integer = 0 To keys.Count - 1
            Try
                Dim key As String = Registry.LocalMachine.OpenSubKey(regpath & "\" & keys(i), False).GetValue("DriverDesc")
                Dim value As String = Registry.LocalMachine.OpenSubKey(regpath & "\" & keys(i), False).GetValue("NetCfgInstanceId")

                If key <> "" And value <> "" Then
                    pairs(key) = value
                End If
            Catch
            End Try
        Next

#If PLATFORM = "x86" Then
        Dim id As String = pairs("DTun4")
#End If
#If PLATFORM = "x64" Then
         Dim id As String = pairs("TAP-Windows Adapter V9")
#End If

        regpath = "SYSTEM\ControlSet001\Control\Network\{4D36E972-E325-11CE-BFC1-08002BE10318}\" & id & "\Connection"

        Dim name As String = Registry.LocalMachine.OpenSubKey(regpath, False).GetValue("Name")

        Console.WriteLine()
        Console.WriteLine("##############################")
        Console.WriteLine("Configuring DTun4 adapter 4 of 5...")
        Console.WriteLine("##############################")
        Console.WriteLine()




        'Dim name() As String = System.IO.File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) & "\name.txt")
        'Dim device As String = ""
        'For i As Integer = 0 To name.GetUpperBound(0)
        '    If name(i).Contains("Name""=") Then
        '        device = name(i).Substring(7)
        '        Exit For
        '    End If
        'Next

        Shell("netsh interface set interface name=" & Chr(34) & name & Chr(34) & " newname=DTun4", AppWinStyle.Hide, True, -1)
        'Console.WriteLine("netsh interface set interface name=" & Chr(34) & name & Chr(34) & " newname=DTun4")

        Console.WriteLine()
        Console.WriteLine("##############################")
        Console.WriteLine("Configuring DTun4 adapter 5 of 5...")
        Console.WriteLine("##############################")
        Console.WriteLine()


        Shell("netsh interface ip set address name=DTun4 source=static addr=31.0.0.10 mask=255.0.0.0 gateway=none", AppWinStyle.Hide, True, -1)

        Console.WriteLine()
        Console.WriteLine("##############################")
        Console.WriteLine("Device installed")
        Console.WriteLine("##############################")
        Console.WriteLine()
        Console.WriteLine("Press any key to finish...")
        'Console.ReadLine()
    End Sub

End Module
