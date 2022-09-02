char Adjust(double tmp)
{
	return (unsigned char)((tmp >= 0 && tmp <= 255) ? tmp : (tmp < 0 ? 0 : 255));
}
extern "C" __declspec(dllexport) void YUV_RGB_8Bit(int i, int videoWidth, int rgb_width, int yuv_width, unsigned char* buffer, unsigned char* bufferY, unsigned char* bufferU, unsigned char* bufferV)
{
	unsigned char Y, U, V;
	int offSet;
	for (int j = 0; j < videoWidth; j++)
	{
		Y = *(bufferY + videoWidth * i + j);
		offSet = (i >> 1) * yuv_width + (j >> 1);
		U = *(bufferU + offSet);
		V = *(bufferV + offSet);

		offSet = rgb_width * i + j * 4;
		*(buffer + offSet) = Adjust((Y * 1.164384f) + (U * -0.000000f) + (V * 1.792741f) + (255 * -0.972945f));
		*(buffer + offSet + 1) = Adjust((Y * 1.164384f) + (U * -0.213249f) + (V * -0.532909f) + (255 * 0.301483f));
		*(buffer + offSet + 2) = Adjust((Y * 1.164384f) + (U * 2.112402f) + (V * -0.000000f) + (255 * -1.133402f));
		*(buffer + offSet + 3) = Adjust((Y * 0.000000f) + (U * 0.000000f) + (V * 0.000000f) + (255 * 1.000000f));
	}
}