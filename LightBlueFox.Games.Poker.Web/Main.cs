using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace LightBlueFox.Games.Poker.Web
{
	public class Entry
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);
			builder.Services.AddRazorPages();
			builder.Services.AddServerSideBlazor();

			var app = builder.Build();

			if (!app.Environment.IsDevelopment())
			{
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}


			app.UseStaticFiles();

			app.UseRouting();

			app.MapBlazorHub();
			app.MapFallbackToPage("/_Host");

			app.Run();
		}
	}

}

