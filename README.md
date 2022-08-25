# MediaWPF（DirectX、OpenGL、Skia）
# 视频播放控件并支持rtsp、rtmp等各类网络协议视频流
# 支持多种渲染模式并不存在空域（airspace）问题

**实现原理：**<br>
[LibVLCSharp](https://code.videolan.org/videolan/LibVLCSharp) 解码获取视频（8bit、10bit）帧数据<br>
在DirectX、OpenGL模式中程序根据视频色彩空间自动选择8bit或10bit处理。<br>
8bit：I420 支持DirectX、OpenGL、Skia<br>
10bit：I0AL 支持DirectX、OpenGL<br>
**Skia没有使用硬件加速，所以在效率上低于前两个图形库接口。**<br>

**4K 60帧**<br>
设备：NVIDIA GeForce RTX 3050 Laptop GPU<br>
OpenGL：<br>
![image](https://user-images.githubusercontent.com/84434846/186556586-b4b7c44a-6145-4c8a-af92-781827902d62.png)
DirectX：<br>
![image](https://user-images.githubusercontent.com/84434846/186556958-05363bb7-cff9-42af-9713-6813ee647841.png)
Skia：（这种cpu渲染图一乐）<br>
![image](https://user-images.githubusercontent.com/84434846/186557210-de5013ca-ffad-4f73-a133-36dc93243578.png)

说说结论：<br>
OpenGL: 考虑到兼容性和跨平台，TA无疑是最好的选择。<br>
DirectX：大微软提出的图形API性能指定没得挑，渲染上原生支持了YUV格式不需要像OpenGL一样在Shader中转换。（性能最佳）<br>
Skia：本身这哥们是支持使用OpenGL进行硬件加速的，但无奈技术功底有限自己没能实现。<br>

**未来如果Maui的Skia库要是支持硬件加速的话，那个人认为，Skia在Maui框架中做视频播放那指定是🐂🖊。**
