namespace PhotoCounter;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();

		builder
			.UseMauiApp<App>()
			.ConfigureMauiHandlers(handlers =>
			{
#if ANDROID
				handlers.AddHandler<CounterView, CounterViewHandler>();
#endif
			});

		return builder.Build();
	}
}
