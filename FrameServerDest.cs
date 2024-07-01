using System;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Microsoft.Graphics.Canvas;
using Windows.Storage.Streams;
using Windows.Media.Transcoding;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.DirectX;

public class FrameServerDest
{
    private CanvasDevice canvasDevice;
    private GraphicsCaptureItem captureItem;
    private Direct3D11CaptureFramePool framePool;
    private GraphicsCaptureSession captureSession;
    private StorageFile recordedFile;
    private CanvasRenderTarget renderTarget;
    private InMemoryRandomAccessStream memoryStream;

    public FrameServerDest(CanvasDevice device, GraphicsCaptureItem item, StorageFile file)
    {
        canvasDevice = device;
        captureItem = item;
        recordedFile = file;
        renderTarget = new CanvasRenderTarget(canvasDevice, (float)item.Size.Width, (float)item.Size.Height, 96);
        memoryStream = new InMemoryRandomAccessStream();
    }

    public void StartRecording()
    {
        StartFrameServer();
    }

    public void StopRecording()
    {
        StopFrameServer();
        SaveRecording();
    }

    private void StartFrameServer()
    {
        framePool = Direct3D11CaptureFramePool.Create(
            canvasDevice,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            2,
            captureItem.Size);
        framePool.FrameArrived += FramePool_FrameArrived;

        captureSession = framePool.CreateCaptureSession(captureItem);
        captureSession.StartCapture();
    }

    private void StopFrameServer()
    {
        captureSession?.Dispose();
        framePool?.Dispose();
    }

    private async void FramePool_FrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        using (var frame = sender.TryGetNextFrame())
        {
            using (var ds = renderTarget.CreateDrawingSession())
            {
                ds.Clear(Windows.UI.Colors.Transparent);
                ds.DrawImage(CanvasBitmap.CreateFromDirect3D11Surface(canvasDevice, frame.Surface));
            }

            await renderTarget.SaveAsync(memoryStream, CanvasBitmapFileFormat.Jpeg, 1f);
        }
    }

    private async void SaveRecording()
    {
        //var profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD720p);
        //var stream = await recordedFile.OpenAsync(FileAccessMode.ReadWrite);

        //var mediaSource = MediaSource.CreateFromStream(memoryStream, "image/jpeg");

        //var transcoder = new MediaTranscoder();
        //var prepareOp = await transcoder.PrepareMediaStreamSourceTranscodeAsync(mediaSource, stream, profile);
        //if (prepareOp.CanTranscode)
        //{
        //    await prepareOp.TranscodeAsync();
        //}
        //else
        //{
        //    // Handle the error case here
        //}
    }
}
