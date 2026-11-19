// در .NET 10 متدهای DisplayAlert / DisplayActionSheet هشدار «منسوخ» می‌دهند و نسخه‌های *Async جایگزین‌اند.
// همه‌ی فراخوانی‌ها از سه متد کمکی پایین فایل (AlertAsync / ConfirmAsync / ChooseAsync) رد می‌شوند،
// پس برای تغییر فقط همان سه متد را عوض کنید.
#pragma warning disable CS0618

using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace PhotoCounter;

public partial class MainPage : ContentPage
{
	const string CancelText = "انصراف";
	const string DeleteText = "حذف این علامت";
	const string RenumberText = "تغییر شماره";
	const string SaveText = "ذخیره در گالری";
	const string ShareText = "اشتراک‌گذاری";

	string? _currentPhotoPath;
	bool _hasImage;
	bool _busy;

	public MainPage()
	{
		InitializeComponent();

		Counter.CountChanged += (_, count) => CountLabel.Text = $"تعداد: {count}";
		Counter.ImageLoaded += OnImageLoaded;
		Counter.MarkerTapped += OnMarkerTapped;
	}

	// ------------------------------------------------------------------ گرفتن / انتخاب عکس

	async void OnTakePhotoClicked(object? sender, EventArgs e)
	{
		if (_busy) return;
		_busy = true;

		try
		{
			if (!await ConfirmReplaceAsync()) return;

			if (!MediaPicker.Default.IsCaptureSupported)
			{
				await AlertAsync("خطا", "دوربین در این دستگاه پشتیبانی نمی‌شود.");
				return;
			}

			var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
			if (status != PermissionStatus.Granted)
				status = await Permissions.RequestAsync<Permissions.Camera>();

			if (status != PermissionStatus.Granted)
			{
				await AlertAsync("دسترسی دوربین", "برای عکس‌گرفتن باید اجازه‌ی استفاده از دوربین را بدهید.");
				return;
			}

			var photo = await MediaPicker.Default.CapturePhotoAsync();
			if (photo != null)
				await ShowPhotoAsync(photo);
		}
		catch (Exception ex)
		{
			await AlertAsync("خطا", ex.Message);
		}
		finally
		{
			_busy = false;
		}
	}

	async void OnPickPhotoClicked(object? sender, EventArgs e)
	{
		if (_busy) return;
		_busy = true;

		try
		{
			if (!await ConfirmReplaceAsync()) return;

			var photo = await MediaPicker.Default.PickPhotoAsync();
			if (photo != null)
				await ShowPhotoAsync(photo);
		}
		catch (Exception ex)
		{
			await AlertAsync("خطا", ex.Message);
		}
		finally
		{
			_busy = false;
		}
	}

	async Task ShowPhotoAsync(FileResult photo)
	{
		// یک کپی در کش خود برنامه می‌سازیم تا مسیر پایدار و یکتا باشد
		var path = Path.Combine(FileSystem.CacheDirectory, $"photo_{DateTime.Now:yyyyMMdd_HHmmssfff}.jpg");

		await using (var source = await photo.OpenReadAsync())
		await using (var target = File.Create(path))
		{
			await source.CopyToAsync(target);
		}

		var previous = _currentPhotoPath;
		_currentPhotoPath = path;

		Busy.IsRunning = true;
		Counter.ImagePath = path;

		TryDelete(previous);
	}

	void OnImageLoaded(object? sender, bool ok)
	{
		Busy.IsRunning = false;

		if (ok)
		{
			_hasImage = true;
			EmptyHint.IsVisible = false;
			ToolsBar.IsEnabled = true;
		}
		else
		{
			_ = AlertAsync("خطا", "بارگذاری تصویر ممکن نبود.");
		}
	}

	async Task<bool> ConfirmReplaceAsync()
	{
		if (!_hasImage || Counter.MarkerCount == 0)
			return true;

		return await ConfirmAsync("عکس جدید", "علامت‌های فعلی پاک می‌شوند. ادامه می‌دهید؟", "ادامه", CancelText);
	}

	static void TryDelete(string? path)
	{
		try
		{
			if (!string.IsNullOrEmpty(path) && File.Exists(path))
				File.Delete(path);
		}
		catch (Exception)
		{
			// مهم نیست؛ کش را سیستم پاک می‌کند
		}
	}

	// ------------------------------------------------------------------ علامت‌ها

	async void OnMarkerTapped(object? sender, int index)
	{
		int number = index + 1;

		try
		{
			var action = await ChooseAsync($"علامت شماره {number}", CancelText, DeleteText, RenumberText);

			if (action == DeleteText)
			{
				Counter.DeleteMarker(index);
			}
			else if (action == RenumberText)
			{
				var input = await DisplayPromptAsync(
					"تغییر شماره",
					$"شماره‌ی جدید (از 1 تا {Counter.MarkerCount}):",
					"تأیید",
					CancelText,
					initialValue: number.ToString(),
					maxLength: 6,
					keyboard: Keyboard.Numeric);

				if (int.TryParse(NormalizeDigits(input), out var newNumber))
					Counter.MoveMarker(index, newNumber);
			}
		}
		finally
		{
			Counter.ClearSelection();
		}
	}

	static string NormalizeDigits(string? text)
	{
		if (string.IsNullOrWhiteSpace(text)) return string.Empty;

		var chars = text.Trim().ToCharArray();
		for (int i = 0; i < chars.Length; i++)
		{
			char c = chars[i];
			if (c >= '\u06F0' && c <= '\u06F9') chars[i] = (char)('0' + (c - '\u06F0'));      // ۰-۹
			else if (c >= '\u0660' && c <= '\u0669') chars[i] = (char)('0' + (c - '\u0660')); // ٠-٩
		}

		return new string(chars);
	}

	// ------------------------------------------------------------------ نوار ابزار

	void OnSizeChanged(object? sender, ValueChangedEventArgs e)
	{
		if (Counter is null) return;
		Counter.MarkerSize = e.NewValue;
	}

	void OnUndoClicked(object? sender, EventArgs e) => Counter.Undo();

	void OnZoomOutClicked(object? sender, EventArgs e) => Counter.ZoomOut();

	void OnZoomInClicked(object? sender, EventArgs e) => Counter.ZoomIn();

	void OnFitClicked(object? sender, EventArgs e) => Counter.ResetZoom();

	async void OnClearClicked(object? sender, EventArgs e)
	{
		if (Counter.MarkerCount == 0) return;

		if (await ConfirmAsync("پاک‌کردن", "همه‌ی علامت‌ها پاک شوند؟ (با دکمه‌ی «بازگشت» برمی‌گردند)", "پاک کن", CancelText))
			Counter.Clear();
	}

	async void OnExportClicked(object? sender, EventArgs e)
	{
		if (_busy || !_hasImage) return;
		_busy = true;

		try
		{
			var choice = await ChooseAsync("خروجی عکس همراه با شماره‌ها", CancelText, null, SaveText, ShareText);
			if (choice != SaveText && choice != ShareText)
				return;

			Busy.IsRunning = true;
			var path = await Counter.ExportAsync();
			Busy.IsRunning = false;

			if (path is null)
			{
				await AlertAsync("خطا", "ساخت خروجی ممکن نبود.");
				return;
			}

			if (choice == SaveText)
			{
				bool saved = GallerySaver.SaveToGallery(path);
				await AlertAsync(saved ? "ذخیره شد" : "خطا",
					saved ? "عکس در پوشه‌ی Pictures/PhotoCounter گالری ذخیره شد." : "ذخیره در گالری ممکن نبود.");
			}
			else
			{
				await Share.Default.RequestAsync(new ShareFileRequest
				{
					Title = "عکس شمارش‌شده",
					File = new ShareFile(path)
				});
			}
		}
		catch (Exception ex)
		{
			await AlertAsync("خطا", ex.Message);
		}
		finally
		{
			Busy.IsRunning = false;
			_busy = false;
		}
	}

	// ------------------------------------------------------------------ کمکی‌های دیالوگ (نقطه‌ی تغییر برای *Async)

	Task AlertAsync(string title, string message)
		=> DisplayAlert(title, message, "باشه");

	Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
		=> DisplayAlert(title, message, accept, cancel);

	Task<string> ChooseAsync(string title, string cancel, string? destruction, params string[] buttons)
		=> DisplayActionSheet(title, cancel, destruction, buttons);
}
