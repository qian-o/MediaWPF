# MediaWPF
# Demo of video hardware decoding and rendering based on .NET 6 (no airspace problem) 
# Code implementation is for reference only 
In this video rendering, videos are **decoded by a GPU**, and the CPU does not work in most cases. Besides, there is no vexing problem of the spatial domain. <br>

The graphics card can exert its maximum performance when playing camera multi-channel videos or high-resolution and high-frame-rate videos (that's done in this project).<br>

It supports various network protocols such as **RTSP, RTMP and FLV**.<br>

It also provides excellent rendering efficiency at the time of playing **4k and 8k videos.** <br>

**This project refers to Lei Xiaohua's blog, so we highly appreciate his contribution to audio and video technology.** 

**Implementation principle:**<br>
Hardware decoding is achieved through the [LibVLCSharp](https://code.videolan.org/videolan/LibVLCSharp) library to call back video frame data in **YUV format (8bit, 10bit)**, and the pictures are rendered through [GLWpfControl](https://github.com/opentk/GLWpfControl) (this control is based on D3DImage, so there is no airspace problem).<br> 

**Video YUV data -> OpenGL -> Shader(YUV to RGB) -> picture rendering** <br>

**Testing equipment**<br>
**CPU: AMD Ryzen 7 5800H**<br>
**GPU: Nvidia GeForce RTX 3050 Laptop GPU 4G**<br>

**Considering that the laptop relies on a core graphics card to render the pictures and the power consumption is limited, the actual test efficiency will be affected to some extent.** 

**4K 60fps SDR video** <br>
**CPU utilization 5 ~ 10%** <br>
**GPU utilization 40 ~ 50%** <br>
 ![image](https://user-images.githubusercontent.com/84434846/175889091-417ee743-86a8-449a-b276-39c425c23e0a.png)


**4K 60fps HDR (in view of insufficient video brightness, HDR videos appearing on SDR screen is post-processed by tone mapping, and the conversion matrix available on the Internet will basically lose brightness)** <br>
**CPU utilization 10 ~ 20%** <br>
**GPU utilization 50 ~ 60%** <br>
 ![image](https://user-images.githubusercontent.com/84434846/175889286-f808e55a-7ed0-44b7-bb94-069d5626b5f2.png)


**4K 144fps SDR video (a high frame rate is achieved for later frame interpolation, so the frame interval is unstable)** <br>
**CPU utilization 10 ~ 20%** <br>
**GPU utilization 60 ~ 75%** <br>
 ![image](https://user-images.githubusercontent.com/84434846/175889702-817eb4da-c223-4025-8d5f-36e7ba78cc7f.png)


**8K 60fps SDR video (actually stable at around 40-45fps)** <br>
**CPU utilization 10 ~ 20%** <br>
**GPU utilization 70 ~ 80%** <br>
 ![image](https://user-images.githubusercontent.com/84434846/175890181-96c9c438-3e3f-4726-9d03-4e3cefecd613.png)


**4-channel 1080p SDR video (30fps for the first two videos, 25fps for the last two)** <br>
![image](https://user-images.githubusercontent.com/84434846/175896535-fbe35026-5b4b-4643-b53a-8497589c2631.png)

