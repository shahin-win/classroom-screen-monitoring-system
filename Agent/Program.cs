using Microsoft.AspNetCore.SignalR.Client;
using Service_Host_Visual;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

class Agent
{

    static async Task Main()
    {

        string serverUrl = "http://your-server-url:Port/screenhub";
        string machineName = Environment.MachineName;

        while (true)   // 🔥 Main infinite recovery loop
        {
            HubConnection hub = null;

            try
            {
                // ---------------------------------------------------------------
                // 1) Build new SignalR connection every cycle
                // ---------------------------------------------------------------
                hub = new HubConnectionBuilder()
                    .WithUrl(serverUrl)
                    .WithAutomaticReconnect()
                    .Build();

                // ---------------------------------------------------------------
                // 2) Attempt to connect
                // ---------------------------------------------------------------
                Console.WriteLine("Connecting to server...");
                await hub.StartAsync();
                Console.WriteLine("Connected!");


                // ===============================================================
                // ✅ STEP 3 — LISTEN FOR REMOTE INPUT (ADD THIS HERE)
                // ===============================================================

                hub.On<RemoteInputCommand>("RemoteInput", cmd =>
                {
                    try
                    {
                        Console.WriteLine($"INPUT RECEIVED → {cmd.Type}");
                        InputInjector.Inject(cmd);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Input inject error: " + ex.Message);
                    }
                });

                // ---------------------------------------------------------------
                // 3) Register this PC
                // ---------------------------------------------------------------
                await hub.InvokeAsync("RegisterAgent", machineName);
                Console.WriteLine("Registered as " + machineName);

                // ---------------------------------------------------------------
                // 4) Loop sending screenshots
                // ---------------------------------------------------------------

                while (hub.State == HubConnectionState.Connected)
                {
                    try
                    {
                        string img = CapturePrimaryScreenBase64(1);
                        await hub.InvokeAsync("SendScreenshot", machineName, img);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Send error: " + ex.Message);
                    }

                    await Task.Delay(10); // .1 second
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connection error: " + ex.Message);
            }

            // ---------------------------------------------------------------
            // 5) If disconnected, wait and reconnect
            // ---------------------------------------------------------------
            Console.WriteLine("Disconnected. Reconnecting in 3 seconds...");
            await Task.Delay(3000);
        }

    }

    // ===================================================================
    // SCREEN CAPTURE
    // ===================================================================
    static string CapturePrimaryScreenBase64(double scale)
    {
        var screen = Screen.PrimaryScreen.Bounds;
        using var fullBmp = new Bitmap(screen.Width, screen.Height);
        using (var g = Graphics.FromImage(fullBmp))
        {
            g.CopyFromScreen(0, 0, 0, 0, screen.Size);
        }

        Bitmap finalBmp;

        if (scale < 1.0)
        {
            int targetW = (int)(screen.Width * scale);
            int targetH = (int)(screen.Height * scale);

            finalBmp = new Bitmap(targetW, targetH);

            using (var g = Graphics.FromImage(finalBmp))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(fullBmp, new Rectangle(0, 0, targetW, targetH));
            }
        }
        else
        {
            // no scaling requested
            finalBmp = (Bitmap)fullBmp.Clone();
        }

        using var ms = new MemoryStream();
        finalBmp.Save(ms, GetEncoder(ImageFormat.Jpeg), new EncoderParameters(1)
        {
            Param = { [0] = new EncoderParameter(Encoder.Quality, 90L) }
        });

        return Convert.ToBase64String(ms.ToArray());
    }

    static ImageCodecInfo GetEncoder(ImageFormat fmt)
    {
        foreach (var c in ImageCodecInfo.GetImageEncoders())
            if (c.FormatID == fmt.Guid) return c;
        return null;
    }
}
