namespace ComputerUse.ScreenCapture;

interface IScreenCapturer
{
    byte[] CaptureScreen(int monitorIndex);
    public (int x, int y) GetScreenSize(int screenIndex);
}
