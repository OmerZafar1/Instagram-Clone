using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace MiniInstagram.Services;

public class ChatHubConnectionFactory(IHttpContextAccessor httpContextAccessor, NavigationManager navigation)
{
    public HubConnection Create()
    {
        var hubUri = navigation.ToAbsoluteUri("/hubs/chat");

        var handler = new HttpClientHandler { UseCookies = true };
        var cookieContainer = new CookieContainer();

        if (httpContextAccessor.HttpContext is { } context)
        {
            foreach (var cookie in context.Request.Cookies)
            {
                cookieContainer.Add(new Cookie(
                    cookie.Key,
                    cookie.Value,
                    "/",
                    hubUri.Host));
            }
        }

        handler.CookieContainer = cookieContainer;

        return new HubConnectionBuilder()
            .WithUrl(hubUri, options => options.HttpMessageHandlerFactory = _ => handler)
            .WithAutomaticReconnect()
            .Build();
    }
}
