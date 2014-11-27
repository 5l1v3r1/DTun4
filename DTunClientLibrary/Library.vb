Imports SharpPcap
Imports System.IO
Imports System.Net.Sockets
Imports System.Net
Imports System.Text
Imports PacketDotNet
Imports System.Security.Cryptography
Imports DTun4.STUN_Client


Public Class Library
    Dim device As ICaptureDevice
    Public listener As UdpClient = New UdpClient()
    Public groupEP As IPEndPoint
    Dim source As New IPEndPoint(IPAddress.Any, 4955)
    Public IP As String
    Dim log1 As StreamWriter '= New StreamWriter("log.txt", True)
#If DEBUG Then
    Dim remote As String = "192.168.1.2"
#Else
    Dim remote As String = "188.116.56.69"
#End If

    Public updateusers As Boolean = False
    Public users As String()
    Public oldusers() As String = {""}
    Public conn As Boolean = False
    Dim serverrsa As New RSACryptoServiceProvider()
    Dim aespass(31) As Byte
    Public state As Integer = 0

    Public Shared icon As System.Windows.Forms.NotifyIcon

    Dim ih As IconHelper

    Public chatlines As New List(Of String)
    Dim chatsender As New UdpClient()

    'Public Shared chatbutton As Windows.Forms.Button
    Public Shared chatbutton

    Dim thr As Threading.Thread
    Public Sub Main(c As Object())
        If Not log1 Is Nothing Then
            log1.Close()
        End If
        log1 = New StreamWriter("log.txt", True)
        log1.WriteLine("Preparing...")



        Dim cname As String = c(0)
        Dim nname As String = c(1)
        Dim staticip As Boolean = c(2)

        icon = c(4)
        ih = New IconHelper

        chatbutton = c(5)

#If Not Debug Then
        Dim w As New MyWebClient
        w.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.0) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.64 Safari/537.11")
        remote = Dns.GetHostEntry("apps.disahome.me").AddressList(0).ToString
        If File.Exists("rsapubkey.txt") And c(3) Then
            serverrsa.FromXmlString(File.ReadAllText("rsapubkey.txt"))
        Else
            Try
                serverrsa.FromXmlString(w.DownloadString("http://dtun4.disahome.me/data/rsapubkey.txt"))
            Catch
                Try
                    serverrsa.FromXmlString(w.DownloadString("http://dtun4.disahome.me/data/rsapubkey.txt"))
                Catch
                    state = 7
                    MsgBox("Can't download server public RSA key.")
                    Exit Sub

                End Try
            End Try
        End If
        w.Dispose()
#End If
        log1.WriteLine("Received IP and public key")
        state = 1
        Threading.Thread.Sleep(200)

        Dim salt1(8) As Byte
        Using rngCsp As New RNGCryptoServiceProvider()
            rngCsp.GetBytes(salt1)
        End Using

        Dim k1 As New Rfc2898DeriveBytes(RandKey(32), salt1, 100000)
        aespass = k1.GetBytes(32)
        k1.Reset()
        k1.Dispose()
        If c(3) = True Then
            log1.WriteLine("Generated AES key - DEBUG - " & BitConverter.ToString(aespass).Replace("-", String.Empty))
            log1.WriteLine("AES test: string = test = " & BitConverter.ToString(AES_Encrypt(Encoding.Default.GetBytes("test"))).Replace("-", String.Empty))
            log1.WriteLine("AES test: string = " & BitConverter.ToString(AES_Decrypt(AES_Encrypt(Encoding.Default.GetBytes("test")))).Replace("-", String.Empty))
        Else
            log1.WriteLine("Generated AES key")
        End If

        log1.Flush()
        Threading.Thread.Sleep(200)

        If staticip Then
            IP = getIP()
            If IP = "31.0.0.10" Then
                IP = "DONOTWANT"
            End If
        Else
            IP = "DONOTWANT"
        End If


        state = 2
        log1.WriteLine("STUN experiment")

        groupEP = New IPEndPoint(IPAddress.Parse(remote), 4955)
        source = groupEP

        Dim dstun As New UdpClient()
        dstun.Client.ReceiveTimeout = 3000
        dstun.Client.SendTimeout = 3000
        Dim stuninfo As String
        Try
            'Dim result1 As STUN_Result = STUN_Client.Query("stun.stunprotocol.org", 3478, socket)
            'stuninfo = result1.NetType.ToString
            'If result1.NetType <> STUN_NetType.UdpBlocked Then
            '    stuninfo = stuninfo & " - " & result1.PublicEndPoint.ToString
            'End If
            dstun.Send(Encoding.Default.GetBytes("DSTUN1"), Encoding.Default.GetByteCount("DSTUN1"), groupEP)
            Dim res As Byte() = dstun.Receive(source)
            dstun.Send(res, res.Length, groupEP)
            source = New IPEndPoint(IPAddress.Any, 0)
            Dim res1 As String = Encoding.Default.GetString(dstun.Receive(source))
            If res1 = "DSTUNOK" Then
                stuninfo = "Full Cone/Open Internet"
            Else
                stuninfo = "Restricted Cone"
            End If
        Catch
            stuninfo = "Restricted Cone"
        End Try

        state = 3
        log1.WriteLine("Connecting to DTun4 Server")

        Dim hellostring As String = String.Format("HELO*{0}*{1}*{2}*{3}*{4}", cname, nname, IP, stuninfo, System.Text.Encoding.Default.GetString(serverrsa.Encrypt(aespass, True)))
        listener.Send(Encoding.Default.GetBytes(hellostring), Encoding.Default.GetByteCount(hellostring), groupEP)

        Dim response() As String = Encoding.Default.GetString(listener.Receive(source)).Split({"*"c}, 3)
        IP = response(1)
        users = response(2).Split("^")


        updateusers = True
        Shell("netsh interface ip set address name=DTun4 source=static addr=" & response(1) & " mask=255.0.0.0 gateway=none", AppWinStyle.Hide, True, -1)


        thr = New Threading.Thread(AddressOf ReceivePacket)
        thr.IsBackground = True


        log1.WriteLine("Connected with server")
        log1.WriteLine("Scanning for network devices...")
        state = 4
        Threading.Thread.Sleep(150)

        log1.Flush()
        Dim devices As CaptureDeviceList = CaptureDeviceList.Instance()
        Dim chdev As Integer = -1

        'Dim log As String = ""


        Dim i As Integer = -1
        For Each dev As ICaptureDevice In devices
            Dim info() As String = dev.ToString.Split(vbLf)
            For j As Integer = 0 To info.GetUpperBound(0)
                If info(j).Contains("FriendlyName: ") Then
                    If info(j).Replace("FriendlyName: ", "").StartsWith("DTun4") Then
                        chdev = i + 1
                        Exit For
                    End If
                End If
            Next
            'log = String.Format("{0}" & vbNewLine, dev.ToString)
            i += 1
        Next
        If chdev = -1 Then
            log1.WriteLine("DTun adapter was not found. Try reinstalling WinPcap.")
            MsgBox("DTun adapter was not found. Try reinstalling WinPcap.")
            state = 7
            Exit Sub
        End If
        log1.WriteLine("Device found. Connecting...")
        state = 5
        Threading.Thread.Sleep(150)


        device = devices(chdev)
        AddHandler device.OnPacketArrival, New SharpPcap.PacketArrivalEventHandler(AddressOf HandlePacket)
        device.Open(DeviceMode.Normal, 1)
        device.StartCapture()
        thr.Start()
        log1.WriteLine("Connected with device.")
        log1.WriteLine("Working...")
        log1.WriteLine()
        state = 6
        conn = True
        log1.Flush()
        'Console.ReadLine()
    End Sub
    Public Shared Function getIP()
        Dim strHostName As String = System.Net.Dns.GetHostName()
        For i As Integer = 0 To System.Net.Dns.GetHostByName(strHostName).AddressList.Count - 1
            If System.Net.Dns.GetHostByName(strHostName).AddressList(i).ToString().StartsWith("31.") Then
                Return System.Net.Dns.GetHostByName(strHostName).AddressList(i).ToString()
            End If
        Next
    End Function
    Public Sub SDTun()
        Try
            log1.Flush()
            thr.Abort()
            device.StopCapture()
            'log1.Close()
        Catch
        End Try
    End Sub


    Private Sub HandlePacket(sender As Object, e As CaptureEventArgs)
        ih.U()
        Dim packet As Byte() = e.Packet.Data
        log1.Write("Captured packet: ")
        Dim pack As Packet = PacketDotNet.Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data)
        Dim ip1 As IpPacket = IpPacket.GetEncapsulated(pack)
        Dim arp As ARPPacket = ARPPacket.GetEncapsulated(pack)

        If (Not ip1 Is Nothing) Then
            ParseTCPIP(pack, packet)
        ElseIf (Not arp Is Nothing) Then
            ParseARP(pack, packet)
        End If

        If (ip1 Is Nothing) And (arp Is Nothing) Then
            log1.WriteLine("nonIP nor arp packet")
            Exit Sub
        End If

        log1.Flush()

    End Sub

    Public Sub SendMessage(mes As String)
        chatsender.Send(System.Text.Encoding.Default.GetBytes("CHAT" & mes), System.Text.Encoding.Default.GetByteCount("CHAT" & mes), New IPEndPoint(IPAddress.Parse("31.255.255.255"), 4956))
        chatlines.Add("You: " & mes)
    End Sub
    Public Sub SendControlMessageReq(ip As String)
        Try
            chatsender.Send(System.Text.Encoding.Default.GetBytes("DTun4CM-REQ"), System.Text.Encoding.Default.GetByteCount("DTun4CM-REQ"), New IPEndPoint(IPAddress.Parse(ip), 4957))
        Catch
        End Try
    End Sub
    Private Sub SendControlMessageReply(ip As String)
        Try
            chatsender.Send(System.Text.Encoding.Default.GetBytes("DTun4CM-REP"), System.Text.Encoding.Default.GetByteCount("DTun4CM-REP"), New IPEndPoint(IPAddress.Parse(ip), 4957))
        Catch
        End Try
    End Sub

    Private Sub ReceivePacket()
        While True
            Try
                source = groupEP
                Dim packet As Byte() = listener.Receive(source)

                Dim message As String = Encoding.Default.GetString(packet)
                If (message.StartsWith("KFINE")) Then
                    users = Encoding.Default.GetString(packet).Substring(5).Split("^")
                    If Not DirectCast(oldusers, IStructuralEquatable).Equals(users, StructuralComparisons.StructuralEqualityComparer) Then
                        oldusers = users
                        updateusers = True
                    End If
                    Continue While
                End If
                If (Encoding.Default.GetString(packet).StartsWith("RECONNPLS")) Then
                    conn = False
                    thr.Abort()
                    device.StopCapture()
                    Exit Sub
                End If
                packet = AES_Decrypt(packet)



                If packet Is {0} Then
                    Continue While
                End If

                ih.R()

                Dim pack As Packet = PacketDotNet.Packet.ParsePacket(LinkLayers.Ethernet, packet)
                Dim ip1 As IpPacket = IpPacket.GetEncapsulated(pack)
                Dim arp As ARPPacket = ARPPacket.GetEncapsulated(pack)



                If (Not ip1 Is Nothing) Then
                    'If ip1.DestinationAddress.Equals(IPAddress.Parse(IP)) Then

                    message = Encoding.Default.GetString(packet)
                    If message.Contains("CHAT") Then
                        chatlines.Add(ip1.SourceAddress.ToString & ":" & message.Substring(message.IndexOf("C")).Replace("CHAT", ""))
                        log1.WriteLine("Created chat message from {0}", ip1.SourceAddress.ToString)
                        'chatbutton.Text = "Chat (!)"
                        chatbutton.Content = "Chat (!)" 'WPF FIX
                        Continue While
                    End If
                    If message.Contains("DTun4CM-REQ") Then
                        If ip1.DestinationAddress.ToString = IP Then
                            SendControlMessageReply(ip1.SourceAddress.ToString)
                            log1.WriteLine("CM-REQ: Replied")
                        End If
                        Continue While
                    End If

                    device.SendPacket(packet)
                    log1.WriteLine("Created IP packet from {0}", ip1.SourceAddress.ToString)
                End If

                If (Not arp Is Nothing) Then
                    device.SendPacket(packet)
                    log1.WriteLine("Created ARP packet from {0}", arp.SenderProtocolAddress.ToString)
                End If

                log1.Flush()
            Catch e As Exception
            End Try
        End While
    End Sub

    Private Sub ParseTCPIP(pack As Packet, Packet As Byte())
        Dim ip1 As IpPacket = IpPacket.GetEncapsulated(pack)
        log1.Write("IP packet: ")
        If ip1.Version = IpVersion.IPv4 Then
            If ip1.SourceAddress.Equals(IPAddress.Parse(IP)) Then
                Dim groupEP As New IPEndPoint(IPAddress.Parse(remote), 4955)
                Packet = AES_Encrypt(Packet)
                listener.Send(Packet, Packet.Count(), groupEP)
                log1.WriteLine("Sent to {0}", ip1.DestinationAddress.ToString)
            Else
                log1.WriteLine("Local packet from {0}. Skipped", ip1.SourceAddress.ToString)
            End If
        Else
            log1.WriteLine("v6Packet")
        End If

    End Sub

    Private Sub ParseARP(pack As Packet, Packet As Byte())
        Dim arp As ARPPacket = ARPPacket.GetEncapsulated(pack)
        log1.Write("ARP packet: ")
        If arp.SenderProtocolAddress.ToString = IP Then
            Dim groupEP As New IPEndPoint(IPAddress.Parse(remote), 4955)
            listener.Send(AES_Encrypt(Packet), AES_Encrypt(Packet).Count(), groupEP)
            log1.WriteLine("Sent to {0}", arp.TargetProtocolAddress.ToString)
        Else
            log1.WriteLine("Local packet. Skipped")
        End If
    End Sub



    Private Function AES_Decrypt(ByVal in1 As Byte()) As Byte()
        Dim AES As New System.Security.Cryptography.AesManaged
        Try
            AES.Key = aespass
            AES.Mode = CipherMode.ECB
            Dim AESDecrypter As System.Security.Cryptography.ICryptoTransform = AES.CreateDecryptor
            Return AESDecrypter.TransformFinalBlock(in1, 0, in1.Length)
        Catch ex As Exception
            Return {0}
        End Try
    End Function
    Private Function AES_Encrypt(ByVal in1 As Byte()) As Byte()
        Dim AES As New System.Security.Cryptography.AesManaged
        Try
            AES.Key = aespass
            AES.Mode = CipherMode.ECB
            Dim AESEncrypter As System.Security.Cryptography.ICryptoTransform = AES.CreateEncryptor
            Return AESEncrypter.TransformFinalBlock(in1, 0, in1.Length)
        Catch ex As Exception
            Return {0}
        End Try
    End Function


    Private Function Rand(ByVal Min As Integer, ByVal Max As Integer) As Integer
        Static Generator As System.Random = New System.Random()
        Return Generator.Next(Min, Max)
    End Function

    Private Function RandKey(RequiredStringLength As Integer) As String
        Dim CharArray() As Char = "(CMWXp),./<123wxyz!@#$%^[]{hijkOPY&*>0ab':Z67cKLsQRSEFGHIJ?;8TUdefg-=_+4vmnoV5ABqrD9tul}\|".ToCharArray
        Dim sb As New System.Text.StringBuilder

        For index As Integer = 1 To RequiredStringLength
            sb.Append(CharArray(Rand(0, CharArray.Length)))
        Next

        Return sb.ToString

    End Function

End Class