Imports Microsoft.Win32

Module Install

    Sub Main()
        If Command$().Contains("-r") Then
            Console.WriteLine("##############################")
            Console.WriteLine("Removing old TAP devices 1 of 1...")
            Console.WriteLine("##############################")
            Console.WriteLine()

            If Environment.Is64BitOperatingSystem Then
                Shell("devcon.exe remove tap0901", AppWinStyle.Hide, True, -1)
            Else
                Shell("tapinstall.exe remove tap4955", AppWinStyle.Hide, True, -1)
            End If
            Console.WriteLine("##############################")
            Console.WriteLine("Done")
            Console.WriteLine("##############################")
            Console.WriteLine()
            Exit Sub
        End If
        Console.Write("Detected ")
        If Environment.Is64BitOperatingSystem Then
            Console.WriteLine("64bit system")
        Else
            Console.WriteLine("32bit system")
        End If
        Console.WriteLine("##############################")
        Console.WriteLine("Removing old TAP devices 1 of 5...")
        Console.WriteLine("##############################")
        Console.WriteLine()


        If Environment.Is64BitOperatingSystem Then
            Shell("devcon.exe remove tap0901", AppWinStyle.Hide, True, -1)
        Else
            Shell("tapinstall.exe remove tap4955", AppWinStyle.Hide, True, -1)
        End If

        Console.WriteLine()
        Console.WriteLine("##############################")
        Console.WriteLine("Installing TAP device 2 of 5...")
        Console.WriteLine("##############################")
        Console.WriteLine()

        If Environment.Is64BitOperatingSystem Then
            Shell("devcon.exe install OemWin2k.inf tap0901", AppWinStyle.Hide, True, -1)
        Else
            Shell("tapinstall.exe install OemWin2k.inf tap4955", AppWinStyle.Hide, True, -1)
        End If


        Console.WriteLine()
        Console.WriteLine("##############################")
        Console.WriteLine("Configuring DTun4 adapter 3 of 5...")
        Console.WriteLine("##############################")
        Console.WriteLine()


        System.Threading.Thread.Sleep(5000)

        Dim regpath As String = "SYSTEM\ControlSet001\Control\Class\{4D36E972-E325-11CE-BFC1-08002BE10318}"
        Dim keys As String() = Registry.LocalMachine.OpenSubKey(regpath, False).GetSubKeyNames


        Dim id As String = ""
        For i As Integer = 0 To keys.Count - 1
            Try
                Dim key As String = Registry.LocalMachine.OpenSubKey(regpath & "\" & keys(i), False).GetValue("DriverDesc")
                Dim value As String = Registry.LocalMachine.OpenSubKey(regpath & "\" & keys(i), False).GetValue("NetCfgInstanceId")

                If key = "TAP-Windows Adapter V9" And Environment.Is64BitOperatingSystem Then
                    Registry.LocalMachine.OpenSubKey(regpath & "\" & keys(i), True).SetValue("ProductName", "DTun4")
                    Registry.LocalMachine.OpenSubKey(regpath & "\" & keys(i), True).SetValue("ProviderName", "DTun4 Interface")
                    Registry.LocalMachine.OpenSubKey(regpath & "\" & keys(i), True).SetValue("MediaStatus", "1")
                    Registry.LocalMachine.OpenSubKey(regpath & "\" & keys(i) & "\Ndi\params\MediaStatus", True).SetValue("Default", "1")
                    id = value
                End If
                If key = "DTun4" Then
                    id = value
                    Exit For
                End If

            Catch
            End Try
        Next
        If id = "" Then
            Console.WriteLine("Error. DTun4 adapter not found.")
            Console.ReadLine()
            Exit Sub
        End If
        regpath = "SYSTEM\ControlSet001\Control\Network\{4D36E972-E325-11CE-BFC1-08002BE10318}\" & id & "\Connection"

        Dim name As String = Registry.LocalMachine.OpenSubKey(regpath, False).GetValue("Name")
        Console.WriteLine("Detected interface: " & name)

        Console.WriteLine()
        Console.WriteLine("##############################")
        Console.WriteLine("Configuring DTun4 adapter 4 of 5...")
        Console.WriteLine("##############################")
        Console.WriteLine()



        Shell("netsh interface set interface name=" & Chr(34) & name & Chr(34) & " newname=DTun4", AppWinStyle.Hide, True, -1)
        Console.WriteLine("Renamed " & name & " to DTun4")

        Console.WriteLine()
        Console.WriteLine("##############################")
        Console.WriteLine("Configuring DTun4 adapter 5 of 5...")
        Console.WriteLine("##############################")
        Console.WriteLine()


        Shell("netsh interface ip set address name=DTun4 source=static addr=32.0.0.10 mask=255.0.0.0 gateway=none", AppWinStyle.Hide, True, -1)
        Console.WriteLine("Set initial config for interface")

        Console.WriteLine()
        Console.WriteLine("##############################")
        Console.WriteLine("Device installed")
        Console.WriteLine("##############################")
        Console.WriteLine()
        Console.WriteLine("Press any key to finish...")
        'Console.ReadLine()
    End Sub

End Module
