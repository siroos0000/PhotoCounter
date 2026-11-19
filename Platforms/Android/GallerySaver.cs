using Android.Content;
using Android.Provider;
using Microsoft.Maui.ApplicationModel;

namespace PhotoCounter;

public static class GallerySaver
{
	public static bool SaveToGallery(string sourcePath)
	{
		try
		{
			var resolver = Platform.AppContext.ContentResolver;

			var values = new ContentValues();
			values.Put(MediaStore.IMediaColumns.DisplayName, Path.GetFileName(sourcePath));
			values.Put(MediaStore.IMediaColumns.MimeType, "image/jpeg");
			values.Put(MediaStore.IMediaColumns.RelativePath, "Pictures/PhotoCounter");

			var uri = resolver.Insert(MediaStore.Images.Media.ExternalContentUri, values);
			if (uri == null) return false;

			using var output = resolver.OpenOutputStream(uri);
			using var input = File.OpenRead(sourcePath);
			input.CopyTo(output);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}
}
