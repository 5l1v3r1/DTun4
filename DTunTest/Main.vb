Imports SharpPcap
Imports System.IO
Imports System.Net.Sockets
Imports System.Net
Imports System.Text
Imports PacketDotNet

Module Main
    Dim device As ICaptureDevice
    Dim listener As UdpClient = New UdpClient()
    Dim groupEP As IPEndPoint
    Dim IP As String
    Sub Main()
        Console.WriteLine("Connecting to DTun Server...")
        groupEP = New IPEndPoint(IPAddress.Parse("188.116.56.69"), 4955)
        Dim name As String
        Console.WriteLine("Enter name*network")
        name = Console.ReadLine()
        listener.Send(Encoding.Default.GetBytes("HELO*" & name), Encoding.Default.GetByteCount("HELO*" & name), groupEP)
        Dim localIPs As IPAddress() = Dns.GetHostAddresses(Dns.GetHostName())

        For k As Integer = 0 To localIPs.GetUpperBound(0)
            If localIPs(k).ToString.StartsWith("31.") Then
                IP = localIPs(k).ToString
            End If
        Next
        listener.Receive(groupEP)
        Dim thr As New Threading.Thread(AddressOf ReceivePacket)
        thr.IsBackground = True


        Dim devices As CaptureDeviceList = CaptureDeviceList.Instance()
        Dim chdev As Integer = -1

        Dim log As String = ""
        Console.WriteLine("Connected with server")
        Console.WriteLine("Scanning for network devices...")
        Dim i As Integer = -1
        For Each dev As ICaptureDevice In devices
            Dim info() As String = dev.ToString.Split(vbLf)
            For j As Integer = 0 To info.GetUpperBound(0)
                If info(j).Contains("FriendlyName: ") Then
                    If info(j).Replace("FriendlyName: ", "") = "DTun4" Then
                        chdev = i + 1
                        Exit For
                    End If
                End If
            Next
            log = String.Format("{0}" & vbNewLine, dev.ToString)
            i += 1
        Next
        If chdev = -1 Then
            'Console.WriteLine(vbNewLine & "The following devices are available on this machine:")
            'Console.WriteLine("----------------------------------------------------" & vbNewLine)
            'Console.Write(log)
            'Console.Write("Choose the device #(0 - {0}): ", i)
            'chdev = Console.ReadLine()
            Console.WriteLine("DTun adapter was not found. Try reinstalling it.")
            Console.ReadLine()
            Exit Sub
        End If
        Console.WriteLine("Device found. Connecting...")


        device = devices(chdev)
        AddHandler device.OnPacketArrival, New SharpPcap.PacketArrivalEventHandler(AddressOf HandlePacket)
        device.Open(DeviceMode.Normal, 30)
        device.StartCapture()
        thr.Start()
        Console.WriteLine("Connected with device.")
        Console.WriteLine("Working...")
        Console.WriteLine()

        Console.ReadLine()
    End Sub

    Sub HandlePacket(sender As Object, e As CaptureEventArgs)
        Dim packet As Byte() = e.Packet.Data
        Console.Write("Received packet: ")
        Dim pack As Packet = PacketDotNet.Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data)
        Dim ip1 As IpPacket = IpPacket.GetEncapsulated(pack)
        'Dim eth As EthernetPacket = EthernetPacket.GetEncapsulated(pack)
        Dim arp As ARPPacket = ARPPacket.GetEncapsulated(pack)

        If (Not ip1 Is Nothing) Then
            ParseTCPIP(pack, packet)
        End If

        If (Not arp Is Nothing) Then
            ParseARP(pack, packet)
        End If


        If (ip1 Is Nothing) And (arp Is Nothing) Then
            Console.WriteLine("nonIP nor arp packet")
            Exit Sub
        End If


    End Sub

    Sub ReceivePacket()
        While True
            Dim packet As Byte() = listener.Receive(groupEP)


            Dim pack As Packet = PacketDotNet.Packet.ParsePacket(LinkLayers.Ethernet, packet)
            Dim ip1 As IpPacket = IpPacket.GetEncapsulated(pack)
            Dim arp As ARPPacket = ARPPacket.GetEncapsulated(pack)


            If (Not ip1 Is Nothing) Then
                If ip1.DestinationAddress.Equals(IPAddress.Parse(IP)) Then
                    device.SendPacket(packet)
                    Console.WriteLine("Created IP packet from {0}", IPAddress.Parse(ip1.SourceAddress.Address))
                Else
                    Console.WriteLine("Received IP packed intended to another device. Skipped.")
                End If
            End If

            If (Not arp Is Nothing) Then
                If arp.TargetProtocolAddress.ToString = IP Then
                    device.SendPacket(packet)
                    Console.WriteLine("Created ARP packet from {0}", arp.SenderProtocolAddress.ToString)
                Else
                    Console.WriteLine("Received ARP packed intended to another device. Skipped.")
                End If
            End If



        End While
    End Sub

    Sub ParseTCPIP(pack As Packet, Packet As Byte())
        Dim ip1 As IpPacket = IpPacket.GetEncapsulated(pack)
        Console.Write("IP packet: ")
        If ip1.Version = IpVersion.IPv4 Then

            If Not ip1.DestinationAddress.Equals(IPAddress.Parse(IP)) And Not ip1.DestinationAddress.Equals(IPAddress.Parse("31.255.255.255")) And ip1.SourceAddress.Equals(IPAddress.Parse(IP)) Then
                Dim groupEP As New IPEndPoint(IPAddress.Parse("188.116.56.69"), 4955)
                listener.Send(Packet, Packet.Count(), groupEP)
                Console.WriteLine("Sent to {0}", IPAddress.Parse(ip1.DestinationAddress.Address))
            Else
                Console.WriteLine("Local packet from {0}. Skipped", IPAddress.Parse(ip1.SourceAddress.Address))
            End If
        Else
            Console.WriteLine("v6Packet")
        End If

    End Sub

    Sub ParseARP(pack As Packet, Packet As Byte())
        Dim arp As ARPPacket = ARPPacket.GetEncapsulated(pack)
        Console.Write("ARP packet: ")
        If Not arp.TargetProtocolAddress.ToString = IP And arp.SenderProtocolAddress.ToString = IP Then
            Dim groupEP As New IPEndPoint(IPAddress.Parse("188.116.56.69"), 4955)
            listener.Send(Packet, Packet.Count(), groupEP)
            Console.WriteLine("Sent to {0}", arp.TargetProtocolAddress.ToString)
        Else
            Console.WriteLine("Local packet. Skipped")
        End If
    End Sub

End Module
