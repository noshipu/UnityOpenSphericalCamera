# UnityOpenSphericalCamera
Support OSC API v1.0 on Unity
Take a picture and Load texture.

![Alt Text](http://img.f.hatena.ne.jp/images/fotolife/n/noshipu/20170221/20170221035242.png)

## How to use
1. Add component "OSCController"
2. Set camera IP address.
Gear360:192.168.107.1 / Bublcam:192.168.0.100 / RICOH THETA S or SC:192.168.1.1
3. Connect to camera by WiFi.
4. Call OSCController.ExecTakePictureAndLoadImage()
5. You can take a picture and load the image texture.

Show sample script
https://github.com/noshipu/UnityOpenSphericalCamera/blob/master/Assets/UnityOpenSphericalCamera/_Demo/Scripts/TakePictureManagerSample.cs

## Third party
This project uses LitJson to read Json format. https://lbv.github.io/litjson/ Thank you very much.

## License
MIT License
Copyright (c) 2017 noshipu
