namespace PhotoCounter;
public class CounterView : View
{
	public static readonly BindableProperty ImagePathProperty =
		BindableProperty.Create(nameof(ImagePath), typeof(string), typeof(CounterView), null);

	public static readonly BindableProperty MarkerSizeProperty =
		BindableProperty.Create(nameof(MarkerSize), typeof(double), typeof(CounterView), 16d);

	public string? ImagePath
	{
		get => (string?)GetValue(ImagePathProperty);
		set => SetValue(ImagePathProperty, value);
	}

	public double MarkerSize
	{
		get => (double)GetValue(MarkerSizeProperty);
		set => SetValue(MarkerSizeProperty, value);
	}
	public int MarkerCount { get; private set; }

	public event EventHandler<int>? CountChanged;

	public event EventHandler<int>? MarkerTapped;

	public event EventHandler<bool>? ImageLoaded;

	internal void RaiseCountChanged(int count)
	{
		MarkerCount = count;
		CountChanged?.Invoke(this, count);
	}

	internal void RaiseMarkerTapped(int index) => MarkerTapped?.Invoke(this, index);

	internal void RaiseImageLoaded(bool ok) => ImageLoaded?.Invoke(this, ok);

	public void Undo() => Handler?.Invoke(nameof(Undo));
	public void Clear() => Handler?.Invoke(nameof(Clear));
	public void ZoomIn() => Handler?.Invoke(nameof(ZoomIn));
	public void ZoomOut() => Handler?.Invoke(nameof(ZoomOut));
	public void ResetZoom() => Handler?.Invoke(nameof(ResetZoom));
	public void ClearSelection() => Handler?.Invoke(nameof(ClearSelection));
	public void DeleteMarker(int index) => Handler?.Invoke(nameof(DeleteMarker), index);
	public void MoveMarker(int index, int newNumber) => Handler?.Invoke(nameof(MoveMarker), (index, newNumber));
	public Task<string?> ExportAsync()
	{
		var tcs = new TaskCompletionSource<string?>();
		if (Handler is null)
			tcs.SetResult(null);
		else
			Handler.Invoke(nameof(ExportAsync), tcs);
		return tcs.Task;
	}
}
