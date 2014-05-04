Imports System.Security.Cryptography

Module RSAGenerator
    Sub Main()
        Console.Write("Enter keysize: ")
        Dim rsa As New RSACryptoServiceProvider(Console.ReadLine())
        System.IO.File.WriteAllText("priv.key", Format(rsa.ToXmlString(True)))
        System.IO.File.WriteAllText("rsapubkey.txt", Format(rsa.ToXmlString(False)))
        Console.WriteLine("Done.")
    End Sub
    Function Format(s As String) As String
        Dim t As String = ""
        For j As Integer = 1 To s.Count
            t &= s(j - 1)
            If j Mod 70 = 0 Then
                t &= vbNewLine
            End If
        Next
        Return t
    End Function
End Module
