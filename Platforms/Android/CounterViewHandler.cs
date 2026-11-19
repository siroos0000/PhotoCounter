#nullable disable
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Storage;

namespace PhotoCounter;

public class CounterViewHandler : ViewHandler<CounterView, CounterNativeView>
{
	public static readonly IPropertyMapper<CounterView, CounterViewHandler> PropertyMapper =
		new PropertyMapper<CounterView, CounterViewHandler>(ViewMapper)
		{
			[nameof(CounterView.ImagePath)] = MapImagePath,
			[nameof(CounterView.MarkerSize)] = MapMarkerSize,
		};

	public static readonly CommandMapper<CounterView, CounterViewHandler> CommandMapper =
		new(ViewCommandMapper)
		{
			[nameof(CounterView.Undo)] = (handler, view, args) => handler.PlatformView.Undo(),
			[nameof(CounterView.Clear)] = (handler, view, args) => handler.PlatformView.Clear(),
			[nameof(CounterView.ZoomIn)] = (handler, view, args) => handler.PlatformView.ZoomIn(),
			[nameof(CounterView.ZoomOut)] = (handler, view, args) => handler.PlatformView.ZoomOut(),
			[nameof(CounterView.ResetZoom)] = (handler, view, args) => handler.PlatformView.ResetZoom(),
			[nameof(CounterView.ClearSelection)] = (handler, view, args) => handler.PlatformView.ClearSelection(),
			[nameof(CounterView.DeleteMarker)] = (handler, view, args) => handler.PlatformView.DeleteMarker((int)args),
			[nameof(CounterView.MoveMarker)] = MapMoveMarker,
			[nameof(CounterView.ExportAsync)] = MapExport,
		};

	public CounterViewHandler() : base(PropertyMapper, CommandMapper)
	{
	}

	protected override CounterNativeView CreatePlatformView() => new CounterNativeView(Context);

	protected override void ConnectHandler(CounterNativeView platformView)
	{
		base.ConnectHandler(platformView);
		platformView.CountChanged += OnCountChanged;
		platformView.MarkerTapped += OnMarkerTapped;
	}

	protected override void DisconnectHandler(CounterNativeView platformView)
	{
		platformView.CountChanged -= OnCountChanged;
		platformView.MarkerTapped -= OnMarkerTapped;
		platformView.Release();
		base.DisconnectHandler(platformView);
	}

	void OnCountChanged(int count) => VirtualView?.RaiseCountChanged(count);

	void OnMarkerTapped(int index) => VirtualView?.RaiseMarkerTapped(index);

	// ------------------------------------------------------------------ نگاشت‌ها

	static void MapImagePath(CounterViewHandler handler, CounterView view)
	{
		var path = view.ImagePath;
		if (string.IsNullOrEmpty(path))
		{
			handler.PlatformView.ClearImage();
			return;
		}

		_ = LoadAsync(handler, view, path);
	}

	static async Task LoadAsync(CounterViewHandler handler, CounterView view, string path)
	{
		bool? ok;
		try
		{
			ok = await handler.PlatformView.LoadImageAsync(path);
		}
		catch (Exception)
		{
			ok = false;
		}

		if (ok.HasValue)
			view.RaiseImageLoaded(ok.Value);
	}

	static void MapMarkerSize(CounterViewHandler handler, CounterView view)
		=> handler.PlatformView.MarkerRadiusDp = (float)view.MarkerSize;

	static void MapMoveMarker(CounterViewHandler handler, CounterView view, object args)
	{
		var move = (ValueTuple<int, int>)args;
		handler.PlatformView.MoveMarker(move.Item1, move.Item2);
	}

	static void MapExport(CounterViewHandler handler, CounterView view, object args)
	{
		var tcs = (TaskCompletionSource<string>)args;

		try
		{
			var bitmap = handler.PlatformView.CreateExportBitmap();
			if (bitmap == null)
			{
				tcs.TrySetResult(null);
				return;
			}

			var path = Path.Combine(FileSystem.CacheDirectory, $"counted_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");

			// فشرده‌سازی JPEG کار سنگینی است؛ خارج از نخ UI
			_ = Task.Run(() =>
			{
				try
				{
					CounterNativeView.SaveJpeg(bitmap, path);
					tcs.TrySetResult(path);
				}
				catch (Exception ex)
				{
					tcs.TrySetException(ex);
				}
				finally
				{
					bitmap.Recycle();
				}
			});
		}
		catch (Exception ex)
		{
			tcs.TrySetException(ex);
		}
	}
}
