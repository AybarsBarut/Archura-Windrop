using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windrop.Domain;

namespace Windrop.App;

public sealed class WpfClipboardService(Dispatcher dispatcher) : IClipboardService
{
    public async Task CopyAsync(ClipboardPayload payload, CancellationToken cancellationToken = default)
    {
        await dispatcher.InvokeAsync(() =>
        {
            var data = new DataObject();
            var fileDropPaths = payload.ConvertedFilePaths is { Count: > 0 }
                ? payload.ConvertedFilePaths
                : [payload.FilePath];
            var files = new StringCollection();
            files.AddRange(fileDropPaths.Where(File.Exists).ToArray());
            data.SetFileDropList(files);
            if (!string.IsNullOrWhiteSpace(payload.Text)) data.SetText(payload.Text, TextDataFormat.UnicodeText);
            var imagePath = payload.Kind is ReceivedItemKind.Jpeg or ReceivedItemKind.Png
                ? payload.FilePath : payload.PreviewImagePath;
            if (imagePath is not null && File.Exists(imagePath))
            {
                using var stream = File.OpenRead(imagePath);
                var image = new BitmapImage();
                image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.StreamSource = stream; image.EndInit(); image.Freeze();
                data.SetImage(image);
            }
            Clipboard.SetDataObject(data, true);
        }, DispatcherPriority.Normal, cancellationToken);
    }
}
