#nullable disable
using Android.Content;
using Android.Views;
using ABitmap = Android.Graphics.Bitmap;
using ACanvas = Android.Graphics.Canvas;
using AColor = Android.Graphics.Color;
using AMatrix = Android.Graphics.Matrix;
using APaint = Android.Graphics.Paint;
using ATypeface = Android.Graphics.Typeface;
using AView = Android.Views.View;
using BitmapFactory = Android.Graphics.BitmapFactory;
using ExifInterface = Android.Media.ExifInterface;

namespace PhotoCounter;

public class CounterNativeView : AView
{
	const int MaxBitmapSide = 4096;   
	const int MaxUndoSteps = 100;

	sealed class Marker
	{
		public float X;   
		public float Y;
	}

	readonly List<Marker> _markers = new();
	readonly List<List<Marker>> _undoSteps = new();
	readonly GestureDetector _gestureDetector;
	readonly ScaleGestureDetector _scaleDetector;

	readonly APaint _bitmapPaint = new() { FilterBitmap = true };
	readonly APaint _fillPaint = new() { AntiAlias = true };
	readonly APaint _ringPaint = new() { AntiAlias = true };
	readonly APaint _textPaint = new() { AntiAlias = true };

	ABitmap _bitmap;
	float _scale = 1f;     
	float _tx, _ty;
	float _fitScale = 1f;   
	float _markerRadiusDp = 16f;
	int _selected = -1;
	int _loadVersion;

	public event Action<int> CountChanged;
	public event Action<int> MarkerTapped;

	public CounterNativeView(Context context) : base(context)
	{
		_fillPaint.Color = AColor.Argb(215, 229, 57, 53);

		_ringPaint.SetStyle(APaint.Style.Stroke);
		_ringPaint.Color = AColor.White;

		_textPaint.Color = AColor.White;
		_textPaint.TextAlign = APaint.Align.Center;
		_textPaint.SetTypeface(ATypeface.DefaultBold);

		_gestureDetector = new GestureDetector(context, new GestureListener(this));
		_scaleDetector = new ScaleGestureDetector(context, new ScaleListener(this));

		SetBackgroundColor(AColor.Argb(255, 18, 18, 18));
	}

	public int MarkerCount => _markers.Count;

	public float MarkerRadiusDp
	{
		get => _markerRadiusDp;
		set
		{
			_markerRadiusDp = Math.Clamp(value, 6f, 60f);
			Invalidate();
		}
	}

	public async Task<bool?> LoadImageAsync(string path)
	{
		int version = ++_loadVersion;
		ABitmap decoded = null;

		try
		{
			decoded = await Task.Run(() => DecodeBitmap(path));
		}
		catch (Exception)
		{
			decoded = null;
		}

		if (version != _loadVersion)
		{
			decoded?.Recycle();
			return null;
		}

		if (decoded == null)
			return false;

		var old = _bitmap;
		_bitmap = decoded;
		_markers.Clear();
		_undoSteps.Clear();
		_selected = -1;
		FitToView();
		Invalidate();
		old?.Recycle();
		CountChanged?.Invoke(0);
		return true;
	}

	public void ClearImage()
	{
		_loadVersion++;
		_bitmap?.Recycle();
		_bitmap = null;
		_markers.Clear();
		_undoSteps.Clear();
		_selected = -1;
		Invalidate();
		CountChanged?.Invoke(0);
	}

	public void Release()
	{
		_loadVersion++;
		_bitmap?.Recycle();
		_bitmap = null;
	}

	public void ZoomIn() => ZoomBy(1.5f, Width / 2f, Height / 2f);

	public void ZoomOut() => ZoomBy(1f / 1.5f, Width / 2f, Height / 2f);

	public void ResetZoom()
	{
		FitToView();
		Invalidate();
	}

	public void Undo()
	{
		if (_undoSteps.Count == 0) return;

		var snapshot = _undoSteps[_undoSteps.Count - 1];
		_undoSteps.RemoveAt(_undoSteps.Count - 1);
		_markers.Clear();
		_markers.AddRange(snapshot);
		_selected = -1;
		Invalidate();
		CountChanged?.Invoke(_markers.Count);
	}

	public void Clear()
	{
		if (_markers.Count == 0) return;

		PushUndo();
		_markers.Clear();
		_selected = -1;
		Invalidate();
		CountChanged?.Invoke(0);
	}

	public void DeleteMarker(int index)
	{
		if (index < 0 || index >= _markers.Count) return;

		PushUndo();
		_markers.RemoveAt(index);
		_selected = -1;
		Invalidate();
		CountChanged?.Invoke(_markers.Count);
	}


	public void MoveMarker(int index, int newNumber)
	{
		if (index < 0 || index >= _markers.Count) return;

		int target = Math.Clamp(newNumber, 1, _markers.Count) - 1;
		if (target != index)
		{
			PushUndo();
			var marker = _markers[index];
			_markers.RemoveAt(index);
			_markers.Insert(target, marker);
		}

		_selected = -1;
		Invalidate();
	}

	public void ClearSelection()
	{
		_selected = -1;
		Invalidate();
	}

	public ABitmap CreateExportBitmap()
	{
		if (_bitmap == null) return null;

		var output = ABitmap.CreateBitmap(_bitmap.Width, _bitmap.Height, ABitmap.Config.Argb8888);
		var canvas = new ACanvas(output);
		canvas.DrawBitmap(_bitmap, 0f, 0f, _bitmapPaint);
		float radius = Dp(_markerRadiusDp) / Math.Max(_fitScale, 0.01f);
		int oldSelected = _selected;
		_selected = -1;
		DrawMarkers(canvas, 1f, 0f, 0f, radius, false);
		_selected = oldSelected;

		return output;
	}

	public static void SaveJpeg(ABitmap bitmap, string path)
	{
		using var stream = File.Create(path);
		bitmap.Compress(ABitmap.CompressFormat.Jpeg, 92, stream);
	}

	static ABitmap DecodeBitmap(string path)
	{
		var bounds = new BitmapFactory.Options { InJustDecodeBounds = true };
		BitmapFactory.DecodeFile(path, bounds);

		int w = bounds.OutWidth;
		int h = bounds.OutHeight;
		if (w <= 0 || h <= 0) return null;

		int sample = 1;
		while (Math.Max(w, h) / sample > MaxBitmapSide)
			sample *= 2;

		var options = new BitmapFactory.Options { InSampleSize = sample };
		var bitmap = BitmapFactory.DecodeFile(path, options);
		if (bitmap == null) return null;

		int orientation = 1;
		try
		{
			using var exif = new ExifInterface(path);
			orientation = exif.GetAttributeInt(ExifInterface.TagOrientation, 1);
		}
		catch (Exception)
		{
		}

		return ApplyOrientation(bitmap, orientation);
	}

	static ABitmap ApplyOrientation(ABitmap source, int orientation)
	{
		var matrix = new AMatrix();
		switch (orientation)
		{
			case 2: matrix.SetScale(-1, 1); break;
			case 3: matrix.SetRotate(180); break;
			case 4: matrix.SetRotate(180); matrix.PostScale(-1, 1); break;
			case 5: matrix.SetRotate(90); matrix.PostScale(-1, 1); break;
			case 6: matrix.SetRotate(90); break;
			case 7: matrix.SetRotate(-90); matrix.PostScale(-1, 1); break;
			case 8: matrix.SetRotate(-90); break;
			default: return source;
		}

		var rotated = ABitmap.CreateBitmap(source, 0, 0, source.Width, source.Height, matrix, true);
		source.Recycle();
		return rotated;
	}


	float Dp(float value) => value * Resources.DisplayMetrics.Density;

	float MinScale => _fitScale;
	float MaxScale => Math.Max(_fitScale * 20f, 4f);

	void FitToView()
	{
		if (_bitmap == null || Width == 0 || Height == 0) return;

		_fitScale = Math.Min((float)Width / _bitmap.Width, (float)Height / _bitmap.Height);
		_scale = _fitScale;
		_tx = (Width - _bitmap.Width * _scale) / 2f;
		_ty = (Height - _bitmap.Height * _scale) / 2f;
	}

	void ZoomBy(float factor, float focusX, float focusY)
	{
		if (_bitmap == null) return;

		float newScale = Math.Clamp(_scale * factor, MinScale, MaxScale);
		float f = newScale / _scale;
		_tx = focusX - (focusX - _tx) * f;
		_ty = focusY - (focusY - _ty) * f;
		_scale = newScale;

		ClampTranslation();
		Invalidate();
	}

	void PanBy(float dx, float dy)
	{
		if (_bitmap == null) return;

		_tx += dx;
		_ty += dy;
		ClampTranslation();
		Invalidate();
	}

	void ClampTranslation()
	{
		if (_bitmap == null) return;

		float margin = Dp(24);
		float w = _bitmap.Width * _scale;
		float h = _bitmap.Height * _scale;

		_tx = w <= Width ? (Width - w) / 2f : Math.Clamp(_tx, Width - w - margin, margin);
		_ty = h <= Height ? (Height - h) / 2f : Math.Clamp(_ty, Height - h - margin, margin);
	}

	protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
	{
		base.OnSizeChanged(w, h, oldw, oldh);
		FitToView();
	}

	void PushUndo()
	{
		_undoSteps.Add(new List<Marker>(_markers));
		if (_undoSteps.Count > MaxUndoSteps)
			_undoSteps.RemoveAt(0);
	}

	int HitTest(float viewX, float viewY)
	{
		float radius = Dp(_markerRadiusDp);
		float best = radius * radius;
		int index = -1;

		for (int i = 0; i < _markers.Count; i++)
		{
			float dx = _markers[i].X * _scale + _tx - viewX;
			float dy = _markers[i].Y * _scale + _ty - viewY;
			float d = dx * dx + dy * dy;
			if (d <= best)
			{
				best = d;
				index = i;
			}
		}

		return index;
	}

	void HandleTap(float viewX, float viewY)
	{
		if (_bitmap == null) return;
		int hit = HitTest(viewX, viewY);
		if (hit >= 0)
		{
			_selected = hit;
			Invalidate();
			MarkerTapped?.Invoke(hit);
			return;
		}

		float ix = (viewX - _tx) / _scale;
		float iy = (viewY - _ty) / _scale;
		if (ix < 0 || iy < 0 || ix > _bitmap.Width || iy > _bitmap.Height)
			return;   // بیرون از عکس

		PushUndo();
		_markers.Add(new Marker { X = ix, Y = iy });
		Invalidate();
		CountChanged?.Invoke(_markers.Count);
	}

	protected override void OnDraw(ACanvas canvas)
	{
		base.OnDraw(canvas);
		if (_bitmap == null) return;

		canvas.Save();
		canvas.Translate(_tx, _ty);
		canvas.Scale(_scale, _scale);
		canvas.DrawBitmap(_bitmap, 0f, 0f, _bitmapPaint);
		canvas.Restore();

		DrawMarkers(canvas, _scale, _tx, _ty, Dp(_markerRadiusDp), true);
	}

	void DrawMarkers(ACanvas canvas, float scale, float tx, float ty, float radius, bool cullOutsideView)
	{
		_ringPaint.StrokeWidth = Math.Max(2f, radius * 0.14f);

		for (int i = 0; i < _markers.Count; i++)
		{
			float x = _markers[i].X * scale + tx;
			float y = _markers[i].Y * scale + ty;

			if (cullOutsideView && (x < -radius || y < -radius || x > Width + radius || y > Height + radius))
				continue;

			canvas.DrawCircle(x, y, radius, _fillPaint);
			_ringPaint.Color = i == _selected ? AColor.Yellow : AColor.White;
			canvas.DrawCircle(x, y, radius, _ringPaint);

			string label = (i + 1).ToString();
			_textPaint.TextSize = radius * (label.Length <= 2 ? 1.1f : label.Length == 3 ? 0.85f : 0.65f);
			var metrics = _textPaint.GetFontMetrics();
			canvas.DrawText(label, x, y - (metrics.Ascent + metrics.Descent) / 2f, _textPaint);
		}
	}

	public override bool OnTouchEvent(MotionEvent e)
	{
		_scaleDetector.OnTouchEvent(e);
		_gestureDetector.OnTouchEvent(e);
		return true;
	}

	sealed class GestureListener : GestureDetector.SimpleOnGestureListener
	{
		readonly CounterNativeView _owner;

		public GestureListener(CounterNativeView owner) => _owner = owner;

		public override bool OnDown(MotionEvent e) => true;

		public override bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
		{
			_owner.PanBy(-distanceX, -distanceY);
			return true;
		}

		public override bool OnSingleTapUp(MotionEvent e)
		{
			_owner.HandleTap(e.GetX(), e.GetY());
			return true;
		}
	}

	sealed class ScaleListener : ScaleGestureDetector.SimpleOnScaleGestureListener
	{
		readonly CounterNativeView _owner;

		public ScaleListener(CounterNativeView owner) => _owner = owner;

		public override bool OnScale(ScaleGestureDetector detector)
		{
			_owner.ZoomBy(detector.ScaleFactor, detector.FocusX, detector.FocusY);
			return true;
		}
	}
}
