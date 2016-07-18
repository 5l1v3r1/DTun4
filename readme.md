DTun4
=================
DTun4 is hosted VPN service designed to allow gamers host their game servers without obligation to have public IP. 
Your traffic is also secured by AES and RSA encryption. 
You can connect to computers within the same network anytime, distance is not a problem.
DTun4 connects to remote server to establish direct p2p connections. 
After the process is done, each packet you send appears on every computer in your network.

## How to install
If you just want to use ready application, go to https://dtun4.disahome.me/ and download launcher.

## How to compile
Just use Visual Studio, any version above 2013 should work.

## TODO
Code needs security audition

## Used components
* MahApps.Metro is under the Microsoft Public License (Ms-PL)
* SharpPcap 4.2 is under the GNU Library or Lesser General Public License version 3.0 (LGPLv3) license 
* PacketDotNet 0.13 is under the GNU Library or Lesser General Public License version 3.0 (LGPLv3) license 
* TUN/TAP Driver from OpenVPN is under the GPL version 2 license.

## License
GNU General Public License v3.0

## FAQ
* Could the data be seen outside of the network

I have done my best to use crypto functions properly, but it needs auditioning to be sure. 
Also no data is stored on server as you may see in source code.
* I have a feature/bug/request

Use issue tracker, I will try to fix it. Or if you already know how to fix it, you can do a pull request.
* I have found a critical bug

I would be very grateful if you mail me ASAP

## Give me feedback
Share your thoughts about the application(positive, negative, watever), I want to know that all my contribution into this project wasn't for nothing.
