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


        Shell("rename.bat", AppWinStyle.Hide, True, -1)

        Console.WriteLine()
        Console.WriteLine("##############################")
        Console.WriteLine("Configuring DTun4 adapter 4 of 5...")
        Console.WriteLine("##############################")
        Console.WriteLine()


        Dim name() As String = System.IO.File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) & "\name.txt")
        Dim device As String = ""
        For i As Integer = 0 To name.GetUpperBound(0)
            If name(i).Contains("Name""=") Then
                device = name(i).Substring(7)
                Exit For
            End If
        Next

        Shell("netsh interface set interface name=" & device & " newname=DTun4", AppWinStyle.Hide, True, -1)

        Console.WriteLine()
        Console.WriteLine("##############################")
        Console.WriteLine("Configuring DTun4 adapter 5 of 5...")
        Console.WriteLine("##############################")
        Console.WriteLine()


        Shell("netsh interface ip set address name=DTun4 source=static address=31.0.0.10 mask=255.0.0.0", AppWinStyle.Hide, True, -1)

        Console.WriteLine()
        Console.WriteLine("##############################")
        Console.WriteLine("Device installed")
        Console.WriteLine("##############################")
        Console.WriteLine()
        Console.WriteLine("Press any key to finish...")
        'Console.ReadLine()
    End Sub

End Module
