﻿global using Plugin.Maui.Audio;
global using MAUIDnr1.Services;
global using MAUIDnr1.Models;
global using MAUIDnr1.Shared;
global using Microsoft.AspNetCore.Components;
global using Microsoft.JSInterop;
global using Newtonsoft.Json;
global using System.Collections.ObjectModel;
global using System.Text;
using Blazored.Modal;

namespace MAUIDnr1;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddBlazoredModal();
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif
        builder.Services.AddSingleton(AudioManager.Current);
        builder.Services.AddSingleton<ApiService>();
        return builder.Build();
    }
}
