# MediaWPF
# 基于 .NET 6 实现视频硬解码渲染Demo（无空域问题）
# 代码实现仅供学习参考
本项目视频渲染通过显卡进行视频解码，CPU几乎不参与工作，并且不存在令人烦躁的空域问题。<br>
在播放摄像头多路视频或高分辨率、高帧率视频时可以极大发挥显卡性能（我认为该项目做到了这一点）。<br>
支持各类网络协议如RTSP、RTMP、FLV等。<br>
播放4k、8k视频也可以做到极佳的渲染效率。<br>

**该项目实现参考雷霄骅大佬的博客，非常感谢他为音视频技术方向做出的贡献。**

**实现原理：**<br>
使用 [LibVLCSharp](https://code.videolan.org/videolan/LibVLCSharp) 库进行硬解码获取视频YUV格式（8bit、10bit）帧数据进行回调，采用[GLWpfControl](https://github.com/opentk/GLWpfControl) 控件用于呈现画面（该控件基于D3DImage，所以不存在空域问题）。<br>
视频YUV数据 -> OpenGL -> Shader(YUV to RGB) -> 呈现画面

**测试设备**<br>
**处理器：** AMD Ryzen 7 5800H<br>
**显卡：** Nvidia GeForce RTX 3050 Laptop GPU 4G<br>

**因笔记本依靠核显渲染画面，并且功耗方面有所限制，实际测试效率会存在一小方面影响。**

**4K 60帧 SDR视频**<br>
**处理器占用率 5~10%**<br>
**显卡占用率 40~50%**<br>
![image](https://user-images.githubusercontent.com/84434846/175889091-417ee743-86a8-449a-b276-39c425c23e0a.png)

**4K 60帧 HDR版本（视频亮度不足，在SDR屏幕上播放HDR视频都是经过色调映射的后处理，网上流传的转换矩阵基本都会丢失亮度）**<br>
**处理器占用率 10~20%**<br>
**显卡占用率 50~60%**<br>
![image](https://user-images.githubusercontent.com/84434846/175889286-f808e55a-7ed0-44b7-bb94-069d5626b5f2.png)

**4K 144帧 SDR视频（该视频为后期补帧实现高帧率，所以帧间隔不稳定）**<br>
**处理器占用率 10~20%**<br>
**显卡占用率 60~75%**<br>
![image](https://user-images.githubusercontent.com/84434846/175889702-817eb4da-c223-4025-8d5f-36e7ba78cc7f.png)

**8K 60帧 SDR视频（实际表现稳定在40~45帧左右）**<br>
**处理器占用率 10~20%**<br>
**显卡占用率 70~80%**<br>
![image](https://user-images.githubusercontent.com/84434846/175890181-96c9c438-3e3f-4726-9d03-4e3cefecd613.png)

**四路 1080p SDR视频（前两个视频为30帧，后两个视频为25帧）**<br>
![image](https://user-images.githubusercontent.com/84434846/175896535-fbe35026-5b4b-4643-b53a-8497589c2631.png)
